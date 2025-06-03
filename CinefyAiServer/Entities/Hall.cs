using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinefyAiServer.Entities
{
    /// <summary>
    /// Salon entity'si
    /// </summary>
    public class Hall
    {
        public Guid Id { get; set; }

        [Required]
        public Guid CinemaId { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        [Required]
        public int Capacity { get; set; }

        /// <summary>
        /// Koltuk düzeni JSON formatında
        /// </summary>
        public Dictionary<string, object> SeatLayout { get; set; } = new();

        /// <summary>
        /// Salon özellikleri JSON formatında - ["IMAX", "4DX", "VIP", "Premium Sound"]
        /// </summary>
        public List<string> Features { get; set; } = new();

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Cinema Cinema { get; set; } = null!;
        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
        public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
    }
}