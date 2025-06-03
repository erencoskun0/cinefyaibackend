using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinefyAiServer.Entities
{
    /// <summary>
    /// Seans entity'si
    /// </summary>
    public class Session
    {
        public Guid Id { get; set; }

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
        [Column(TypeName = "decimal(10,2)")]
        public decimal StandardPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal VipPrice { get; set; }

        public int AvailableSeats { get; set; }

        public int TotalSeats { get; set; }

        public OccupancyStatus OccupancyStatus { get; set; } = OccupancyStatus.Müsait;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Movie Movie { get; set; } = null!;
        public virtual Hall Hall { get; set; } = null!;
        public virtual Cinema Cinema { get; set; } = null!;
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Analytics> Analytics { get; set; } = new List<Analytics>();
    }
}