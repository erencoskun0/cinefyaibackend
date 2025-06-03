using System.ComponentModel.DataAnnotations;

namespace CinefyAiServer.Entities.DTOs;

#region Request DTOs

public class CreateCinemaRequest
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Brand { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string District { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? Description { get; set; }

    public List<string>? Facilities { get; set; }

    public List<string>? Features { get; set; }

    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Range(-180, 180)]
    public decimal? Longitude { get; set; }

    public Dictionary<string, string>? OpeningHours { get; set; }
}

public class UpdateCinemaRequest
{
    [StringLength(255)]
    public string? Name { get; set; }

    [StringLength(100)]
    public string? Brand { get; set; }

    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? District { get; set; }

    [Phone]
    public string? Phone { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? Description { get; set; }

    public List<string>? Facilities { get; set; }

    public List<string>? Features { get; set; }

    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Range(-180, 180)]
    public decimal? Longitude { get; set; }

    public Dictionary<string, string>? OpeningHours { get; set; }
}

public class CinemasQuery
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;
    public string? City { get; set; }
    public string? Brand { get; set; }
    public List<string>? Features { get; set; }
    public string? Search { get; set; }
    public string SortBy { get; set; } = "name"; // distance, rating, name
    public decimal? UserLat { get; set; }
    public decimal? UserLng { get; set; }
}

#endregion

#region Response DTOs

public class CinemaResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Description { get; set; }
    public List<string>? Facilities { get; set; }
    public List<string>? Features { get; set; }
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public Dictionary<string, string>? OpeningHours { get; set; }
    public double? Distance { get; set; } // km cinsinden
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CinemasResponse
{
    public List<CinemaResponse> Cinemas { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
    public CinemaFilters Filters { get; set; } = new();
}

public class CinemaFilters
{
    public List<string> Cities { get; set; } = new();
    public List<string> Brands { get; set; } = new();
    public List<string> Features { get; set; } = new();
}

public class CinemaDetailResponse : CinemaResponse
{
    public List<MovieResponse> Movies { get; set; } = new();
    public List<HallResponse> Halls { get; set; } = new();
    public List<SessionResponse> UpcomingSessions { get; set; } = new();
    public List<ReviewResponse> Reviews { get; set; } = new();
}

#endregion