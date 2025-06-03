using System.ComponentModel.DataAnnotations;

namespace CinefyAiServer.Entities.DTOs;

#region Dashboard Stats DTOs

public class DashboardStatsResponse
{
    public decimal TotalRevenue { get; set; }
    public int TotalTickets { get; set; }
    public decimal AverageOccupancy { get; set; }
    public int ActiveCinemas { get; set; }
    public List<BookingResponse> RecentBookings { get; set; } = new();
    public List<MovieStats> TopMovies { get; set; } = new();
    public List<ChartDataPoint> RevenueChart { get; set; } = new();
    public List<ChartDataPoint> OccupancyChart { get; set; } = new();
    public PerformanceMetrics Performance { get; set; } = new();
}

public class MovieStats
{
    public Guid MovieId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Poster { get; set; }
    public int TotalTickets { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageRating { get; set; }
    public int SessionCount { get; set; }
    public decimal OccupancyRate { get; set; }
}

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime Date { get; set; }
    public string? Category { get; set; }
}

public class PerformanceMetrics
{
    public decimal RevenueGrowth { get; set; } // Önceki döneme göre %
    public decimal TicketSalesGrowth { get; set; } // Önceki döneme göre %
    public decimal OccupancyGrowth { get; set; } // Önceki döneme göre %
    public List<string> TopPerformingHours { get; set; } = new();
    public List<string> TopPerformingDays { get; set; } = new();
}

#endregion

#region Analytics DTOs

public class AnalyticsQuery
{
    public string Period { get; set; } = "month"; // today, week, month, year
    public Guid? CinemaId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? GroupBy { get; set; } // day, week, month, hour
}

public class AnalyticsResponse
{
    public List<AnalyticsDataPoint> Data { get; set; } = new();
    public AnalyticsSummary Summary { get; set; } = new();
    public List<ComparisonMetric> Comparisons { get; set; } = new();
    public List<TrendAnalysis> Trends { get; set; } = new();
}

public class AnalyticsDataPoint
{
    public DateTime Date { get; set; }
    public int TicketsSold { get; set; }
    public decimal Revenue { get; set; }
    public decimal OccupancyRate { get; set; }
    public int SessionsCount { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}

public class AnalyticsSummary
{
    public int TotalTickets { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOccupancy { get; set; }
    public decimal AverageRevenuePerTicket { get; set; }
    public int TotalSessions { get; set; }
    public List<string> BestPerformingMovies { get; set; } = new();
    public List<string> BestPerformingCinemas { get; set; } = new();
}

public class ComparisonMetric
{
    public string MetricName { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal ChangePercentage { get; set; }
    public string Trend { get; set; } = string.Empty; // up, down, stable
}

public class TrendAnalysis
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public decimal Impact { get; set; }
}

#endregion

#region Cinema Management DTOs

public class CinemaManagementResponse
{
    public List<CinemaResponse> Cinemas { get; set; } = new();
    public CinemaStats Stats { get; set; } = new();
}

public class CinemaStats
{
    public int TotalCinemas { get; set; }
    public int ActiveCinemas { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalHalls { get; set; }
    public int TotalCapacity { get; set; }
}

#endregion

#region Hall Management DTOs

public class CreateHallRequest
{
    [Required]
    public Guid CinemaId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(1, 1000)]
    public int Capacity { get; set; }

    public List<string>? Features { get; set; }

    [Required]
    public SeatLayoutRequest SeatLayout { get; set; } = new();
}

public class UpdateHallRequest
{
    [StringLength(100)]
    public string? Name { get; set; }

    [Range(1, 1000)]
    public int? Capacity { get; set; }

    public List<string>? Features { get; set; }

    public SeatLayoutRequest? SeatLayout { get; set; }
}

public class SeatLayoutRequest
{
    [Required]
    public List<SeatRowRequest> Rows { get; set; } = new();
}

public class SeatRowRequest
{
    [Required]
    public string RowLetter { get; set; } = string.Empty;

    [Required]
    public List<SeatRequest> Seats { get; set; } = new();
}

public class SeatRequest
{
    [Required]
    [Range(1, 100)]
    public int Number { get; set; }

    [Required]
    public SeatType Type { get; set; }
}

#endregion

#region Revenue & Financial DTOs

public class RevenueAnalysisResponse
{
    public List<RevenueByPeriod> RevenueData { get; set; } = new();
    public RevenueBreakdown Breakdown { get; set; } = new();
    public List<PaymentMethodStats> PaymentMethods { get; set; } = new();
    public List<DiscountAnalysis> Discounts { get; set; } = new();
}

public class RevenueByPeriod
{
    public DateTime Period { get; set; }
    public decimal Revenue { get; set; }
    public int TicketCount { get; set; }
    public decimal AverageTicketPrice { get; set; }
}

public class RevenueBreakdown
{
    public decimal StandardSeatsRevenue { get; set; }
    public decimal VipSeatsRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal NetRevenue { get; set; }
}

public class PaymentMethodStats
{
    public string PaymentMethod { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Revenue { get; set; }
    public decimal Percentage { get; set; }
}

public class DiscountAnalysis
{
    public string DiscountType { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal AverageDiscount { get; set; }
}

#endregion