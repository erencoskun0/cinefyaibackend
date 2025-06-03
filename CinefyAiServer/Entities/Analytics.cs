using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinefyAiServer.Entities
{
    /// <summary>
    /// Analytics entity'si
    /// </summary>
    public class Analytics
    {
        public Guid Id { get; set; }

        [Required]
        public Guid CinemaId { get; set; }

        public Guid? SessionId { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        public int TicketsSold { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Revenue { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal OccupancyRate { get; set; } = 0;

        /// <summary>
        /// Detaylı metrikler JSON formatında
        /// </summary>
        public Dictionary<string, object> Metrics { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Cinema Cinema { get; set; } = null!;
        public virtual Session? Session { get; set; }
    }
}