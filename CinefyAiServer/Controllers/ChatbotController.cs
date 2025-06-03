using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinefyAiServer.Data;
using CinefyAiServer.Entities;
using CinefyAiServer.Entities.DTOs;
using System.Text.RegularExpressions;

namespace CinefyAiServer.Controllers;

/// <summary>
/// AI Chatbot API endpoint'leri
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Chatbot")]
public class ChatbotController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ChatbotController> _logger;

    public ChatbotController(ApplicationDbContext context, ILogger<ChatbotController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Chatbot'a mesaj gönderir ve yanıt alır
    /// </summary>
    [HttpPost("message")]
    public async Task<ActionResult<ChatResponse>> SendMessage(ChatRequest request)
    {
        try
        {
            // Intent analizi yap
            var intentAnalysis = await AnalyzeIntentAsync(request.Message);

            // Yanıt oluştur
            var responseMessage = await GenerateResponseAsync(intentAnalysis);

            // Mesajı veritabanına kaydet
            var chatMessage = new ChatMessage
            {
                UserId = request.UserId ?? Guid.Empty,
                Message = request.Message,
                Response = responseMessage.Response,
                Intent = intentAnalysis.Intent
            };

            string responseText;
            ChatData? responseData = null;
            List<string>? suggestions = null;
            string? followUpQuestion = null;

            // Intent'e göre yanıt oluştur
            switch (intentAnalysis.Intent.ToLower())
            {
                case "film_onerisi":
                    (responseText, responseData) = await HandleMovieRecommendation(request, intentAnalysis);
                    suggestions = new List<string>
                    {
                        "Hangi türde film arıyorsun?",
                        "Yakın sinemalar göster",
                        "Bu filmler için seans saatleri"
                    };
                    break;

                case "yakin_sinemalar":
                    (responseText, responseData) = await HandleNearByCinemas(request, intentAnalysis);
                    suggestions = new List<string>
                    {
                        "Bu sinemalardaki filmler",
                        "Yol tarifi al",
                        "Fiyat bilgileri"
                    };
                    break;

                case "fiyat_bilgisi":
                    (responseText, responseData) = await HandlePriceInquiry(request, intentAnalysis);
                    suggestions = new List<string>
                    {
                        "İndirimli biletler",
                        "Bilet satın al",
                        "Başka filmler"
                    };
                    break;

                case "seans_saatleri":
                    (responseText, responseData) = await HandleSessionTimes(request, intentAnalysis);
                    suggestions = new List<string>
                    {
                        "Bilet satın al",
                        "Koltuk durumu",
                        "Başka saatler"
                    };
                    break;

                case "rezervasyon_yardim":
                    (responseText, responseData) = await HandleBookingHelp(request, intentAnalysis);
                    suggestions = new List<string>
                    {
                        "Nasıl rezervasyon yaparım?",
                        "İptal işlemleri",
                        "Ödeme seçenekleri"
                    };
                    break;

                case "selamlaşma":
                    responseText = "Merhaba! CinefyAI'ya hoş geldiniz! 🎬 Size nasıl yardımcı olabilirim?";
                    suggestions = new List<string>
                    {
                        "Film önerisinde bulun",
                        "Yakın sinemaları göster",
                        "Güncel fiyatlar"
                    };
                    followUpQuestion = "Hangi konuda yardım almak istersiniz?";
                    break;

                case "veda":
                    responseText = "Teşekkürler! İyi seyirler dilerim! 🍿 Her zaman yardım için buradayım.";
                    break;

                default:
                    responseText = GenerateDefaultResponse(request.Message);
                    suggestions = new List<string>
                    {
                        "Film önerisinde bulun",
                        "Yakın sinemaları göster",
                        "Seans saatleri",
                        "Fiyat bilgileri"
                    };
                    followUpQuestion = "Yukarıdaki seçeneklerden birini deneyebilirsiniz.";
                    break;
            }

            // Yanıtı kaydet
            chatMessage.Response = responseText;
            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            var response = new ChatResponse
            {
                Response = responseText,
                Intent = intentAnalysis.Intent,
                Confidence = intentAnalysis.Confidence,
                Suggestions = suggestions,
                Data = responseData,
                SessionId = request.SessionId,
                Timestamp = DateTime.UtcNow,
                FollowUpQuestion = followUpQuestion
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chatbot mesajı işlenirken hata oluştu");
            return StatusCode(500, new { message = "Mesajınız işlenirken bir hata oluştu. Lütfen tekrar deneyin." });
        }
    }

    /// <summary>
    /// Chatbot önerilerini getirir
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<ActionResult<ChatSuggestionsResponse>> GetSuggestions()
    {
        try
        {
            var suggestions = new List<ChatSuggestion>
            {
                new() { Text = "Film önerisinde bulun", Category = "Öneri", Intent = "film_onerisi" },
                new() { Text = "Yakın sinemaları göster", Category = "Konum", Intent = "yakin_sinemalar" },
                new() { Text = "Güncel fiyatları öğren", Category = "Fiyat", Intent = "fiyat_bilgisi" },
                new() { Text = "Bugünkü seanslar", Category = "Seans", Intent = "seans_saatleri" },
                new() { Text = "İndirimli biletler", Category = "İndirim", Intent = "indirim_bilgisi" },
                new() { Text = "IMAX salonlar", Category = "Premium", Intent = "premium_deneyim" },
                new() { Text = "Rezervasyon yardımı", Category = "Yardım", Intent = "rezervasyon_yardim" }
            };

            var popularQuestions = new List<string>
            {
                "Hangi filmler vizyonda?",
                "En yakın sinema nerede?",
                "Bilet fiyatları ne kadar?",
                "Bugün hangi seanslar var?",
                "İndirimli bilet nasıl alırım?",
                "IMAX deneyimi nasıl?",
                "Rezervasyon nasıl iptal ederim?"
            };

            var quickActions = new List<QuickAction>
            {
                new() { Label = "Film Ara", Action = "search_movies", Icon = "🎬" },
                new() { Label = "Sinema Bul", Action = "find_cinemas", Icon = "📍" },
                new() { Label = "Fiyat Sor", Action = "ask_price", Icon = "💰" },
                new() { Label = "Seans Bak", Action = "check_sessions", Icon = "🕐" }
            };

            var response = new ChatSuggestionsResponse
            {
                Suggestions = suggestions,
                PopularQuestions = popularQuestions,
                QuickActions = quickActions
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chatbot önerileri getirilirken hata oluştu");
            return StatusCode(500, new { message = "Öneriler getirilemedi" });
        }
    }

    /// <summary>
    /// Kullanıcının chat geçmişini getirir
    /// </summary>
    [HttpGet("history/{userId}")]
    public async Task<ActionResult<ChatHistoryResponse>> GetChatHistory(Guid userId)
    {
        try
        {
            var chatHistory = await _context.ChatMessages
                .Where(cm => cm.UserId == userId)
                .OrderBy(cm => cm.CreatedAt)
                .Select(cm => new ChatHistoryItem
                {
                    Message = cm.Message,
                    Response = cm.Response,
                    Intent = cm.Intent ?? "",
                    Confidence = 0.8m, // Default değer
                    Timestamp = cm.CreatedAt,
                    IsUserMessage = false
                })
                .ToListAsync();

            var response = new ChatHistoryResponse
            {
                History = chatHistory,
                TotalMessages = chatHistory.Count,
                LastActivity = chatHistory.LastOrDefault()?.Timestamp
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat geçmişi getirilirken hata oluştu: {UserId}", userId);
            return StatusCode(500, new { message = "Chat geçmişi getirilemedi" });
        }
    }

    /// <summary>
    /// Kişiselleştirilmiş öneri getirir
    /// </summary>
    [HttpPost("recommendations")]
    public async Task<ActionResult<RecommendationResponse>> GetRecommendations(RecommendationRequest request)
    {
        try
        {
            var movies = new List<MovieRecommendation>();
            var cinemas = new List<CinemaRecommendation>();
            var sessions = new List<SessionRecommendation>();

            // Film önerileri
            var moviesQuery = _context.Movies.Where(m => m.IsActive);

            if (!string.IsNullOrEmpty(request.Genre))
            {
                moviesQuery = moviesQuery.Where(m => m.Genre != null && m.Genre.Contains(request.Genre));
            }

            var recommendedMovies = await moviesQuery
                .OrderByDescending(m => m.IsPopular)
                .ThenByDescending(m => m.Rating)
                .Take(5)
                .ToListAsync();

            foreach (var movie in recommendedMovies)
            {
                var matchScore = CalculateMovieMatchScore(movie, request);
                var reasons = GenerateMovieReasons(movie, request);

                movies.Add(new MovieRecommendation
                {
                    Movie = new MovieResponse
                    {
                        Id = movie.Id,
                        Title = movie.Title,
                        Description = movie.Description,
                        Poster = movie.Poster,
                        Genre = movie.Genre,
                        Duration = movie.Duration,
                        Rating = movie.Rating,
                        Director = movie.Director,
                        Cast = movie.Cast
                    },
                    MatchScore = matchScore,
                    Reasons = reasons
                });
            }

            // Sinema önerileri
            if (request.Location != null)
            {
                var nearbyCinemas = await GetNearbyCinemas(request.Location, request.City);
                cinemas.AddRange(nearbyCinemas);
            }

            var reasoningText = GenerateReasoningText(request, movies.Count, cinemas.Count);

            var response = new RecommendationResponse
            {
                Movies = movies,
                Cinemas = cinemas,
                Sessions = sessions,
                ReasoningText = reasoningText
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Öneriler oluşturulurken hata oluştu");
            return StatusCode(500, new { message = "Öneriler oluşturulamadı" });
        }
    }

    #region Private Methods

    /// <summary>
    /// Intent analizi yapar
    /// </summary>
    private async Task<IntentAnalysisResponse> AnalyzeIntentAsync(string message)
    {
        var intent = AnalyzeIntent(message);
        return await Task.FromResult(intent);
    }

    /// <summary>
    /// Yanıt üretir
    /// </summary>
    private async Task<ChatResponse> GenerateResponseAsync(IntentAnalysisResponse intent)
    {
        var response = await GenerateResponseAsync(intent.Intent, intent.RequiredAction, intent.Confidence);
        return response;
    }

    /// <summary>
    /// Intent ve parametrelere göre yanıt oluşturur
    /// </summary>
    private async Task<ChatResponse> GenerateResponseAsync(string intent, string? action, decimal confidence)
    {
        return await Task.FromResult(new ChatResponse
        {
            Response = $"Intent: {intent} işleniyor...",
            Intent = intent,
            Confidence = confidence,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Mesajın intent'ini analiz eder
    /// </summary>
    private static IntentAnalysisResponse AnalyzeIntent(string message)
    {
        var normalizedMessage = message.ToLowerInvariant().Trim();

        // Basit intent analizi (gerçek uygulamada ML modeli kullanılacak)
        var intentPatterns = new Dictionary<string, (string Pattern, decimal Confidence)>
        {
            { "film_onerisi", (@"\b(film|movie|öneri|öner|izle|seyret|vizyond)", 0.9m) },
            { "yakin_sinemalar", (@"\b(yakın|near|sinema|cinema|nered|konum|mesafe)", 0.85m) },
            { "fiyat_bilgisi", (@"\b(fiyat|price|ücret|para|bilet|ticket|kaç)", 0.9m) },
            { "seans_saatleri", (@"\b(seans|saat|time|schedule|gösteri|when)", 0.85m) },
            { "rezervasyon_yardim", (@"\b(rezerv|book|nasıl|how|yardım|help)", 0.8m) },
            { "selamlaşma", (@"\b(merhaba|hello|hi|selam|hey|iyi günler)", 0.95m) },
            { "veda", (@"\b(bye|güle güle|hoşça kal|teşekkür|thanks|görüşürüz)", 0.9m) },
            { "indirim_bilgisi", (@"\b(indirim|discount|kampanya|ucuz|öğrenci)", 0.85m) },
            { "premium_deneyim", (@"\b(imax|4dx|vip|premium|özel|lüks)", 0.9m) }
        };

        foreach (var (intent, (pattern, confidence)) in intentPatterns)
        {
            if (Regex.IsMatch(normalizedMessage, pattern, RegexOptions.IgnoreCase))
            {
                return new IntentAnalysisResponse
                {
                    Intent = intent,
                    Confidence = confidence,
                    Entities = ExtractEntities(normalizedMessage),
                    Parameters = new Dictionary<string, object>()
                };
            }
        }

        return new IntentAnalysisResponse
        {
            Intent = "genel_soru",
            Confidence = 0.3m,
            Entities = new List<EntityExtraction>(),
            Parameters = new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// Mesajdan entity'leri çıkarır
    /// </summary>
    private static List<EntityExtraction> ExtractEntities(string message)
    {
        var entities = new List<EntityExtraction>();

        // Film türlerini tespit et
        var genres = new[] { "aksiyon", "drama", "komedi", "korku", "bilim kurgu", "romantik", "animasyon" };
        foreach (var genre in genres)
        {
            if (message.Contains(genre))
            {
                entities.Add(new EntityExtraction
                {
                    Entity = "genre",
                    Value = genre,
                    Confidence = 0.8m,
                    StartIndex = message.IndexOf(genre),
                    EndIndex = message.IndexOf(genre) + genre.Length
                });
            }
        }

        // Şehir isimlerini tespit et
        var cities = new[] { "istanbul", "ankara", "izmir", "antalya", "bursa", "adana" };
        foreach (var city in cities)
        {
            if (message.Contains(city))
            {
                entities.Add(new EntityExtraction
                {
                    Entity = "city",
                    Value = city,
                    Confidence = 0.85m,
                    StartIndex = message.IndexOf(city),
                    EndIndex = message.IndexOf(city) + city.Length
                });
            }
        }

        return entities;
    }

    /// <summary>
    /// Film önerisi işler
    /// </summary>
    private async Task<(string Response, ChatData? Data)> HandleMovieRecommendation(ChatRequest request, IntentAnalysisResponse intent)
    {
        var genreEntity = intent.Entities.FirstOrDefault(e => e.Entity == "genre");
        var moviesQuery = _context.Movies.Where(m => m.IsActive);

        if (genreEntity != null)
        {
            moviesQuery = moviesQuery.Where(m => m.Genre != null && m.Genre.Contains(genreEntity.Value));
        }

        var movies = await moviesQuery
            .OrderByDescending(m => m.IsPopular)
            .ThenByDescending(m => m.Rating)
            .Take(5)
            .Select(m => new MovieResponse
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                Poster = m.Poster,
                Genre = m.Genre,
                Duration = m.Duration,
                Rating = m.Rating,
                Director = m.Director,
                ReleaseDate = m.ReleaseDate
            })
            .ToListAsync();

        var response = genreEntity != null
            ? $"İşte {genreEntity.Value} türünde önerdiğim filmler! 🎬"
            : "Size özel film önerilerim! 🎭";

        if (movies.Count == 0)
        {
            response = "Üzgünüm, bu kriterlere uygun film bulamadım. Başka türlerde film önerebilirim.";
        }

        var data = new ChatData
        {
            Movies = movies,
            QuickReplies = new List<string> { "Bu filmler için seans saatleri", "Yakın sinemalar", "Fiyat bilgileri" }
        };

        return (response, data);
    }

    /// <summary>
    /// Yakın sinema işler
    /// </summary>
    private async Task<(string Response, ChatData? Data)> HandleNearByCinemas(ChatRequest request, IntentAnalysisResponse intent)
    {
        var cityEntity = intent.Entities.FirstOrDefault(e => e.Entity == "city");
        var cinemasQuery = _context.Cinemas.Where(c => c.IsActive);

        if (cityEntity != null)
        {
            cinemasQuery = cinemasQuery.Where(c => c.City.ToLower().Contains(cityEntity.Value));
        }
        else if (!string.IsNullOrEmpty(request.Context?.UserLocation?.City))
        {
            cinemasQuery = cinemasQuery.Where(c => c.City.ToLower().Contains(request.Context.UserLocation.City.ToLower()));
        }

        var cinemas = await cinemasQuery
            .OrderByDescending(c => c.Rating)
            .Take(5)
            .Select(c => new CinemaResponse
            {
                Id = c.Id,
                Name = c.Name,
                Brand = c.Brand,
                Address = c.Address,
                City = c.City,
                District = c.District,
                Rating = c.Rating,
                Features = c.Features,
                Phone = c.Phone
            })
            .ToListAsync();

        var cityName = cityEntity?.Value ?? request.Context?.UserLocation?.City ?? "bulunduğunuz bölgede";
        var response = $"{cityName} yakınındaki sinema önerilerim! 🏢";

        if (cinemas.Count == 0)
        {
            response = "Bu bölgede sinema bulamadım. Başka bir şehir deneyebilirsiniz.";
        }

        var data = new ChatData
        {
            Cinemas = cinemas,
            QuickReplies = new List<string> { "Bu sinemalardaki filmler", "Yol tarifi", "İletişim bilgileri" }
        };

        return (response, data);
    }

    /// <summary>
    /// Fiyat bilgisi işler
    /// </summary>
    private async Task<(string Response, ChatData? Data)> HandlePriceInquiry(ChatRequest request, IntentAnalysisResponse intent)
    {
        // En güncel fiyat bilgilerini al
        var priceInfo = await _context.Sessions
            .Where(s => s.IsActive && s.SessionDate >= DateOnly.FromDateTime(DateTime.Today))
            .GroupBy(s => 1)
            .Select(g => new PriceInfo
            {
                MinPrice = g.Min(s => s.StandardPrice),
                MaxPrice = g.Max(s => s.VipPrice),
                AveragePrice = g.Average(s => s.StandardPrice),
                Categories = new List<PriceCategory>
                {
                    new() { Name = "Standart", Price = g.Average(s => s.StandardPrice), Description = "Normal koltuklar" },
                    new() { Name = "VIP", Price = g.Average(s => s.VipPrice), Description = "Premium koltuklar" }
                }
            })
            .FirstOrDefaultAsync();

        if (priceInfo == null)
        {
            return ("Şu anda fiyat bilgisi alınamıyor. Lütfen daha sonra tekrar deneyin.", null);
        }

        var response = $"Güncel bilet fiyatları 💰\n" +
                      $"• Standart: {priceInfo.AveragePrice:F2} TL\n" +
                      $"• VIP: {priceInfo.Categories.FirstOrDefault(c => c.Name == "VIP")?.Price:F2} TL\n" +
                      $"• Fiyat aralığı: {priceInfo.MinPrice:F2} - {priceInfo.MaxPrice:F2} TL";

        var data = new ChatData
        {
            PriceInfo = priceInfo,
            QuickReplies = new List<string> { "İndirimli biletler", "Bilet satın al", "Öğrenci indirimi" }
        };

        return (response, data);
    }

    /// <summary>
    /// Seans saatleri işler
    /// </summary>
    private async Task<(string Response, ChatData? Data)> HandleSessionTimes(ChatRequest request, IntentAnalysisResponse intent)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var sessions = await _context.Sessions
            .Include(s => s.Movie)
            .Include(s => s.Cinema)
            .Where(s => s.IsActive && s.SessionDate == today)
            .OrderBy(s => s.SessionTime)
            .Take(10)
            .Select(s => new SessionResponse
            {
                Id = s.Id,
                SessionTime = s.SessionTime,
                StandardPrice = s.StandardPrice,
                VipPrice = s.VipPrice,
                AvailableSeats = s.AvailableSeats,
                TotalSeats = s.TotalSeats,
                Movie = s.Movie != null ? new MovieResponse
                {
                    Title = s.Movie.Title,
                    Duration = s.Movie.Duration
                } : null,
                Cinema = s.Cinema != null ? new CinemaResponse
                {
                    Name = s.Cinema.Name,
                    City = s.Cinema.City
                } : null
            })
            .ToListAsync();

        var response = sessions.Count > 0
            ? "Bugünkü seans saatleri! 🕐"
            : "Bugün için seans bulunamadı. Yarınki seansları görmek ister misiniz?";

        var data = new ChatData
        {
            Sessions = sessions,
            QuickReplies = new List<string> { "Bilet al", "Koltuk durumu", "Yarınki seanslar" }
        };

        return (response, data);
    }

    /// <summary>
    /// Rezervasyon yardımı işler
    /// </summary>
    private async Task<(string Response, ChatData? Data)> HandleBookingHelp(ChatRequest request, IntentAnalysisResponse intent)
    {
        var response = "Rezervasyon konusunda yardım! 🎫\n\n" +
                      "1️⃣ Film ve sinema seçin\n" +
                      "2️⃣ Seans saati belirleyin\n" +
                      "3️⃣ Koltuk seçimi yapın\n" +
                      "4️⃣ Ödeme bilgilerini girin\n" +
                      "5️⃣ QR kodunuzu alın!\n\n" +
                      "Hangi adımda yardıma ihtiyacınız var?";

        var data = new ChatData
        {
            QuickReplies = new List<string>
            {
                "Nasıl koltuk seçerim?",
                "Ödeme seçenekleri",
                "İptal nasıl yapılır?",
                "QR kod nerede?"
            }
        };

        return (response, data);
    }

    /// <summary>
    /// Varsayılan yanıt oluşturur
    /// </summary>
    private static string GenerateDefaultResponse(string message)
    {
        var responses = new[]
        {
            "Anlayamadım, başka bir şekilde sorabilir misiniz? 🤔",
            "Bu konuda size nasıl yardımcı olabilirim? 💭",
            "Lütfen daha açık bir şekilde sorabilir misiniz? 🎬",
            "Size film, sinema veya bilet konularında yardım edebilirim! 🎭"
        };

        var random = new Random();
        return responses[random.Next(responses.Length)];
    }

    /// <summary>
    /// Yakın sinemaları getirir
    /// </summary>
    private async Task<List<CinemaRecommendation>> GetNearbyCinemas(UserLocation location, string? city)
    {
        var cinemasQuery = _context.Cinemas.Where(c => c.IsActive);

        if (!string.IsNullOrEmpty(city))
        {
            cinemasQuery = cinemasQuery.Where(c => c.City.ToLower().Contains(city.ToLower()));
        }

        var cinemas = await cinemasQuery
            .OrderByDescending(c => c.Rating)
            .Take(5)
            .ToListAsync();

        return cinemas.Select(c => new CinemaRecommendation
        {
            Cinema = new CinemaResponse
            {
                Id = c.Id,
                Name = c.Name,
                Brand = c.Brand,
                Address = c.Address,
                City = c.City,
                Rating = c.Rating,
                Features = c.Features
            },
            MatchScore = 85m, // Örnek skor
            Reasons = new List<string> { "Yüksek rating", "Yakın konum", "Modern olanaklar" },
            Distance = null // Hesaplanabilir
        }).ToList();
    }

    /// <summary>
    /// Film eşleşme skorunu hesaplar
    /// </summary>
    private static decimal CalculateMovieMatchScore(Movie movie, RecommendationRequest request)
    {
        decimal score = 50; // Base score

        if (!string.IsNullOrEmpty(request.Genre) && movie.Genre?.Contains(request.Genre) == true)
            score += 30;

        if (movie.IsPopular)
            score += 15;

        if (movie.Rating >= 4)
            score += 5;

        return Math.Min(score, 100);
    }

    /// <summary>
    /// Film öneri nedenlerini oluşturur
    /// </summary>
    private static List<string> GenerateMovieReasons(Movie movie, RecommendationRequest request)
    {
        var reasons = new List<string>();

        if (movie.IsPopular)
            reasons.Add("Popüler film");

        if (movie.Rating >= 4)
            reasons.Add($"Yüksek rating ({movie.Rating:F1})");

        if (!string.IsNullOrEmpty(request.Genre) && movie.Genre?.Contains(request.Genre) == true)
            reasons.Add($"{request.Genre} türünde");

        if (movie.IsNew)
            reasons.Add("Yeni vizyon");

        return reasons;
    }

    /// <summary>
    /// Öneri gerekçe metnini oluşturur
    /// </summary>
    private static string GenerateReasoningText(RecommendationRequest request, int movieCount, int cinemaCount)
    {
        var text = "Size özel öneri kriterlerim:\n";

        if (!string.IsNullOrEmpty(request.Genre))
            text += $"• {request.Genre} türü filmleri tercih ediyorsunuz\n";

        if (!string.IsNullOrEmpty(request.City))
            text += $"• {request.City} şehrindeki sinemalar\n";

        if (request.MaxPrice.HasValue)
            text += $"• {request.MaxPrice} TL altında fiyatlar\n";

        text += $"\n{movieCount} film ve {cinemaCount} sinema önerisi buldum!";

        return text;
    }

    #endregion
}