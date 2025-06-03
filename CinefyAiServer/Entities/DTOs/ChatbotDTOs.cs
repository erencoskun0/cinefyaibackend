using System.ComponentModel.DataAnnotations;

namespace CinefyAiServer.Entities.DTOs;

#region Request DTOs

public class ChatRequest
{
    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    public string SessionId { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public ChatContext? Context { get; set; }
}

public class ChatContext
{
    public string? CurrentPage { get; set; }
    public UserLocation? UserLocation { get; set; }
    public string? Language { get; set; } = "tr";
    public Dictionary<string, object>? AdditionalData { get; set; }
}

public class UserLocation
{
    [Range(-90, 90)]
    public double Lat { get; set; }

    [Range(-180, 180)]
    public double Lng { get; set; }

    public string? City { get; set; }
}

#endregion

#region Response DTOs

public class ChatResponse
{
    public string Response { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public List<string>? Suggestions { get; set; }
    public ChatData? Data { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? FollowUpQuestion { get; set; }
}

public class ChatData
{
    public List<MovieResponse>? Movies { get; set; }
    public List<CinemaResponse>? Cinemas { get; set; }
    public List<SessionResponse>? Sessions { get; set; }
    public PriceInfo? PriceInfo { get; set; }
    public List<string>? QuickReplies { get; set; }
}

public class PriceInfo
{
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public List<PriceCategory> Categories { get; set; } = new();
}

public class PriceCategory
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
}

#endregion

#region Chat Suggestions DTOs

public class ChatSuggestionsResponse
{
    public List<ChatSuggestion> Suggestions { get; set; } = new();
    public List<string> PopularQuestions { get; set; } = new();
    public List<QuickAction> QuickActions { get; set; } = new();
}

public class ChatSuggestion
{
    public string Text { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
}

public class QuickAction
{
    public string Label { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string? Url { get; set; }
}

#endregion

#region Intent & NLP DTOs

public class IntentAnalysisResponse
{
    public string Intent { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public List<EntityExtraction> Entities { get; set; } = new();
    public string? RequiredAction { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class EntityExtraction
{
    public string Entity { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
}

#endregion

#region Chat History DTOs

public class ChatHistoryResponse
{
    public List<ChatHistoryItem> History { get; set; } = new();
    public int TotalMessages { get; set; }
    public DateTime? LastActivity { get; set; }
}

public class ChatHistoryItem
{
    public string Message { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsUserMessage { get; set; }
}

#endregion

#region Recommendation DTOs

public class RecommendationRequest
{
    public Guid? UserId { get; set; }
    public string? Genre { get; set; }
    public string? City { get; set; }
    public decimal? MaxPrice { get; set; }
    public DateTime? PreferredDate { get; set; }
    public TimeOnly? PreferredTime { get; set; }
    public List<string>? Features { get; set; } // IMAX, 4DX, VIP
    public UserLocation? Location { get; set; }
}

public class RecommendationResponse
{
    public List<MovieRecommendation> Movies { get; set; } = new();
    public List<CinemaRecommendation> Cinemas { get; set; } = new();
    public List<SessionRecommendation> Sessions { get; set; } = new();
    public string ReasoningText { get; set; } = string.Empty;
}

public class MovieRecommendation
{
    public MovieResponse Movie { get; set; } = new();
    public decimal MatchScore { get; set; }
    public List<string> Reasons { get; set; } = new();
    public List<SessionRecommendation> NearBySessions { get; set; } = new();
}

public class CinemaRecommendation
{
    public CinemaResponse Cinema { get; set; } = new();
    public decimal MatchScore { get; set; }
    public List<string> Reasons { get; set; } = new();
    public double? Distance { get; set; }
    public List<MovieRecommendation> AvailableMovies { get; set; } = new();
}

public class SessionRecommendation
{
    public SessionResponse Session { get; set; } = new();
    public decimal MatchScore { get; set; }
    public List<string> Reasons { get; set; } = new();
    public bool IsOptimalTime { get; set; }
    public bool IsGoodPrice { get; set; }
}

#endregion