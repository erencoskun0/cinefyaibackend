using System;
using System.ComponentModel.DataAnnotations;

namespace CinefyAiServer.Entities
{
    /// <summary>
    /// Biletleri temsil eder. Kullanıcı, seans ve koltuk ilişkisi içerir.
    /// </summary>
    public class Ticket
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }

        public Guid ShowId { get; set; }
        public Show Show { get; set; }

        public int SeatId { get; set; }
        public Seat Seat { get; set; }

        public string QrCode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}