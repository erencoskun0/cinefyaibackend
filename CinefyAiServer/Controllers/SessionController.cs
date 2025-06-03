using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CinefyAiServer.Data;
using CinefyAiServer.Entities;
using CinefyAiServer.Entities.DTOs;
using System.Security.Claims;

namespace CinefyAiServer.Controllers;

/// <summary>
/// Seans yönetimi API endpoint'leri
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Sessions")]
public class SessionController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SessionController> _logger;

    public SessionController(ApplicationDbContext context, ILogger<SessionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seansları listeler (filtreleme ile)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<SessionsResponse>> GetSessions([FromQuery] SessionsQuery query)
    {
        try
        {
            var sessionsQueryable = _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Include(s => s.Cinema)
                .Where(s => s.IsActive)
                .AsQueryable();

            // Filtreleme
            if (query.CinemaId.HasValue)
            {
                sessionsQueryable = sessionsQueryable.Where(s => s.CinemaId == query.CinemaId.Value);
            }

            if (query.MovieId.HasValue)
            {
                sessionsQueryable = sessionsQueryable.Where(s => s.MovieId == query.MovieId.Value);
            }

            if (query.Date.HasValue)
            {
                sessionsQueryable = sessionsQueryable.Where(s => s.SessionDate == query.Date.Value);
            }
            else
            {
                // Geçmiş tarihli seansları gösterme
                var today = DateOnly.FromDateTime(DateTime.Today);
                sessionsQueryable = sessionsQueryable.Where(s => s.SessionDate >= today);
            }

            if (!string.IsNullOrEmpty(query.HallType))
            {
                sessionsQueryable = sessionsQueryable.Where(s =>
                    s.Hall != null && s.Hall.Features != null &&
                    s.Hall.Features.Contains(query.HallType));
            }

            // Toplam sayı
            var totalCount = await sessionsQueryable.CountAsync();

            // Sıralama
            sessionsQueryable = sessionsQueryable
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.SessionTime);

            // Sayfalama
            var sessions = await sessionsQueryable
                .Skip((query.Page - 1) * query.Limit)
                .Take(query.Limit)
                .Select(s => new SessionResponse
                {
                    Id = s.Id,
                    MovieId = s.MovieId,
                    HallId = s.HallId,
                    CinemaId = s.CinemaId,
                    SessionDate = s.SessionDate,
                    SessionTime = s.SessionTime,
                    EndTime = s.EndTime,
                    StandardPrice = s.StandardPrice,
                    VipPrice = s.VipPrice,
                    AvailableSeats = s.AvailableSeats,
                    TotalSeats = s.TotalSeats,
                    OccupancyStatus = s.OccupancyStatus,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    Movie = s.Movie != null ? new MovieResponse
                    {
                        Id = s.Movie.Id,
                        Title = s.Movie.Title,
                        Poster = s.Movie.Poster,
                        Duration = s.Movie.Duration,
                        Rating = s.Movie.Rating,
                        Genre = s.Movie.Genre,
                        Description = s.Movie.Description,
                        ReleaseDate = s.Movie.ReleaseDate,
                        Director = s.Movie.Director,
                        Cast = s.Movie.Cast,
                        AgeRating = s.Movie.AgeRating,
                        IsPopular = s.Movie.IsPopular,
                        IsNew = s.Movie.IsNew,
                        IsActive = s.Movie.IsActive
                    } : null,
                    Hall = s.Hall != null ? new HallResponse
                    {
                        Id = s.Hall.Id,
                        Name = s.Hall.Name,
                        Capacity = s.Hall.Capacity,
                        Features = s.Hall.Features,
                        IsActive = s.Hall.IsActive
                    } : null,
                    Cinema = s.Cinema != null ? new CinemaResponse
                    {
                        Id = s.Cinema.Id,
                        Name = s.Cinema.Name,
                        Brand = s.Cinema.Brand,
                        City = s.Cinema.City,
                        District = s.Cinema.District,
                        Address = s.Cinema.Address
                    } : null
                })
                .ToListAsync();

            // Salon türüne göre gruplandırma
            var groupedByHall = sessions
                .GroupBy(s => s.Hall?.Features?.FirstOrDefault() ?? "Standard")
                .ToDictionary(g => g.Key, g => g.ToList());

            var response = new SessionsResponse
            {
                Sessions = sessions,
                GroupedByHall = groupedByHall,
                Pagination = new PaginationInfo
                {
                    Page = query.Page,
                    Limit = query.Limit,
                    Total = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / query.Limit)
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Seanslar listelenirken hata oluştu");
            return StatusCode(500, new { message = "Seanslar listelenirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Belirli bir seansın detayını getirir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<SessionResponse>> GetSession(Guid id)
    {
        try
        {
            var session = await _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Include(s => s.Cinema)
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

            if (session == null)
            {
                return NotFound(new { message = "Seans bulunamadı" });
            }

            var response = new SessionResponse
            {
                Id = session.Id,
                MovieId = session.MovieId,
                HallId = session.HallId,
                CinemaId = session.CinemaId,
                SessionDate = session.SessionDate,
                SessionTime = session.SessionTime,
                EndTime = session.EndTime,
                StandardPrice = session.StandardPrice,
                VipPrice = session.VipPrice,
                AvailableSeats = session.AvailableSeats,
                TotalSeats = session.TotalSeats,
                OccupancyStatus = session.OccupancyStatus,
                IsActive = session.IsActive,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                Movie = session.Movie != null ? new MovieResponse
                {
                    Id = session.Movie.Id,
                    Title = session.Movie.Title,
                    Poster = session.Movie.Poster,
                    Duration = session.Movie.Duration,
                    Rating = session.Movie.Rating,
                    Genre = session.Movie.Genre,
                    Description = session.Movie.Description,
                    ReleaseDate = session.Movie.ReleaseDate,
                    Director = session.Movie.Director,
                    Cast = session.Movie.Cast,
                    AgeRating = session.Movie.AgeRating,
                    IsPopular = session.Movie.IsPopular,
                    IsNew = session.Movie.IsNew,
                    IsActive = session.Movie.IsActive
                } : null,
                Hall = session.Hall != null ? new HallResponse
                {
                    Id = session.Hall.Id,
                    Name = session.Hall.Name,
                    Capacity = session.Hall.Capacity,
                    Features = session.Hall.Features,
                    IsActive = session.Hall.IsActive
                } : null,
                Cinema = session.Cinema != null ? new CinemaResponse
                {
                    Id = session.Cinema.Id,
                    Name = session.Cinema.Name,
                    Brand = session.Cinema.Brand,
                    City = session.Cinema.City,
                    District = session.Cinema.District,
                    Address = session.Cinema.Address,
                    Phone = session.Cinema.Phone,
                    Email = session.Cinema.Email
                } : null
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Seans detayı getirilirken hata oluştu: {SessionId}", id);
            return StatusCode(500, new { message = "Seans detayı getirilirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Seansın koltuk düzenini ve müsaitlik durumunu getirir
    /// </summary>
    [HttpGet("{id}/seats")]
    public async Task<ActionResult<SeatsResponse>> GetSessionSeats(Guid id)
    {
        try
        {
            var session = await _context.Sessions
                .Include(s => s.Hall)
                .Include(s => s.Bookings)
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

            if (session == null)
            {
                return NotFound(new { message = "Seans bulunamadı" });
            }

            // Rezerve edilmiş koltukları al
            var occupiedSeats = session.Bookings?
                .Where(b => b.Status == BookingStatus.Confirmed && b.PaymentStatus == PaymentStatus.Completed)
                .SelectMany(b => b.SelectedSeats)
                .Select(s => $"{s.Row}{s.Number}")
                .ToList() ?? new List<string>();

            // Koltuk düzenini oluştur (örnek düzen - gerçekte Hall entity'sinden gelecek)
            var seatLayout = GenerateSeatLayout(session.Hall!, occupiedSeats);

            var response = new SeatsResponse
            {
                Layout = seatLayout,
                OccupiedSeats = occupiedSeats,
                Prices = new SeatPrices
                {
                    Standard = session.StandardPrice,
                    Vip = session.VipPrice
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Seans koltuk bilgileri getirilirken hata oluştu: {SessionId}", id);
            return StatusCode(500, new { message = "Seans koltuk bilgileri getirilirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Yeni seans oluşturur (sadece Owner/Admin)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<SessionResponse>> CreateSession(CreateSessionRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Cinema ve Hall'ın varlığını kontrol et
            var cinema = await _context.Cinemas.FindAsync(request.CinemaId);
            var hall = await _context.Halls.FindAsync(request.HallId);
            var movie = await _context.Movies.FindAsync(request.MovieId);

            if (cinema == null || hall == null || movie == null)
            {
                return BadRequest(new { message = "Sinema, salon veya film bulunamadı" });
            }

            // Owner kontrolü
            if (userRole == "Owner" && cinema.OwnerId != Guid.Parse(userId!))
            {
                return Forbid("Bu sinemada seans oluşturma yetkiniz yok");
            }

            // Çakışma kontrolü
            var conflictingSessions = await _context.Sessions
                .Where(s => s.HallId == request.HallId &&
                           s.SessionDate == request.SessionDate &&
                           s.IsActive &&
                           ((s.SessionTime <= request.SessionTime && s.EndTime > request.SessionTime) ||
                            (s.SessionTime < request.EndTime && s.EndTime >= request.EndTime) ||
                            (s.SessionTime >= request.SessionTime && s.EndTime <= request.EndTime)))
                .AnyAsync();

            if (conflictingSessions)
            {
                return BadRequest(new { message = "Bu salon ve zaman diliminde zaten bir seans mevcut" });
            }

            var session = new Session
            {
                Id = Guid.NewGuid(),
                MovieId = request.MovieId,
                HallId = request.HallId,
                CinemaId = request.CinemaId,
                SessionDate = request.SessionDate,
                SessionTime = request.SessionTime,
                EndTime = request.EndTime,
                StandardPrice = request.StandardPrice,
                VipPrice = request.VipPrice,
                AvailableSeats = request.TotalSeats,
                TotalSeats = request.TotalSeats,
                OccupancyStatus = OccupancyStatus.Müsait,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            // Response için session'ı tekrar yükle
            var createdSession = await _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Include(s => s.Cinema)
                .FirstAsync(s => s.Id == session.Id);

            var response = new SessionResponse
            {
                Id = createdSession.Id,
                MovieId = createdSession.MovieId,
                HallId = createdSession.HallId,
                CinemaId = createdSession.CinemaId,
                SessionDate = createdSession.SessionDate,
                SessionTime = createdSession.SessionTime,
                EndTime = createdSession.EndTime,
                StandardPrice = createdSession.StandardPrice,
                VipPrice = createdSession.VipPrice,
                AvailableSeats = createdSession.AvailableSeats,
                TotalSeats = createdSession.TotalSeats,
                OccupancyStatus = createdSession.OccupancyStatus,
                IsActive = createdSession.IsActive,
                CreatedAt = createdSession.CreatedAt,
                UpdatedAt = createdSession.UpdatedAt,
                Movie = createdSession.Movie != null ? new MovieResponse
                {
                    Id = createdSession.Movie.Id,
                    Title = createdSession.Movie.Title,
                    Poster = createdSession.Movie.Poster,
                    Duration = createdSession.Movie.Duration
                } : null,
                Hall = createdSession.Hall != null ? new HallResponse
                {
                    Id = createdSession.Hall.Id,
                    Name = createdSession.Hall.Name,
                    Capacity = createdSession.Hall.Capacity
                } : null,
                Cinema = createdSession.Cinema != null ? new CinemaResponse
                {
                    Id = createdSession.Cinema.Id,
                    Name = createdSession.Cinema.Name,
                    Brand = createdSession.Cinema.Brand
                } : null
            };

            return CreatedAtAction(nameof(GetSession), new { id = session.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Seans oluşturulurken hata oluştu");
            return StatusCode(500, new { message = "Seans oluşturulurken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Seans günceller (sadece Owner/Admin)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<SessionResponse>> UpdateSession(Guid id, UpdateSessionRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var session = await _context.Sessions
                .Include(s => s.Cinema)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                return NotFound(new { message = "Seans bulunamadı" });
            }

            // Owner kontrolü
            if (userRole == "Owner" && session.Cinema?.OwnerId != Guid.Parse(userId!))
            {
                return Forbid("Bu seansı güncelleme yetkiniz yok");
            }

            // Güncelleme işlemleri
            if (request.SessionDate.HasValue)
                session.SessionDate = request.SessionDate.Value;
            if (request.SessionTime.HasValue)
                session.SessionTime = request.SessionTime.Value;
            if (request.EndTime.HasValue)
                session.EndTime = request.EndTime.Value;
            if (request.StandardPrice.HasValue)
                session.StandardPrice = request.StandardPrice.Value;
            if (request.VipPrice.HasValue)
                session.VipPrice = request.VipPrice.Value;

            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Güncellenmiş response
            var updatedSession = await _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Include(s => s.Cinema)
                .FirstAsync(s => s.Id == id);

            var response = new SessionResponse
            {
                Id = updatedSession.Id,
                MovieId = updatedSession.MovieId,
                HallId = updatedSession.HallId,
                CinemaId = updatedSession.CinemaId,
                SessionDate = updatedSession.SessionDate,
                SessionTime = updatedSession.SessionTime,
                EndTime = updatedSession.EndTime,
                StandardPrice = updatedSession.StandardPrice,
                VipPrice = updatedSession.VipPrice,
                AvailableSeats = updatedSession.AvailableSeats,
                TotalSeats = updatedSession.TotalSeats,
                OccupancyStatus = updatedSession.OccupancyStatus,
                IsActive = updatedSession.IsActive,
                CreatedAt = updatedSession.CreatedAt,
                UpdatedAt = updatedSession.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Seans güncellenirken hata oluştu: {SessionId}", id);
            return StatusCode(500, new { message = "Seans güncellenirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Seans siler (sadece Owner/Admin)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> DeleteSession(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var session = await _context.Sessions
                .Include(s => s.Cinema)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                return NotFound(new { message = "Seans bulunamadı" });
            }

            // Owner kontrolü
            if (userRole == "Owner" && session.Cinema?.OwnerId != Guid.Parse(userId!))
            {
                return Forbid("Bu seansı silme yetkiniz yok");
            }

            // Rezervasyonu olan seansları kontrol et
            var hasBookings = await _context.Bookings
                .AnyAsync(b => b.SessionId == id && b.Status == BookingStatus.Confirmed);

            if (hasBookings)
            {
                return BadRequest(new { message = "Rezervasyonu olan seans silinemez" });
            }

            // Soft delete
            session.IsActive = false;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Seans silinirken hata oluştu: {SessionId}", id);
            return StatusCode(500, new { message = "Seans silinirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Koltuk düzeni oluşturur (örnek implementasyon)
    /// </summary>
    private static SeatLayoutResponse GenerateSeatLayout(Hall hall, List<string> occupiedSeats)
    {
        var rows = new List<SeatRowResponse>();
        var capacity = hall.Capacity;
        var rowCount = (int)Math.Ceiling(capacity / 20.0); // Her sırada maksimum 20 koltuk
        var seatsPerRow = capacity / rowCount;

        for (int i = 0; i < rowCount; i++)
        {
            var rowLetter = ((char)('A' + i)).ToString();
            var seats = new List<SeatResponse>();

            for (int j = 1; j <= seatsPerRow; j++)
            {
                var seatCode = $"{rowLetter}{j}";
                var seatType = j > seatsPerRow - 4 ? SeatType.Vip : SeatType.Standard; // Son 4 koltuk VIP

                seats.Add(new SeatResponse
                {
                    Number = j,
                    Type = seatType,
                    IsOccupied = occupiedSeats.Contains(seatCode),
                    IsReserved = false,
                    RowLetter = rowLetter
                });
            }

            rows.Add(new SeatRowResponse
            {
                RowLetter = rowLetter,
                Seats = seats
            });
        }

        return new SeatLayoutResponse
        {
            Rows = rows
        };
    }
}