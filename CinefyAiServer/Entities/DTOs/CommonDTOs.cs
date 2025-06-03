namespace CinefyAiServer.Entities.DTOs
{
    /// <summary>
    /// API yanıt wrapper'ı
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new();

        public static ApiResponse<T> SuccessResult(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        public static ApiResponse<T> ErrorResult(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }

    /// <summary>
    /// Sayfalama bilgileri
    /// </summary>
    public class PaginationInfo
    {
        public int Page { get; set; }
        public int Limit { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }

    /// <summary>
    /// Sayfalanmış sonuç
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
    }

    /// <summary>
    /// Filtreleme seçenekleri
    /// </summary>
    public class FilterOptions
    {
        public List<string> Cities { get; set; } = new();
        public List<string> Brands { get; set; } = new();
        public List<string> Features { get; set; } = new();
        public List<string> Genres { get; set; } = new();
    }

    /// <summary>
    /// Arama parametreleri
    /// </summary>
    public class SearchParams
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; } = "asc";
    }

    /// <summary>
    /// Grafik verileri
    /// </summary>
    public class ChartData
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
        public Dictionary<string, object> Extra { get; set; } = new();
    }
}