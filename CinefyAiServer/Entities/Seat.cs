using System.ComponentModel.DataAnnotations;

namespace CinefyAiServer.Entities
{
    /// <summary>
    /// Salonlardaki koltukları temsil eder. Koltuklar dinamik olarak eklenebilir.
    /// </summary>
    public class Seat
    {
        public int Id { get; set; }
        public Guid HallId { get; set; }
        public Hall Hall { get; set; }

        [Required, MaxLength(10)]
        public string Row { get; set; } // Örn: A, B, C

        [Required, MaxLength(10)]
        public string Number { get; set; } // Örn: 1, 2, 3

        public ICollection<Ticket> Tickets { get; set; }
    }
}