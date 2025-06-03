using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CinefyAiServer.Data;
using CinefyAiServer.Entities;
using CinefyAiServer.Entities.DTOs;
using System.Security.Claims;

namespace CinefyAiServer.Controllers;

/// <summary>
/// Sinema yönetimi API endpoint'leri
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Cinemas")]
public class CinemaController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CinemaController> _logger;

    public CinemaController(ApplicationDbContext context, ILogger<CinemaController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Tüm sinemaları listeler (filtreleme ve sayfalama ile)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<CinemasResponse>> GetCinemas([FromQuery] CinemasQuery query)
    {
        try
        {
            var cinemasQueryable = _context.Cinemas
                .Where(c => c.IsActive)
                .AsQueryable();

            // Filtreleme
            if (!string.IsNullOrEmpty(query.City))
            {
                cinemasQueryable = cinemasQueryable.Where(c =>
                    c.City.ToLower().Contains(query.City.ToLower()));
            }

            if (!string.IsNullOrEmpty(query.Brand))
            {
                cinemasQueryable = cinemasQueryable.Where(c =>
                    c.Brand.ToLower().Contains(query.Brand.ToLower()));
            }

            if (query.Features != null && query.Features.Any())
            {
                foreach (var feature in query.Features)
                {
                    cinemasQueryable = cinemasQueryable.Where(c =>
                        c.Features != null && c.Features.Contains(feature));
                }
            }

            if (!string.IsNullOrEmpty(query.Search))
            {
                cinemasQueryable = cinemasQueryable.Where(c =>
                    c.Name.ToLower().Contains(query.Search.ToLower()) ||
                    c.Address.ToLower().Contains(query.Search.ToLower()) ||
                    c.District.ToLower().Contains(query.Search.ToLower()));
            }

            // Toplam sayı
            var totalCount = await cinemasQueryable.CountAsync();

            // Sıralama
            cinemasQueryable = query.SortBy.ToLower() switch
            {
                "rating" => cinemasQueryable.OrderByDescending(c => c.Rating),
                "name" => cinemasQueryable.OrderBy(c => c.Name),
                "distance" when query.UserLat.HasValue && query.UserLng.HasValue =>
                    cinemasQueryable.OrderBy(c => Math.Sqrt(
                        Math.Pow((double)(c.Latitude ?? 0) - (double)query.UserLat.Value, 2) +
                        Math.Pow((double)(c.Longitude ?? 0) - (double)query.UserLng.Value, 2))),
                _ => cinemasQueryable.OrderBy(c => c.Name)
            };

            // Sayfalama
            var cinemas = await cinemasQueryable
                .Skip((query.Page - 1) * query.Limit)
                .Take(query.Limit)
                .Select(c => new CinemaResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Brand = c.Brand,
                    Address = c.Address,
                    City = c.City,
                    District = c.District,
                    Phone = c.Phone,
                    Email = c.Email,
                    Description = c.Description,
                    Facilities = c.Facilities,
                    Features = c.Features,
                    Rating = c.Rating,
                    ReviewCount = c.ReviewCount,
                    Capacity = c.Capacity,
                    IsActive = c.IsActive,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    OpeningHours = c.OpeningHours,
                    Distance = query.UserLat.HasValue && query.UserLng.HasValue && c.Latitude.HasValue && c.Longitude.HasValue ?
                        CalculateDistance((double)query.UserLat.Value, (double)query.UserLng.Value,
                                        (double)c.Latitude.Value, (double)c.Longitude.Value) : null,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            // Filtre seçeneklerini al
            var filters = new CinemaFilters
            {
                Cities = await _context.Cinemas
                    .Where(c => c.IsActive)
                    .Select(c => c.City)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync(),

                Brands = await _context.Cinemas
                    .Where(c => c.IsActive)
                    .Select(c => c.Brand)
                    .Distinct()
                    .OrderBy(b => b)
                    .ToListAsync(),

                Features = await _context.Cinemas
                    .Where(c => c.IsActive && c.Features != null)
                    .SelectMany(c => c.Features!)
                    .Distinct()
                    .OrderBy(f => f)
                    .ToListAsync()
            };

            var response = new CinemasResponse
            {
                Cinemas = cinemas,
                Pagination = new PaginationInfo
                {
                    Page = query.Page,
                    Limit = query.Limit,
                    Total = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / query.Limit)
                },
                Filters = filters
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sinemalar listelenirken hata oluştu");
            return StatusCode(500, new { message = "Sinemalar listelenirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Belirli bir sinema detayını getirir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CinemaDetailResponse>> GetCinema(Guid id)
    {
        try
        {
            var cinema = await _context.Cinemas
                .Include(c => c.Halls)
                .Include(c => c.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (cinema == null)
            {
                return NotFound(new { message = "Sinema bulunamadı" });
            }

            // Yakın seansları al
            var upcomingSessions = await _context.Sessions
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Where(s => s.CinemaId == id && s.IsActive &&
                           s.SessionDate >= DateOnly.FromDateTime(DateTime.Today))
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.SessionTime)
                .Take(10)
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
                        Genre = s.Movie.Genre
                    } : null,
                    Hall = s.Hall != null ? new HallResponse
                    {
                        Id = s.Hall.Id,
                        Name = s.Hall.Name,
                        Capacity = s.Hall.Capacity,
                        Features = s.Hall.Features,
                        IsActive = s.Hall.IsActive
                    } : null
                })
                .ToListAsync();

            var response = new CinemaDetailResponse
            {
                Id = cinema.Id,
                Name = cinema.Name,
                Brand = cinema.Brand,
                Address = cinema.Address,
                City = cinema.City,
                District = cinema.District,
                Phone = cinema.Phone,
                Email = cinema.Email,
                Description = cinema.Description,
                Facilities = cinema.Facilities,
                Features = cinema.Features,
                Rating = cinema.Rating,
                ReviewCount = cinema.ReviewCount,
                Capacity = cinema.Capacity,
                IsActive = cinema.IsActive,
                Latitude = cinema.Latitude,
                Longitude = cinema.Longitude,
                OpeningHours = cinema.OpeningHours,
                CreatedAt = cinema.CreatedAt,
                UpdatedAt = cinema.UpdatedAt,
                Halls = cinema.Halls?.Select(h => new HallResponse
                {
                    Id = h.Id,
                    Name = h.Name,
                    Capacity = h.Capacity,
                    Features = h.Features,
                    IsActive = h.IsActive
                }).ToList() ?? new List<HallResponse>(),
                UpcomingSessions = upcomingSessions,
                Reviews = cinema.Reviews?.Where(r => r.IsApproved).Select(r => new ReviewResponse
                {
                    Id = r.Id,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    UserName = r.User?.FirstName + " " + r.User?.LastName
                }).ToList() ?? new List<ReviewResponse>()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sinema detayı getirilirken hata oluştu: {CinemaId}", id);
            return StatusCode(500, new { message = "Sinema detayı getirilirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Yeni sinema oluşturur (sadece Owner/Admin)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<CinemaResponse>> CreateCinema(CreateCinemaRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var cinema = new Cinema
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Brand = request.Brand,
                Address = request.Address,
                City = request.City,
                District = request.District,
                Phone = request.Phone,
                Email = request.Email,
                Description = request.Description,
                Facilities = request.Facilities,
                Features = request.Features,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                OpeningHours = request.OpeningHours,
                OwnerId = userRole == "Admin" ? null : Guid.Parse(userId!),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Cinemas.Add(cinema);
            await _context.SaveChangesAsync();

            var response = new CinemaResponse
            {
                Id = cinema.Id,
                Name = cinema.Name,
                Brand = cinema.Brand,
                Address = cinema.Address,
                City = cinema.City,
                District = cinema.District,
                Phone = cinema.Phone,
                Email = cinema.Email,
                Description = cinema.Description,
                Facilities = cinema.Facilities,
                Features = cinema.Features,
                Rating = cinema.Rating,
                ReviewCount = cinema.ReviewCount,
                Capacity = cinema.Capacity,
                IsActive = cinema.IsActive,
                Latitude = cinema.Latitude,
                Longitude = cinema.Longitude,
                OpeningHours = cinema.OpeningHours,
                CreatedAt = cinema.CreatedAt,
                UpdatedAt = cinema.UpdatedAt
            };

            return CreatedAtAction(nameof(GetCinema), new { id = cinema.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sinema oluşturulurken hata oluştu");
            return StatusCode(500, new { message = "Sinema oluşturulurken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Sinema günceller (sadece Owner/Admin)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<CinemaResponse>> UpdateCinema(Guid id, UpdateCinemaRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var cinema = await _context.Cinemas.FindAsync(id);
            if (cinema == null)
            {
                return NotFound(new { message = "Sinema bulunamadı" });
            }

            // Owner kontrolü
            if (userRole == "Owner" && cinema.OwnerId != Guid.Parse(userId!))
            {
                return Forbid("Bu sinemayı güncelleme yetkiniz yok");
            }

            // Güncelleme işlemleri
            if (!string.IsNullOrEmpty(request.Name))
                cinema.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Brand))
                cinema.Brand = request.Brand;
            if (!string.IsNullOrEmpty(request.Address))
                cinema.Address = request.Address;
            if (!string.IsNullOrEmpty(request.City))
                cinema.City = request.City;
            if (!string.IsNullOrEmpty(request.District))
                cinema.District = request.District;
            if (request.Phone != null)
                cinema.Phone = request.Phone;
            if (request.Email != null)
                cinema.Email = request.Email;
            if (request.Description != null)
                cinema.Description = request.Description;
            if (request.Facilities != null)
                cinema.Facilities = request.Facilities;
            if (request.Features != null)
                cinema.Features = request.Features;
            if (request.Latitude.HasValue)
                cinema.Latitude = request.Latitude;
            if (request.Longitude.HasValue)
                cinema.Longitude = request.Longitude;
            if (request.OpeningHours != null)
                cinema.OpeningHours = request.OpeningHours;

            cinema.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new CinemaResponse
            {
                Id = cinema.Id,
                Name = cinema.Name,
                Brand = cinema.Brand,
                Address = cinema.Address,
                City = cinema.City,
                District = cinema.District,
                Phone = cinema.Phone,
                Email = cinema.Email,
                Description = cinema.Description,
                Facilities = cinema.Facilities,
                Features = cinema.Features,
                Rating = cinema.Rating,
                ReviewCount = cinema.ReviewCount,
                Capacity = cinema.Capacity,
                IsActive = cinema.IsActive,
                Latitude = cinema.Latitude,
                Longitude = cinema.Longitude,
                OpeningHours = cinema.OpeningHours,
                CreatedAt = cinema.CreatedAt,
                UpdatedAt = cinema.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sinema güncellenirken hata oluştu: {CinemaId}", id);
            return StatusCode(500, new { message = "Sinema güncellenirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Sinema siler (sadece Owner/Admin)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> DeleteCinema(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var cinema = await _context.Cinemas.FindAsync(id);
            if (cinema == null)
            {
                return NotFound(new { message = "Sinema bulunamadı" });
            }

            // Owner kontrolü
            if (userRole == "Owner" && cinema.OwnerId != Guid.Parse(userId!))
            {
                return Forbid("Bu sinemayı silme yetkiniz yok");
            }

            // Soft delete
            cinema.IsActive = false;
            cinema.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sinema silinirken hata oluştu: {CinemaId}", id);
            return StatusCode(500, new { message = "Sinema silinirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Mesafe hesaplama yardımcı metodu (Haversine formülü)
    /// </summary>
    private static double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371; // Dünya'nın yarıçapı (km)
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180);
}