using System.ComponentModel.DataAnnotations;

namespace CinefyAiServer.Entities.DTOs;

#region Request DTOs

public class CreateBookingRequest
{
    [Required]
    public Guid SessionId { get; set; }

    [Required]
    public List<SeatSelection> SelectedSeats { get; set; } = new();

    [Required]
    public CustomerInfo CustomerInfo { get; set; } = new();

    public string? DiscountType { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = string.Empty;
}

public class SeatSelection
{
    [Required]
    public string Row { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Number { get; set; }

    [Required]
    public SeatType Type { get; set; }

    public decimal Price { get; set; }
}

public class CustomerInfo
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }
}

public class UpdateBookingRequest
{
    public BookingStatus? Status { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public string? TransactionId { get; set; }
}

public class CancelBookingRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;
}

#endregion

#region Response DTOs

public class BookingResponse
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid? UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public List<SeatSelection> SelectedSeats { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string? DiscountType { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string? QrCode { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public SessionResponse? Session { get; set; }
    public string? PaymentUrl { get; set; }
}

public class BookingDetailResponse : BookingResponse
{
    public MovieResponse? Movie { get; set; }
    public CinemaResponse? Cinema { get; set; }
    public HallResponse? Hall { get; set; }
    public PaymentDetails? PaymentDetails { get; set; }
}

public class PaymentDetails
{
    public string? PaymentUrl { get; set; }
    public string? PaymentToken { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? PaymentProvider { get; set; }
}

public class UserBookingsResponse
{
    public List<BookingResponse> Bookings { get; set; } = new();
    public BookingStats Stats { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class BookingStats
{
    public int TotalBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public decimal TotalSpent { get; set; }
    public int UpcomingMovies { get; set; }
}

#endregion

#region Helper DTOs

/// <summary>
/// QR kod yanıt DTO'su
/// </summary>
public class QrCodeResponse
{
    public string QrCode { get; set; } = string.Empty;
    public string BookingCode { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
}

public class BookingValidationResponse
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public BookingResponse? Booking { get; set; }
}

#endregion