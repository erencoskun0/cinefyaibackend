using System.ComponentModel.DataAnnotations;

namespace CinefyAiServer.Entities.DTOs
{
    /// <summary>
    /// Film oluşturma DTO'su
    /// </summary>
    public class CreateMovieDto
    {
        [Required(ErrorMessage = "Film adı zorunludur")]
        [StringLength(255, ErrorMessage = "Film adı en fazla 255 karakter olabilir")]
        public required string Title { get; set; }

        public string? Description { get; set; }
        public string? Poster { get; set; }
        public string? Backdrop { get; set; }
        public string? TrailerUrl { get; set; }

        public List<string> Genre { get; set; } = new();

        [Required(ErrorMessage = "Film süresi zorunludur")]
        [Range(1, 600, ErrorMessage = "Film süresi 1-600 dakika arasında olmalıdır")]
        public int Duration { get; set; }

        [Range(0, 10, ErrorMessage = "Rating 0-10 arasında olmalıdır")]
        public decimal Rating { get; set; } = 0;

        [Required(ErrorMessage = "Vizyona giriş tarihi zorunludur")]
        public DateTime ReleaseDate { get; set; }

        [StringLength(255, ErrorMessage = "Yönetmen adı en fazla 255 karakter olabilir")]
        public string? Director { get; set; }

        public List<string> Cast { get; set; } = new();

        [StringLength(10, ErrorMessage = "Yaş sınırı en fazla 10 karakter olabilir")]
        public string? AgeRating { get; set; }

        public bool IsPopular { get; set; } = false;
        public bool IsNew { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Film güncelleme DTO'su
    /// </summary>
    public class UpdateMovieDto : CreateMovieDto
    {
        // Inherit all properties from CreateMovieDto
    }

    /// <summary>
    /// Film liste DTO'su
    /// </summary>
    public class MovieListDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Poster { get; set; }
        public string? Backdrop { get; set; }
        public string? TrailerUrl { get; set; }
        public List<string> Genre { get; set; } = new();
        public int Duration { get; set; }
        public decimal Rating { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string? Director { get; set; }
        public List<string> Cast { get; set; } = new();
        public string? AgeRating { get; set; }
        public bool IsPopular { get; set; }
        public bool IsNew { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Computed properties
        public int ReviewCount { get; set; }
        public bool IsComingSoon => ReleaseDate > DateTime.Now;
        public bool IsCurrentlyShowing { get; set; }
    }

    /// <summary>
    /// Film detay DTO'su
    /// </summary>
    public class MovieDetailDto : MovieListDto
    {
        public List<SessionListDto> Sessions { get; set; } = new();
        public List<ReviewDto> Reviews { get; set; } = new();
        public List<CinemaListDto> Cinemas { get; set; } = new();
        public List<MovieListDto> SimilarMovies { get; set; } = new();
    }

    /// <summary>
    /// Film sorgu parametreleri
    /// </summary>
    public class MovieQueryParams : SearchParams
    {
        public string? Genre { get; set; }
        public string? City { get; set; }
        public DateTime? ReleaseYear { get; set; }
        public string? AgeRating { get; set; }
        public bool? IsPopular { get; set; }
        public bool? IsNew { get; set; }
        public bool? IsActive { get; set; }
        public new string? SortBy { get; set; } = "title";
    }

    /// <summary>
    /// Session için basit DTO (forward reference için)
    /// </summary>
    public class SessionListDto
    {
        public Guid Id { get; set; }
        public DateOnly SessionDate { get; set; }
        public TimeOnly SessionTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string HallName { get; set; } = string.Empty;
        public string CinemaName { get; set; } = string.Empty;
        public decimal StandardPrice { get; set; }
        public decimal VipPrice { get; set; }
        public int AvailableSeats { get; set; }
        public OccupancyStatus OccupancyStatus { get; set; }
    }

    /// <summary>
    /// Cinema için basit DTO (forward reference için)
    /// </summary>
    public class CinemaListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new();
        public decimal Rating { get; set; }
    }

    /// <summary>
    /// Review için basit DTO (forward reference için)
    /// </summary>
    public class ReviewDto
    {
        public Guid Id { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsApproved { get; set; }
    }

    #region Request DTOs

    /// <summary>
    /// Film oluşturma DTO'su
    /// </summary>
    public class CreateMovieRequest
    {
        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? Poster { get; set; }
        public string? Backdrop { get; set; }
        public string? TrailerUrl { get; set; }

        public List<string>? Genre { get; set; }

        [Required]
        [Range(1, 600)]
        public int Duration { get; set; }

        [Required]
        public DateOnly ReleaseDate { get; set; }

        public string? Director { get; set; }
        public List<string>? Cast { get; set; }
        public string? AgeRating { get; set; }
    }

    /// <summary>
    /// Film güncelleme DTO'su
    /// </summary>
    public class UpdateMovieRequest
    {
        [StringLength(255)]
        public string? Title { get; set; }

        public string? Description { get; set; }
        public string? Poster { get; set; }
        public string? Backdrop { get; set; }
        public string? TrailerUrl { get; set; }
        public List<string>? Genre { get; set; }

        [Range(1, 600)]
        public int? Duration { get; set; }

        public DateOnly? ReleaseDate { get; set; }
        public string? Director { get; set; }
        public List<string>? Cast { get; set; }
        public string? AgeRating { get; set; }
        public bool? IsPopular { get; set; }
    }

    /// <summary>
    /// Film sorgu parametreleri
    /// </summary>
    public class MoviesQuery
    {
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 10;
        public string? Genre { get; set; }
        public string? City { get; set; }
        public string? Search { get; set; }
        public string SortBy { get; set; } = "popularity"; // popularity, rating, title, releaseDate
        public string SortOrder { get; set; } = "desc"; // asc, desc
    }

    #endregion

    #region Response DTOs

    /// <summary>
    /// Film yanıt DTO'su
    /// </summary>
    public class MovieResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Poster { get; set; }
        public string? Backdrop { get; set; }
        public string? TrailerUrl { get; set; }
        public List<string>? Genre { get; set; }
        public int Duration { get; set; }
        public decimal Rating { get; set; }
        public DateOnly ReleaseDate { get; set; }
        public string? Director { get; set; }
        public List<string>? Cast { get; set; }
        public string? AgeRating { get; set; }
        public bool IsPopular { get; set; }
        public bool IsNew { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Film listesi yanıt DTO'su
    /// </summary>
    public class MoviesResponse
    {
        public List<MovieResponse> Movies { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
        public MoviesFilters Filters { get; set; } = new();
    }

    /// <summary>
    /// Film filtreleri
    /// </summary>
    public class MoviesFilters
    {
        public List<string> Genres { get; set; } = new();
        public List<string> Cities { get; set; } = new();
        public List<string> Cinemas { get; set; } = new();
    }

    /// <summary>
    /// Film detay yanıt DTO'su
    /// </summary>
    public class MovieDetailResponse : MovieResponse
    {
        public List<SessionResponse> UpcomingSessions { get; set; } = new();
        public List<ReviewResponse> Reviews { get; set; } = new();
        public Dictionary<string, List<SessionResponse>> SessionsGroupedByCity { get; set; } = new();
    }

    #endregion
}