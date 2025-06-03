using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CinefyAiServer.Data;
using CinefyAiServer.Entities;
using CinefyAiServer.Entities.DTOs;
using System.Security.Claims;
using System.Collections.Generic;
using Microsoft.OpenApi.Models;

namespace CinefyAiServer.Controllers;

/// <summary>
/// Dashboard yönetimi API endpoint'leri (sadece Owner/Admin)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner,Admin")]
[Produces("application/json")]
[Tags("Dashboard")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Dashboard ana istatistiklerini getirir
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsResponse>> GetDashboardStats()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Owner ise sadece kendi sinemalarının verilerini getir
            var cinemaIds = userRole == "Admin" ? null :
                await _context.Cinemas
                    .Where(c => c.OwnerId == Guid.Parse(userId!) && c.IsActive)
                    .Select(c => c.Id)
                    .ToListAsync();

            // Temel istatistikler
            var bookingsQuery = _context.Bookings
                .Include(b => b.Session)
                    .ThenInclude(s => s!.Cinema)
                .Where(b => b.PaymentStatus == PaymentStatus.Completed);

            if (cinemaIds != null)
            {
                bookingsQuery = bookingsQuery.Where(b =>
                    b.Session != null && cinemaIds.Contains(b.Session.CinemaId));
            }

            var totalRevenue = await bookingsQuery.SumAsync(b => b.FinalAmount);
            var totalTickets = await bookingsQuery.SumAsync(b => b.SelectedSeats.Count);

            // Aktif sinemalar
            var activeCinemasQuery = _context.Cinemas.Where(c => c.IsActive);
            if (cinemaIds != null)
            {
                activeCinemasQuery = activeCinemasQuery.Where(c => cinemaIds.Contains(c.Id));
            }
            var activeCinemas = await activeCinemasQuery.CountAsync();

            // Ortalama doluluk oranı
            var sessionsQuery = _context.Sessions.Where(s => s.IsActive);
            if (cinemaIds != null)
            {
                sessionsQuery = sessionsQuery.Where(s => cinemaIds.Contains(s.CinemaId));
            }

            var averageOccupancy = await sessionsQuery.AverageAsync(s =>
                s.TotalSeats > 0 ? (decimal)(s.TotalSeats - s.AvailableSeats) / s.TotalSeats * 100 : 0);

            // Son rezervasyonlar
            var recentBookings = await bookingsQuery
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .Select(b => new BookingResponse
                {
                    Id = b.Id,
                    CustomerName = b.CustomerName,
                    FinalAmount = b.FinalAmount,
                    PaymentStatus = b.PaymentStatus,
                    BookingCode = b.BookingCode,
                    CreatedAt = b.CreatedAt,
                    Session = b.Session != null ? new SessionResponse
                    {
                        SessionDate = b.Session.SessionDate,
                        SessionTime = b.Session.SessionTime,
                        Movie = b.Session.Movie != null ? new MovieResponse
                        {
                            Title = b.Session.Movie.Title
                        } : null,
                        Cinema = b.Session.Cinema != null ? new CinemaResponse
                        {
                            Name = b.Session.Cinema.Name
                        } : null
                    } : null
                })
                .ToListAsync();

            // En popüler filmler
            var topMovies = await _context.Bookings
                .Include(b => b.Session)
                    .ThenInclude(s => s!.Movie)
                .Where(b => b.PaymentStatus == PaymentStatus.Completed)
                .Where(b => cinemaIds == null || (b.Session != null && cinemaIds.Contains(b.Session.CinemaId)))
                .GroupBy(b => b.Session!.Movie)
                .Select(g => new MovieStats
                {
                    MovieId = g.Key!.Id,
                    Title = g.Key.Title,
                    Poster = g.Key.Poster,
                    TotalTickets = g.Sum(b => b.SelectedSeats.Count),
                    TotalRevenue = g.Sum(b => b.FinalAmount),
                    AverageRating = g.Key.Rating,
                    SessionCount = g.Count(),
                    OccupancyRate = 0 // Hesaplanacak
                })
                .OrderByDescending(m => m.TotalTickets)
                .Take(5)
                .ToListAsync();

            // Revenue chart verileri (son 7 gün)
            var revenueChart = new List<ChartDataPoint>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                var dayRevenue = await bookingsQuery
                    .Where(b => b.CreatedAt.Date == date)
                    .SumAsync(b => b.FinalAmount);

                revenueChart.Add(new ChartDataPoint
                {
                    Label = date.ToString("dd/MM"),
                    Value = dayRevenue,
                    Date = date
                });
            }

            // Occupancy chart verileri (son 7 gün)
            var occupancyChart = new List<ChartDataPoint>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateOnly.FromDateTime(DateTime.Today.AddDays(-i));
                var dayOccupancy = await sessionsQuery
                    .Where(s => s.SessionDate == date)
                    .AverageAsync(s => s.TotalSeats > 0 ?
                        (decimal)(s.TotalSeats - s.AvailableSeats) / s.TotalSeats * 100 : 0);

                occupancyChart.Add(new ChartDataPoint
                {
                    Label = date.ToString("dd/MM"),
                    Value = dayOccupancy,
                    Date = date.ToDateTime(TimeOnly.MinValue)
                });
            }

            // Performance metrikleri
            var performance = new PerformanceMetrics
            {
                RevenueGrowth = await CalculateGrowthRate("revenue", cinemaIds),
                TicketSalesGrowth = await CalculateGrowthRate("tickets", cinemaIds),
                OccupancyGrowth = await CalculateGrowthRate("occupancy", cinemaIds),
                TopPerformingHours = new List<string> { "20:00", "18:00", "15:00" }, // Örnek veri
                TopPerformingDays = new List<string> { "Cumartesi", "Pazar", "Cuma" } // Örnek veri
            };

            var response = new DashboardStatsResponse
            {
                TotalRevenue = totalRevenue,
                TotalTickets = totalTickets,
                AverageOccupancy = averageOccupancy,
                ActiveCinemas = activeCinemas,
                RecentBookings = recentBookings,
                TopMovies = topMovies,
                RevenueChart = revenueChart,
                OccupancyChart = occupancyChart,
                Performance = performance
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard istatistikleri getirilirken hata oluştu");
            return StatusCode(500, new { message = "Dashboard istatistikleri getirilirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Detaylı analitik verilerini getirir
    /// </summary>
    [HttpGet("analytics")]
    public async Task<ActionResult<AnalyticsResponse>> GetAnalytics([FromQuery] AnalyticsQuery query)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Owner kontrolü
            if (userRole == "Owner" && query.CinemaId.HasValue)
            {
                var cinema = await _context.Cinemas.FindAsync(query.CinemaId.Value);
                if (cinema?.OwnerId != Guid.Parse(userId!))
                {
                    return Forbid("Bu sinemaya erişim yetkiniz yok");
                }
            }

            // Tarih aralığını belirle
            var (startDate, endDate) = GetDateRange(query.Period, query.StartDate, query.EndDate);

            // Analytics verileri
            var analyticsData = await GetAnalyticsData(startDate, endDate, query.CinemaId, query.GroupBy);

            // Özet bilgiler
            var summary = new AnalyticsSummary
            {
                TotalTickets = analyticsData.Sum(d => d.TicketsSold),
                TotalRevenue = analyticsData.Sum(d => d.Revenue),
                AverageOccupancy = analyticsData.Any() ? analyticsData.Average(d => d.OccupancyRate) : 0,
                AverageRevenuePerTicket = analyticsData.Sum(d => d.TicketsSold) > 0 ?
                    analyticsData.Sum(d => d.Revenue) / analyticsData.Sum(d => d.TicketsSold) : 0,
                TotalSessions = analyticsData.Sum(d => d.SessionsCount)
            };

            // Karşılaştırma metrikleri
            var comparisons = await GetComparisonMetrics(startDate, endDate, query.CinemaId);

            // Trend analizi
            var trends = GenerateTrendAnalysis(analyticsData);

            var response = new AnalyticsResponse
            {
                Data = analyticsData,
                Summary = summary,
                Comparisons = comparisons,
                Trends = trends
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analytics verileri getirilirken hata oluştu");
            return StatusCode(500, new { message = "Analytics verileri getirilirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Sinema yönetimi verilerini getirir
    /// </summary>
    [HttpGet("cinemas")]
    public async Task<ActionResult<CinemaManagementResponse>> GetCinemas()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var cinemasQuery = _context.Cinemas.AsQueryable();

            // Owner sadece kendi sinemalarını görür
            if (userRole == "Owner")
            {
                cinemasQuery = cinemasQuery.Where(c => c.OwnerId == Guid.Parse(userId!));
            }

            var cinemas = await cinemasQuery
                .Include(c => c.Halls)
                .Select(c => new CinemaResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Brand = c.Brand,
                    City = c.City,
                    District = c.District,
                    Address = c.Address,
                    Phone = c.Phone,
                    Email = c.Email,
                    Rating = c.Rating,
                    ReviewCount = c.ReviewCount,
                    Capacity = c.Capacity,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            var stats = new CinemaStats
            {
                TotalCinemas = cinemas.Count,
                ActiveCinemas = cinemas.Count(c => c.IsActive),
                AverageRating = cinemas.Any() ? cinemas.Average(c => c.Rating) : 0,
                TotalHalls = await _context.Halls
                    .Where(h => cinemasQuery.Any(c => c.Id == h.CinemaId))
                    .CountAsync(),
                TotalCapacity = cinemas.Sum(c => c.Capacity)
            };

            var response = new CinemaManagementResponse
            {
                Cinemas = cinemas,
                Stats = stats
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sinema yönetimi verileri getirilirken hata oluştu");
            return StatusCode(500, new { message = "Sinema yönetimi verileri getirilirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Salon yönetimi - yeni salon oluşturur
    /// </summary>
    [HttpPost("halls")]
    public async Task<ActionResult<HallResponse>> CreateHall(CreateHallRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Cinema ownership kontrolü
            var cinema = await _context.Cinemas.FindAsync(request.CinemaId);
            if (cinema == null)
            {
                return NotFound(new { message = "Sinema bulunamadı" });
            }

            if (userRole == "Owner" && cinema.OwnerId != Guid.Parse(userId!))
            {
                return Forbid("Bu sinemada salon oluşturma yetkiniz yok");
            }

            var hall = new Hall
            {
                Id = Guid.NewGuid(),
                CinemaId = request.CinemaId,
                Name = request.Name,
                Capacity = request.Capacity,
                Features = request.Features ?? new List<string>(),
                SeatLayout = new Dictionary<string, object>
                {
                    ["rows"] = request.SeatLayout.Rows,
                    ["createdAt"] = DateTime.UtcNow
                },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Halls.Add(hall);
            await _context.SaveChangesAsync();

            var response = new HallResponse
            {
                Id = hall.Id,
                Name = hall.Name,
                Capacity = hall.Capacity,
                Features = hall.Features,
                IsActive = hall.IsActive
            };

            return CreatedAtAction(nameof(GetHall), new { id = hall.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Salon oluşturulurken hata oluştu");
            return StatusCode(500, new { message = "Salon oluşturulurken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Salon detayını getirir
    /// </summary>
    [HttpGet("halls/{id}")]
    public async Task<ActionResult<HallResponse>> GetHall(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var hall = await _context.Halls
                .Include(h => h.Cinema)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hall == null)
            {
                return NotFound(new { message = "Salon bulunamadı" });
            }

            // Owner kontrolü
            if (userRole == "Owner" && hall.Cinema?.OwnerId != Guid.Parse(userId!))
            {
                return Forbid("Bu salonu görüntüleme yetkiniz yok");
            }

            var response = new HallResponse
            {
                Id = hall.Id,
                Name = hall.Name,
                Capacity = hall.Capacity,
                Features = hall.Features,
                IsActive = hall.IsActive
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Salon detayı getirilirken hata oluştu: {HallId}", id);
            return StatusCode(500, new { message = "Salon detayı getirilirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Revenue analizi getirir
    /// </summary>
    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueAnalysisResponse>> GetRevenueAnalysis([FromQuery] AnalyticsQuery query)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Owner kontrolü
            var cinemaIds = userRole == "Admin" ? null :
                await _context.Cinemas
                    .Where(c => c.OwnerId == Guid.Parse(userId!) && c.IsActive)
                    .Select(c => c.Id)
                    .ToListAsync();

            var (startDate, endDate) = GetDateRange(query.Period, query.StartDate, query.EndDate);

            var bookingsQuery = _context.Bookings
                .Include(b => b.Session)
                .Where(b => b.PaymentStatus == PaymentStatus.Completed &&
                           b.CreatedAt.Date >= startDate &&
                           b.CreatedAt.Date <= endDate);

            if (cinemaIds != null)
            {
                bookingsQuery = bookingsQuery.Where(b =>
                    b.Session != null && cinemaIds.Contains(b.Session.CinemaId));
            }

            // Revenue by period
            var revenueData = await bookingsQuery
                .GroupBy(b => b.CreatedAt.Date)
                .Select(g => new RevenueByPeriod
                {
                    Period = g.Key,
                    Revenue = g.Sum(b => b.FinalAmount),
                    TicketCount = g.Sum(b => b.SelectedSeats.Count),
                    AverageTicketPrice = g.Sum(b => b.FinalAmount) / g.Sum(b => b.SelectedSeats.Count)
                })
                .OrderBy(r => r.Period)
                .ToListAsync();

            // Revenue breakdown
            var breakdown = new RevenueBreakdown
            {
                StandardSeatsRevenue = await bookingsQuery
                    .SelectMany(b => b.SelectedSeats)
                    .Where(s => s.Type == SeatType.Standard)
                    .SumAsync(s => s.Price),
                VipSeatsRevenue = await bookingsQuery
                    .SelectMany(b => b.SelectedSeats)
                    .Where(s => s.Type == SeatType.Vip)
                    .SumAsync(s => s.Price),
                TotalRevenue = await bookingsQuery.SumAsync(b => b.TotalAmount),
                TotalDiscount = await bookingsQuery.SumAsync(b => b.DiscountAmount),
                NetRevenue = await bookingsQuery.SumAsync(b => b.FinalAmount)
            };

            // Payment methods
            var paymentMethods = await bookingsQuery
                .GroupBy(b => b.PaymentMethod)
                .Select(g => new PaymentMethodStats
                {
                    PaymentMethod = g.Key!,
                    Count = g.Count(),
                    Revenue = g.Sum(b => b.FinalAmount),
                    Percentage = 0 // Hesaplanacak
                })
                .ToListAsync();

            var totalRevenue = paymentMethods.Sum(p => p.Revenue);
            foreach (var pm in paymentMethods)
            {
                pm.Percentage = totalRevenue > 0 ? pm.Revenue / totalRevenue * 100 : 0;
            }

            var response = new RevenueAnalysisResponse
            {
                RevenueData = revenueData,
                Breakdown = breakdown,
                PaymentMethods = paymentMethods,
                Discounts = new List<DiscountAnalysis>() // Implement if needed
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Revenue analizi getirilirken hata oluştu");
            return StatusCode(500, new { message = "Revenue analizi getirilirken bir hata oluştu" });
        }
    }

    #region Helper Methods

    private async Task<decimal> CalculateGrowthRate(string metric, List<Guid>? cinemaIds)
    {
        // Bu ayın ilk gününden bugüne ve geçen ayın aynı dönemine göre büyüme oranı
        var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);
        var lastMonthEnd = thisMonthStart.AddDays(-1);

        var thisMonthQuery = _context.Bookings
            .Include(b => b.Session)
            .Where(b => b.PaymentStatus == PaymentStatus.Completed &&
                       b.CreatedAt >= thisMonthStart);

        var lastMonthQuery = _context.Bookings
            .Include(b => b.Session)
            .Where(b => b.PaymentStatus == PaymentStatus.Completed &&
                       b.CreatedAt >= lastMonthStart &&
                       b.CreatedAt <= lastMonthEnd);

        if (cinemaIds != null)
        {
            thisMonthQuery = thisMonthQuery.Where(b =>
                b.Session != null && cinemaIds.Contains(b.Session.CinemaId));
            lastMonthQuery = lastMonthQuery.Where(b =>
                b.Session != null && cinemaIds.Contains(b.Session.CinemaId));
        }

        decimal thisMonthValue = 0, lastMonthValue = 0;

        switch (metric)
        {
            case "revenue":
                thisMonthValue = await thisMonthQuery.SumAsync(b => b.FinalAmount);
                lastMonthValue = await lastMonthQuery.SumAsync(b => b.FinalAmount);
                break;
            case "tickets":
                thisMonthValue = await thisMonthQuery.SumAsync(b => b.SelectedSeats.Count);
                lastMonthValue = await lastMonthQuery.SumAsync(b => b.SelectedSeats.Count);
                break;
        }

        return lastMonthValue > 0 ? ((thisMonthValue - lastMonthValue) / lastMonthValue) * 100 : 0;
    }

    private (DateTime startDate, DateTime endDate) GetDateRange(string period, DateOnly? startDate, DateOnly? endDate)
    {
        if (startDate.HasValue && endDate.HasValue)
        {
            return (startDate.Value.ToDateTime(TimeOnly.MinValue), endDate.Value.ToDateTime(TimeOnly.MaxValue));
        }

        var today = DateTime.Today;
        return period.ToLower() switch
        {
            "today" => (today, today),
            "week" => (today.AddDays(-7), today),
            "month" => (today.AddDays(-30), today),
            "year" => (today.AddDays(-365), today),
            _ => (today.AddDays(-30), today)
        };
    }

    private async Task<List<AnalyticsDataPoint>> GetAnalyticsData(DateTime startDate, DateTime endDate, Guid? cinemaId, string? groupBy)
    {
        var bookingsQuery = _context.Bookings
            .Include(b => b.Session)
            .Where(b => b.PaymentStatus == PaymentStatus.Completed &&
                       b.CreatedAt.Date >= startDate.Date &&
                       b.CreatedAt.Date <= endDate.Date);

        if (cinemaId.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(b =>
                b.Session != null && b.Session.CinemaId == cinemaId.Value);
        }

        var bookings = await bookingsQuery.ToListAsync();

        return bookings
            .GroupBy(b => b.CreatedAt.Date)
            .Select(g => new AnalyticsDataPoint
            {
                Date = g.Key,
                TicketsSold = g.Sum(b => b.SelectedSeats.Count),
                Revenue = g.Sum(b => b.FinalAmount),
                OccupancyRate = 0, // Hesaplanacak
                SessionsCount = g.Select(b => b.SessionId).Distinct().Count()
            })
            .OrderBy(d => d.Date)
            .ToList();
    }

    private async Task<List<ComparisonMetric>> GetComparisonMetrics(DateTime startDate, DateTime endDate, Guid? cinemaId)
    {
        // Örnek karşılaştırma metrikleri
        return new List<ComparisonMetric>
        {
            new()
            {
                MetricName = "Revenue",
                CurrentValue = 10000,
                PreviousValue = 8000,
                ChangePercentage = 25,
                Trend = "up"
            }
        };
    }

    private static List<TrendAnalysis> GenerateTrendAnalysis(List<AnalyticsDataPoint> data)
    {
        // Örnek trend analizi
        return new List<TrendAnalysis>
        {
            new()
            {
                Category = "Revenue",
                Description = "Revenue artış eğiliminde",
                Recommendation = "Bu trendi sürdürmek için pazarlama yatırımlarını artırın",
                Impact = 8.5m
            }
        };
    }

    #endregion
}