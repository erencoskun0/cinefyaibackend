using System.ComponentModel.DataAnnotations;

namespace CinefyAiServer.Entities.DTOs;

#region Request DTOs

public class CreateReviewRequest
{
    public Guid? CinemaId { get; set; }
    public Guid? MovieId { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Rating 1-5 arasında olmalıdır")]
    public int Rating { get; set; }

    [StringLength(1000, ErrorMessage = "Yorum maksimum 1000 karakter olabilir")]
    public string? Comment { get; set; }
}

public class UpdateReviewRequest
{
    [Range(1, 5)]
    public int? Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}

public class ReviewsQuery
{
    public Guid? CinemaId { get; set; }
    public Guid? MovieId { get; set; }
    public Guid? UserId { get; set; }
    public int? MinRating { get; set; }
    public int? MaxRating { get; set; }
    public bool? IsApproved { get; set; }
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 20;
    public string SortBy { get; set; } = "createdAt"; // createdAt, rating
    public string SortOrder { get; set; } = "desc";
}

#endregion

#region Response DTOs

public class ReviewResponse
{
    public Guid Id { get; set; }
    public Guid? CinemaId { get; set; }
    public Guid? MovieId { get; set; }
    public Guid? UserId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // User info
    public string? UserName { get; set; }
    public string? UserAvatar { get; set; }

    // Related entities
    public string? CinemaName { get; set; }
    public string? MovieTitle { get; set; }
}

public class ReviewsResponse
{
    public List<ReviewResponse> Reviews { get; set; } = new();
    public ReviewStats Stats { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class ReviewStats
{
    public int TotalReviews { get; set; }
    public decimal AverageRating { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new(); // {5: 100, 4: 80, 3: 30, 2: 10, 1: 5}
    public int PendingApproval { get; set; }
}

#endregion