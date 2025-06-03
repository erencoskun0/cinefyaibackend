using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CinefyAiServer.Data;
using CinefyAiServer.Entities;
using CinefyAiServer.Entities.DTOs;
using System.Security.Claims;
using System.Text;

namespace CinefyAiServer.Controllers;

/// <summary>
/// Rezervasyon yönetimi API endpoint'leri
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Bookings")]
public class BookingController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BookingController> _logger;

    public BookingController(ApplicationDbContext context, ILogger<BookingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Yeni rezervasyon oluşturur
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> CreateBooking(CreateBookingRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var userId = User.Identity?.IsAuthenticated == true ?
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value : null;

            // Session kontrolü
            var session = await _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Include(s => s.Cinema)
                .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.IsActive);

            if (session == null)
            {
                return NotFound(new { message = "Seans bulunamadı" });
            }

            // Geçmiş seans kontrolü
            var sessionDateTime = session.SessionDate.ToDateTime(session.SessionTime);
            if (sessionDateTime <= DateTime.Now)
            {
                return BadRequest(new { message = "Geçmiş tarihli seanslar için rezervasyon yapılamaz" });
            }

            // Koltuk müsaitlik kontrolü
            var occupiedSeats = await _context.Bookings
                .Where(b => b.SessionId == request.SessionId &&
                           b.Status == BookingStatus.Confirmed &&
                           b.PaymentStatus != PaymentStatus.Failed)
                .SelectMany(b => b.SelectedSeats)
                .Select(s => $"{s.Row}{s.Number}")
                .ToListAsync();

            var requestedSeats = request.SelectedSeats.Select(s => $"{s.Row}{s.Number}").ToList();
            var conflictingSeats = requestedSeats.Intersect(occupiedSeats).ToList();

            if (conflictingSeats.Any())
            {
                return BadRequest(new
                {
                    message = "Seçilen koltuklar zaten rezerve edilmiş",
                    conflictingSeats
                });
            }

            // Fiyat hesaplama
            var totalAmount = request.SelectedSeats.Sum(s =>
                s.Type == SeatType.Vip ? session.VipPrice : session.StandardPrice);

            // İndirim hesaplama
            var discountAmount = CalculateDiscount(totalAmount, request.DiscountType);
            var finalAmount = totalAmount - discountAmount;

            // Rezervasyon kodu oluştur
            var bookingCode = GenerateBookingCode();

            // QR kod oluştur
            var qrCode = GenerateQRCode(bookingCode);

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                SessionId = request.SessionId,
                UserId = userId != null ? Guid.Parse(userId) : null,
                CustomerName = request.CustomerInfo.Name,
                CustomerEmail = request.CustomerInfo.Email,
                CustomerPhone = request.CustomerInfo.Phone,
                SelectedSeats = request.SelectedSeats.Select(s => new SeatSelection
                {
                    Row = s.Row,
                    Number = s.Number,
                    Type = s.Type,
                    Price = s.Type == SeatType.Vip ? session.VipPrice : session.StandardPrice
                }).ToList(),
                TotalAmount = totalAmount,
                DiscountAmount = discountAmount,
                FinalAmount = finalAmount,
                DiscountType = request.DiscountType,
                PaymentStatus = PaymentStatus.Pending,
                PaymentMethod = request.PaymentMethod,
                BookingCode = bookingCode,
                QrCode = qrCode,
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);

            // Session'ın müsait koltuk sayısını güncelle
            session.AvailableSeats -= request.SelectedSeats.Count;
            session.OccupancyStatus = CalculateOccupancyStatus(session.AvailableSeats, session.TotalSeats);
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var response = new BookingResponse
            {
                Id = booking.Id,
                SessionId = booking.SessionId,
                UserId = booking.UserId,
                CustomerName = booking.CustomerName,
                CustomerEmail = booking.CustomerEmail,
                CustomerPhone = booking.CustomerPhone,
                SelectedSeats = booking.SelectedSeats,
                TotalAmount = booking.TotalAmount,
                DiscountAmount = booking.DiscountAmount,
                FinalAmount = booking.FinalAmount,
                DiscountType = booking.DiscountType,
                PaymentStatus = booking.PaymentStatus,
                PaymentMethod = booking.PaymentMethod,
                BookingCode = booking.BookingCode,
                QrCode = booking.QrCode,
                Status = booking.Status,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                Session = new SessionResponse
                {
                    Id = session.Id,
                    SessionDate = session.SessionDate,
                    SessionTime = session.SessionTime,
                    Movie = session.Movie != null ? new MovieResponse
                    {
                        Id = session.Movie.Id,
                        Title = session.Movie.Title,
                        Poster = session.Movie.Poster,
                        Duration = session.Movie.Duration
                    } : null,
                    Hall = session.Hall != null ? new HallResponse
                    {
                        Id = session.Hall.Id,
                        Name = session.Hall.Name
                    } : null,
                    Cinema = session.Cinema != null ? new CinemaResponse
                    {
                        Id = session.Cinema.Id,
                        Name = session.Cinema.Name,
                        Address = session.Cinema.Address,
                        City = session.Cinema.City
                    } : null
                },
                PaymentUrl = GeneratePaymentUrl(booking.Id, finalAmount) // Ödeme URL'i oluştur
            };

            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, response);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Rezervasyon oluşturulurken hata oluştu");
            return StatusCode(500, new { message = "Rezervasyon oluşturulurken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Rezervasyon detayını getirir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BookingDetailResponse>> GetBooking(Guid id)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.Session)
                    .ThenInclude(s => s!.Movie)
                .Include(b => b.Session)
                    .ThenInclude(s => s!.Hall)
                .Include(b => b.Session)
                    .ThenInclude(s => s!.Cinema)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound(new { message = "Rezervasyon bulunamadı" });
            }

            var response = new BookingDetailResponse
            {
                Id = booking.Id,
                SessionId = booking.SessionId,
                UserId = booking.UserId,
                CustomerName = booking.CustomerName,
                CustomerEmail = booking.CustomerEmail,
                CustomerPhone = booking.CustomerPhone,
                SelectedSeats = booking.SelectedSeats,
                TotalAmount = booking.TotalAmount,
                DiscountAmount = booking.DiscountAmount,
                FinalAmount = booking.FinalAmount,
                DiscountType = booking.DiscountType,
                PaymentStatus = booking.PaymentStatus,
                PaymentMethod = booking.PaymentMethod,
                TransactionId = booking.TransactionId,
                BookingCode = booking.BookingCode,
                QrCode = booking.QrCode,
                Status = booking.Status,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                Movie = booking.Session?.Movie != null ? new MovieResponse
                {
                    Id = booking.Session.Movie.Id,
                    Title = booking.Session.Movie.Title,
                    Poster = booking.Session.Movie.Poster,
                    Duration = booking.Session.Movie.Duration,
                    Genre = booking.Session.Movie.Genre
                } : null,
                Cinema = booking.Session?.Cinema != null ? new CinemaResponse
                {
                    Id = booking.Session.Cinema.Id,
                    Name = booking.Session.Cinema.Name,
                    Address = booking.Session.Cinema.Address,
                    City = booking.Session.Cinema.City,
                    Phone = booking.Session.Cinema.Phone
                } : null,
                Hall = booking.Session?.Hall != null ? new HallResponse
                {
                    Id = booking.Session.Hall.Id,
                    Name = booking.Session.Hall.Name,
                    Capacity = booking.Session.Hall.Capacity
                } : null,
                Session = booking.Session != null ? new SessionResponse
                {
                    Id = booking.Session.Id,
                    SessionDate = booking.Session.SessionDate,
                    SessionTime = booking.Session.SessionTime,
                    EndTime = booking.Session.EndTime
                } : null
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rezervasyon detayı getirilirken hata oluştu: {BookingId}", id);
            return StatusCode(500, new { message = "Rezervasyon detayı getirilirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Kullanıcının rezervasyonlarını listeler
    /// </summary>
    [HttpGet("user/{userId}")]
    [Authorize]
    public async Task<ActionResult<UserBookingsResponse>> GetUserBookings(Guid userId, [FromQuery] int page = 1, [FromQuery] int limit = 10)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Kullanıcı sadece kendi rezervasyonlarını görebilir (Admin hariç)
            if (userRole != "Admin" && currentUserId != userId.ToString())
            {
                return Forbid("Bu rezervasyonları görüntüleme yetkiniz yok");
            }

            var bookingsQueryable = _context.Bookings
                .Include(b => b.Session)
                    .ThenInclude(s => s!.Movie)
                .Include(b => b.Session)
                    .ThenInclude(s => s!.Cinema)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt);

            var totalCount = await bookingsQueryable.CountAsync();

            var bookings = await bookingsQueryable
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(b => new BookingResponse
                {
                    Id = b.Id,
                    SessionId = b.SessionId,
                    CustomerName = b.CustomerName,
                    CustomerEmail = b.CustomerEmail,
                    SelectedSeats = b.SelectedSeats,
                    FinalAmount = b.FinalAmount,
                    PaymentStatus = b.PaymentStatus,
                    BookingCode = b.BookingCode,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    Session = b.Session != null ? new SessionResponse
                    {
                        Id = b.Session.Id,
                        SessionDate = b.Session.SessionDate,
                        SessionTime = b.Session.SessionTime,
                        Movie = b.Session.Movie != null ? new MovieResponse
                        {
                            Id = b.Session.Movie.Id,
                            Title = b.Session.Movie.Title,
                            Poster = b.Session.Movie.Poster
                        } : null,
                        Cinema = b.Session.Cinema != null ? new CinemaResponse
                        {
                            Id = b.Session.Cinema.Id,
                            Name = b.Session.Cinema.Name,
                            City = b.Session.Cinema.City
                        } : null
                    } : null
                })
                .ToListAsync();

            // İstatistikler
            var stats = new BookingStats
            {
                TotalBookings = await _context.Bookings.CountAsync(b => b.UserId == userId),
                CompletedBookings = await _context.Bookings.CountAsync(b => b.UserId == userId && b.Status == BookingStatus.Completed),
                CancelledBookings = await _context.Bookings.CountAsync(b => b.UserId == userId && b.Status == BookingStatus.Cancelled),
                TotalSpent = await _context.Bookings
                    .Where(b => b.UserId == userId && b.PaymentStatus == PaymentStatus.Completed)
                    .SumAsync(b => b.FinalAmount),
                UpcomingMovies = await _context.Bookings
                    .Include(b => b.Session)
                    .Where(b => b.UserId == userId &&
                               b.Status == BookingStatus.Confirmed &&
                               b.Session!.SessionDate >= DateOnly.FromDateTime(DateTime.Today))
                    .CountAsync()
            };

            var response = new UserBookingsResponse
            {
                Bookings = bookings,
                Stats = stats,
                Pagination = new PaginationInfo
                {
                    Page = page,
                    Limit = limit,
                    Total = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / limit)
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı rezervasyonları getirilirken hata oluştu: {UserId}", userId);
            return StatusCode(500, new { message = "Kullanıcı rezervasyonları getirilirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Rezervasyonu iptal eder
    /// </summary>
    [HttpPut("{id}/cancel")]
    [Authorize]
    public async Task<ActionResult> CancelBooking(Guid id, CancelBookingRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var booking = await _context.Bookings
                .Include(b => b.Session)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound(new { message = "Rezervasyon bulunamadı" });
            }

            // Yetki kontrolü
            if (userRole != "Admin" && booking.UserId?.ToString() != userId)
            {
                return Forbid("Bu rezervasyonu iptal etme yetkiniz yok");
            }

            // İptal edilebilir mi kontrolü
            if (booking.Status == BookingStatus.Cancelled)
            {
                return BadRequest(new { message = "Rezervasyon zaten iptal edilmiş" });
            }

            if (booking.Status == BookingStatus.Completed)
            {
                return BadRequest(new { message = "Tamamlanmış rezervasyon iptal edilemez" });
            }

            // Seans zamanı kontrolü (2 saat öncesine kadar iptal edilebilir)
            var sessionDateTime = booking.Session!.SessionDate.ToDateTime(booking.Session.SessionTime);
            if (sessionDateTime.AddHours(-2) <= DateTime.Now)
            {
                return BadRequest(new { message = "Seans başlangıcından 2 saat öncesine kadar iptal edebilirsiniz" });
            }

            // Rezervasyonu iptal et
            booking.Status = BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;

            // Ödeme yapılmışsa iade işlemi başlat
            if (booking.PaymentStatus == PaymentStatus.Completed)
            {
                booking.PaymentStatus = PaymentStatus.Refunded;
                // Burada ödeme sağlayıcısı ile iade işlemi yapılacak
            }

            // Session'ın müsait koltuk sayısını geri yükle
            booking.Session.AvailableSeats += booking.SelectedSeats.Count;
            booking.Session.OccupancyStatus = CalculateOccupancyStatus(booking.Session.AvailableSeats, booking.Session.TotalSeats);
            booking.Session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Rezervasyon iptal edilirken hata oluştu: {BookingId}", id);
            return StatusCode(500, new { message = "Rezervasyon iptal edilirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// QR kod getirir
    /// </summary>
    [HttpGet("{id}/qr")]
    public async Task<ActionResult<QrCodeResponse>> GetBookingQR(Guid id)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.Session)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound(new { message = "Rezervasyon bulunamadı" });
            }

            if (booking.Status != BookingStatus.Confirmed || booking.PaymentStatus != PaymentStatus.Completed)
            {
                return BadRequest(new { message = "Geçersiz rezervasyon durumu" });
            }

            var sessionDateTime = booking.Session!.SessionDate.ToDateTime(booking.Session.SessionTime);

            var response = new QrCodeResponse
            {
                QrCode = booking.QrCode!,
                BookingCode = booking.BookingCode,
                ExpiryDate = sessionDateTime.AddHours(1) // Seans başlangıcından 1 saat sonra geçersiz
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QR kod getirilirken hata oluştu: {BookingId}", id);
            return StatusCode(500, new { message = "QR kod getirilirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Rezervasyon kodu oluşturur
    /// </summary>
    private static string GenerateBookingCode()
    {
        var random = new Random();
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// QR kod oluşturur
    /// </summary>
    private static string GenerateQRCode(string bookingCode)
    {
        // Gerçek uygulamada QR kod kütüphanesi kullanılacak
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"CINEFY_{bookingCode}_{DateTime.UtcNow:yyyyMMddHHmmss}"));
    }

    /// <summary>
    /// İndirim hesaplar
    /// </summary>
    private static decimal CalculateDiscount(decimal totalAmount, string? discountType)
    {
        return discountType?.ToLower() switch
        {
            "öğrenci" => totalAmount * 0.2m, // %20 öğrenci indirimi
            "65+ yaş" => totalAmount * 0.3m, // %30 yaşlı indirimi
            "çarşamba" => totalAmount * 0.15m, // %15 çarşamba indirimi
            _ => 0
        };
    }

    /// <summary>
    /// Doluluk durumunu hesaplar
    /// </summary>
    private static OccupancyStatus CalculateOccupancyStatus(int availableSeats, int totalSeats)
    {
        var occupancyRate = (double)(totalSeats - availableSeats) / totalSeats;

        return occupancyRate switch
        {
            >= 0.9 => OccupancyStatus.AzKoltuk,
            >= 0.7 => OccupancyStatus.DolmakÜzere,
            _ => OccupancyStatus.Müsait
        };
    }

    /// <summary>
    /// Ödeme URL'i oluşturur
    /// </summary>
    private static string GeneratePaymentUrl(Guid bookingId, decimal amount)
    {
        // Gerçek uygulamada ödeme sağlayıcısı entegrasyonu yapılacak
        return $"https://payment.cinefy.ai/pay?booking={bookingId}&amount={amount:F2}";
    }
}