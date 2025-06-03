using System.ComponentModel.DataAnnotations;

namespace CinefyAiServer.Entities.DTOs;

#region Request DTOs

public class CreateSessionRequest
{
    [Required]
    public Guid MovieId { get; set; }

    [Required]
    public Guid HallId { get; set; }

    [Required]
    public Guid CinemaId { get; set; }

    [Required]
    public DateOnly SessionDate { get; set; }

    [Required]
    public TimeOnly SessionTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Standart fiyat 0'dan büyük olmalıdır")]
    public decimal StandardPrice { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "VIP fiyat 0'dan büyük olmalıdır")]
    public decimal VipPrice { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Toplam koltuk sayısı 1'den büyük olmalıdır")]
    public int TotalSeats { get; set; }
}

public class UpdateSessionRequest
{
    public DateOnly? SessionDate { get; set; }
    public TimeOnly? SessionTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal? StandardPrice { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal? VipPrice { get; set; }
}

public class SessionsQuery
{
    public Guid? CinemaId { get; set; }
    public Guid? MovieId { get; set; }
    public DateOnly? Date { get; set; }
    public string? HallType { get; set; } // "IMAX", "4DX", "VIP"
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 20;
}

#endregion

#region Response DTOs

public class SessionResponse
{
    public Guid Id { get; set; }
    public Guid MovieId { get; set; }
    public Guid HallId { get; set; }
    public Guid CinemaId { get; set; }
    public DateOnly SessionDate { get; set; }
    public TimeOnly SessionTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal StandardPrice { get; set; }
    public decimal VipPrice { get; set; }
    public int AvailableSeats { get; set; }
    public int TotalSeats { get; set; }
    public OccupancyStatus OccupancyStatus { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties için response objeler
    public MovieResponse? Movie { get; set; }
    public HallResponse? Hall { get; set; }
    public CinemaResponse? Cinema { get; set; }
}

public class SessionsResponse
{
    public List<SessionResponse> Sessions { get; set; } = new();
    public Dictionary<string, List<SessionResponse>> GroupedByHall { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

/// <summary>
/// Salon yanıt DTO'su
/// </summary>
public class HallResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public List<string> Features { get; set; } = new();
    public bool IsActive { get; set; }
    public SeatLayoutResponse? SeatLayout { get; set; }
}

public class SeatLayoutResponse
{
    public List<SeatRowResponse> Rows { get; set; } = new();
    public SeatPrices Prices { get; set; } = new();
}

public class SeatRowResponse
{
    public string RowLetter { get; set; } = string.Empty;
    public List<SeatResponse> Seats { get; set; } = new();
}

public class SeatResponse
{
    public int Number { get; set; }
    public SeatType Type { get; set; }
    public bool IsOccupied { get; set; }
    public bool IsReserved { get; set; }
    public string SeatCode => $"{RowLetter}{Number}";
    public string RowLetter { get; set; } = string.Empty;
}

public class SeatPrices
{
    public decimal Standard { get; set; }
    public decimal Vip { get; set; }
}

public class SeatsResponse
{
    public SeatLayoutResponse Layout { get; set; } = new();
    public List<string> OccupiedSeats { get; set; } = new(); // ["A1", "A2", "B5"]
    public SeatPrices Prices { get; set; } = new();
}

#endregion