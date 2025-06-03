namespace CinefyAiServer.Entities
{
    /// <summary>
    /// Kullanıcı rolleri
    /// </summary>
    public enum UserRole
    {
        User = 0,
        Owner = 1,
        Admin = 2
    }

    /// <summary>
    /// Ödeme durumu
    /// </summary>
    public enum PaymentStatus
    {
        Pending = 0,
        Completed = 1,
        Failed = 2,
        Refunded = 3
    }

    /// <summary>
    /// Rezervasyon durumu
    /// </summary>
    public enum BookingStatus
    {
        Confirmed = 0,
        Cancelled = 1,
        Completed = 2
    }

    /// <summary>
    /// Doluluk durumu
    /// </summary>
    public enum OccupancyStatus
    {
        Müsait = 0,
        DolmakÜzere = 1,
        AzKoltuk = 2
    }

    /// <summary>
    /// Koltuk tipi
    /// </summary>
    public enum SeatType
    {
        Standard = 0,
        Vip = 1,
        Premium = 2,
        Disabled = 3 // Engelli koltukları
    }

    /// <summary>
    /// Film yaş sınırlaması
    /// </summary>
    public enum AgeRating
    {
        G = 0,      // Genel izleyici
        PG = 1,     // 7+ yaş
        PG13 = 2,   // 13+ yaş
        R = 3,      // 15+ yaş
        NC17 = 4    // 18+ yaş
    }
}