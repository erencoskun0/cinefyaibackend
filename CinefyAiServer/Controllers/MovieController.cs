using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CinefyAiServer.Data;
using CinefyAiServer.Entities;
using CinefyAiServer.Entities.DTOs;
using System.Security.Claims;

namespace CinefyAiServer.Controllers;

/// <summary>
/// Film yönetimi API endpoint'leri
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Movies")]
public class MovieController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MovieController> _logger;

    public MovieController(ApplicationDbContext context, ILogger<MovieController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Tüm filmleri listeler (filtreleme ve sayfalama ile)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<MoviesResponse>> GetMovies([FromQuery] MoviesQuery query)
    {
        try
        {
            var moviesQueryable = _context.Movies
                .Where(m => m.IsActive)
                .AsQueryable();

            // Filtreleme
            if (!string.IsNullOrEmpty(query.Genre))
            {
                moviesQueryable = moviesQueryable.Where(m =>
                    m.Genre != null && m.Genre.Contains(query.Genre));
            }

            if (!string.IsNullOrEmpty(query.City))
            {
                // Şehirde gösterilen filmleri bul
                var cityMovieIds = await _context.Sessions
                    .Include(s => s.Cinema)
                    .Where(s => s.Cinema!.City.ToLower().Contains(query.City.ToLower()) &&
                               s.IsActive &&
                               s.SessionDate >= DateOnly.FromDateTime(DateTime.Today))
                    .Select(s => s.MovieId)
                    .Distinct()
                    .ToListAsync();

                moviesQueryable = moviesQueryable.Where(m => cityMovieIds.Contains(m.Id));
            }

            if (!string.IsNullOrEmpty(query.Search))
            {
                moviesQueryable = moviesQueryable.Where(m =>
                    m.Title.ToLower().Contains(query.Search.ToLower()) ||
                    (m.Description != null && m.Description.ToLower().Contains(query.Search.ToLower())) ||
                    (m.Director != null && m.Director.ToLower().Contains(query.Search.ToLower())));
            }

            // Toplam sayı
            var totalCount = await moviesQueryable.CountAsync();

            // Sıralama
            moviesQueryable = query.SortBy.ToLower() switch
            {
                "popularity" => query.SortOrder == "desc" ?
                    moviesQueryable.OrderByDescending(m => m.IsPopular).ThenByDescending(m => m.Rating) :
                    moviesQueryable.OrderBy(m => m.IsPopular).ThenBy(m => m.Rating),
                "rating" => query.SortOrder == "desc" ?
                    moviesQueryable.OrderByDescending(m => m.Rating) :
                    moviesQueryable.OrderBy(m => m.Rating),
                "title" => query.SortOrder == "desc" ?
                    moviesQueryable.OrderByDescending(m => m.Title) :
                    moviesQueryable.OrderBy(m => m.Title),
                "releasedate" => query.SortOrder == "desc" ?
                    moviesQueryable.OrderByDescending(m => m.ReleaseDate) :
                    moviesQueryable.OrderBy(m => m.ReleaseDate),
                _ => moviesQueryable.OrderByDescending(m => m.IsPopular).ThenByDescending(m => m.Rating)
            };

            // Sayfalama
            var movies = await moviesQueryable
                .Skip((query.Page - 1) * query.Limit)
                .Take(query.Limit)
                .Select(m => new MovieResponse
                {
                    Id = m.Id,
                    Title = m.Title,
                    Description = m.Description,
                    Poster = m.Poster,
                    Backdrop = m.Backdrop,
                    TrailerUrl = m.TrailerUrl,
                    Genre = m.Genre,
                    Duration = m.Duration,
                    Rating = m.Rating,
                    ReleaseDate = m.ReleaseDate,
                    Director = m.Director,
                    Cast = m.Cast,
                    AgeRating = m.AgeRating,
                    IsPopular = m.IsPopular,
                    IsNew = m.IsNew,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync();

            // Filtre seçeneklerini al
            var filters = new MoviesFilters
            {
                Genres = await _context.Movies
                    .Where(m => m.IsActive && m.Genre != null)
                    .SelectMany(m => m.Genre!)
                    .Distinct()
                    .OrderBy(g => g)
                    .ToListAsync(),

                Cities = await _context.Sessions
                    .Include(s => s.Cinema)
                    .Where(s => s.IsActive && s.SessionDate >= DateOnly.FromDateTime(DateTime.Today))
                    .Select(s => s.Cinema!.City)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync(),

                Cinemas = await _context.Sessions
                    .Include(s => s.Cinema)
                    .Where(s => s.IsActive && s.SessionDate >= DateOnly.FromDateTime(DateTime.Today))
                    .Select(s => new { s.Cinema!.Id, s.Cinema.Name })
                    .Distinct()
                    .OrderBy(c => c.Name)
                    .Select(c => c.Name)
                    .ToListAsync()
            };

            var response = new MoviesResponse
            {
                Movies = movies,
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
            _logger.LogError(ex, "Filmler listelenirken hata oluştu");
            return StatusCode(500, new { message = "Filmler listelenirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Belirli bir film detayını getirir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MovieDetailResponse>> GetMovie(Guid id)
    {
        try
        {
            var movie = await _context.Movies
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (movie == null)
            {
                return NotFound(new { message = "Film bulunamadı" });
            }

            // Bu film için yakın seansları al
            var upcomingSessions = await _context.Sessions
                .Include(s => s.Hall)
                .Include(s => s.Cinema)
                .Where(s => s.MovieId == id && s.IsActive &&
                           s.SessionDate >= DateOnly.FromDateTime(DateTime.Today))
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.SessionTime)
                .Take(20)
                .Select(s => new SessionResponse
                {
                    Id = s.Id,
                    SessionDate = s.SessionDate,
                    SessionTime = s.SessionTime,
                    EndTime = s.EndTime,
                    StandardPrice = s.StandardPrice,
                    VipPrice = s.VipPrice,
                    AvailableSeats = s.AvailableSeats,
                    TotalSeats = s.TotalSeats,
                    OccupancyStatus = s.OccupancyStatus,
                    Hall = s.Hall != null ? new HallResponse
                    {
                        Id = s.Hall.Id,
                        Name = s.Hall.Name,
                        Features = s.Hall.Features
                    } : null,
                    Cinema = s.Cinema != null ? new CinemaResponse
                    {
                        Id = s.Cinema.Id,
                        Name = s.Cinema.Name,
                        City = s.Cinema.City,
                        District = s.Cinema.District,
                        Address = s.Cinema.Address,
                        Phone = s.Cinema.Phone
                    } : null
                })
                .ToListAsync();

            // Film yorumlarını al
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.MovieId == id && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .Select(r => new ReviewResponse
                {
                    Id = r.Id,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : "Anonim"
                })
                .ToListAsync();

            var response = new MovieDetailResponse
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                Poster = movie.Poster,
                Backdrop = movie.Backdrop,
                TrailerUrl = movie.TrailerUrl,
                Genre = movie.Genre,
                Duration = movie.Duration,
                Rating = movie.Rating,
                ReleaseDate = movie.ReleaseDate,
                Director = movie.Director,
                Cast = movie.Cast,
                AgeRating = movie.AgeRating,
                IsPopular = movie.IsPopular,
                IsNew = movie.IsNew,
                IsActive = movie.IsActive,
                CreatedAt = movie.CreatedAt,
                UpdatedAt = movie.UpdatedAt,
                UpcomingSessions = upcomingSessions,
                Reviews = reviews,
                SessionsGroupedByCity = upcomingSessions
                    .GroupBy(s => s.Cinema?.City ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.ToList())
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Film detayı getirilirken hata oluştu: {MovieId}", id);
            return StatusCode(500, new { message = "Film detayı getirilirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Yeni film oluşturur (sadece Owner/Admin)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<MovieResponse>> CreateMovie(CreateMovieRequest request)
    {
        try
        {
            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                Poster = request.Poster,
                Backdrop = request.Backdrop,
                TrailerUrl = request.TrailerUrl,
                Genre = request.Genre,
                Duration = request.Duration,
                ReleaseDate = request.ReleaseDate,
                Director = request.Director,
                Cast = request.Cast,
                AgeRating = request.AgeRating,
                IsPopular = false,
                IsNew = request.ReleaseDate > DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
                IsActive = true,
                Rating = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            var response = new MovieResponse
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                Poster = movie.Poster,
                Backdrop = movie.Backdrop,
                TrailerUrl = movie.TrailerUrl,
                Genre = movie.Genre,
                Duration = movie.Duration,
                Rating = movie.Rating,
                ReleaseDate = movie.ReleaseDate,
                Director = movie.Director,
                Cast = movie.Cast,
                AgeRating = movie.AgeRating,
                IsPopular = movie.IsPopular,
                IsNew = movie.IsNew,
                IsActive = movie.IsActive,
                CreatedAt = movie.CreatedAt,
                UpdatedAt = movie.UpdatedAt
            };

            return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Film oluşturulurken hata oluştu");
            return StatusCode(500, new { message = "Film oluşturulurken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Film günceller (sadece Owner/Admin)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<MovieResponse>> UpdateMovie(Guid id, UpdateMovieRequest request)
    {
        try
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
            {
                return NotFound(new { message = "Film bulunamadı" });
            }

            // Güncelleme işlemleri
            if (!string.IsNullOrEmpty(request.Title))
                movie.Title = request.Title;
            if (request.Description != null)
                movie.Description = request.Description;
            if (request.Poster != null)
                movie.Poster = request.Poster;
            if (request.Backdrop != null)
                movie.Backdrop = request.Backdrop;
            if (request.TrailerUrl != null)
                movie.TrailerUrl = request.TrailerUrl;
            if (request.Genre != null)
                movie.Genre = request.Genre;
            if (request.Duration.HasValue)
                movie.Duration = request.Duration.Value;
            if (request.ReleaseDate.HasValue)
                movie.ReleaseDate = request.ReleaseDate.Value;
            if (request.Director != null)
                movie.Director = request.Director;
            if (request.Cast != null)
                movie.Cast = request.Cast;
            if (request.AgeRating != null)
                movie.AgeRating = request.AgeRating;
            if (request.IsPopular.HasValue)
                movie.IsPopular = request.IsPopular.Value;

            movie.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new MovieResponse
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                Poster = movie.Poster,
                Backdrop = movie.Backdrop,
                TrailerUrl = movie.TrailerUrl,
                Genre = movie.Genre,
                Duration = movie.Duration,
                Rating = movie.Rating,
                ReleaseDate = movie.ReleaseDate,
                Director = movie.Director,
                Cast = movie.Cast,
                AgeRating = movie.AgeRating,
                IsPopular = movie.IsPopular,
                IsNew = movie.IsNew,
                IsActive = movie.IsActive,
                CreatedAt = movie.CreatedAt,
                UpdatedAt = movie.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Film güncellenirken hata oluştu: {MovieId}", id);
            return StatusCode(500, new { message = "Film güncellenirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Film siler (sadece Owner/Admin)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> DeleteMovie(Guid id)
    {
        try
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
            {
                return NotFound(new { message = "Film bulunamadı" });
            }

            // Aktif seansları kontrol et
            var hasActiveSessions = await _context.Sessions
                .AnyAsync(s => s.MovieId == id && s.IsActive &&
                              s.SessionDate >= DateOnly.FromDateTime(DateTime.Today));

            if (hasActiveSessions)
            {
                return BadRequest(new { message = "Aktif seansları olan film silinemez" });
            }

            // Soft delete
            movie.IsActive = false;
            movie.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Film silinirken hata oluştu: {MovieId}", id);
            return StatusCode(500, new { message = "Film silinirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Popüler filmleri getirir
    /// </summary>
    [HttpGet("popular")]
    public async Task<ActionResult<List<MovieResponse>>> GetPopularMovies([FromQuery] int limit = 10)
    {
        try
        {
            var popularMovies = await _context.Movies
                .Where(m => m.IsActive && m.IsPopular)
                .OrderByDescending(m => m.Rating)
                .Take(limit)
                .Select(m => new MovieResponse
                {
                    Id = m.Id,
                    Title = m.Title,
                    Poster = m.Poster,
                    Genre = m.Genre,
                    Duration = m.Duration,
                    Rating = m.Rating,
                    ReleaseDate = m.ReleaseDate,
                    IsPopular = m.IsPopular,
                    IsNew = m.IsNew
                })
                .ToListAsync();

            return Ok(popularMovies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Popüler filmler getirilirken hata oluştu");
            return StatusCode(500, new { message = "Popüler filmler getirilirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Yeni filmleri getirir
    /// </summary>
    [HttpGet("new")]
    public async Task<ActionResult<List<MovieResponse>>> GetNewMovies([FromQuery] int limit = 10)
    {
        try
        {
            var newMovies = await _context.Movies
                .Where(m => m.IsActive && m.IsNew)
                .OrderByDescending(m => m.ReleaseDate)
                .Take(limit)
                .Select(m => new MovieResponse
                {
                    Id = m.Id,
                    Title = m.Title,
                    Poster = m.Poster,
                    Genre = m.Genre,
                    Duration = m.Duration,
                    Rating = m.Rating,
                    ReleaseDate = m.ReleaseDate,
                    IsPopular = m.IsPopular,
                    IsNew = m.IsNew
                })
                .ToListAsync();

            return Ok(newMovies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yeni filmler getirilirken hata oluştu");
            return StatusCode(500, new { message = "Yeni filmler getirilirken bir hata oluştu" });
        }
    }

    /// <summary>
    /// Film türlerini getirir
    /// </summary>
    [HttpGet("genres")]
    public async Task<ActionResult<List<string>>> GetGenres()
    {
        try
        {
            var genres = await _context.Movies
                .Where(m => m.IsActive && m.Genre != null)
                .SelectMany(m => m.Genre!)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            return Ok(genres);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Film türleri getirilirken hata oluştu");
            return StatusCode(500, new { message = "Film türleri getirilirken bir hata oluştu" });
        }
    }
}