using System;
using System.ComponentModel.DataAnnotations;

namespace CinefyAiServer.Entities
{
    /// <summary>
    /// Yorum entity'si
    /// </summary>
    public class Review
    {
        public Guid Id { get; set; }

        public Guid? CinemaId { get; set; }

        public Guid? MovieId { get; set; }

        public Guid? UserId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Cinema? Cinema { get; set; }
        public virtual Movie? Movie { get; set; }
        public virtual User? User { get; set; }
    }
}