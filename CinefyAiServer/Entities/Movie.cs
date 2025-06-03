using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinefyAiServer.Entities
{
    /// <summary>
    /// Filmleri temsil eder. Sektörel ihtiyaçlara uygun, zenginleştirilmiş film entity'si.
    /// </summary>
    public class Movie
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Title { get; set; }

        public string? Description { get; set; }

        public string? Poster { get; set; }

        public string? Backdrop { get; set; }

        public string? TrailerUrl { get; set; }

        /// <summary>
        /// Film türleri JSON formatında - ["Aksiyon", "Drama", "Bilim Kurgu"]
        /// </summary>
        public List<string> Genre { get; set; } = new();

        [Required]
        public int Duration { get; set; } // dakika cinsinden

        [Column(TypeName = "decimal(2,1)")]
        public decimal Rating { get; set; } = 0;

        [Required]
        public DateOnly ReleaseDate { get; set; }

        [MaxLength(255)]
        public string? Director { get; set; }

        /// <summary>
        /// Oyuncular JSON formatında - ["Actor 1", "Actor 2"]
        /// </summary>
        public List<string> Cast { get; set; } = new();

        [MaxLength(10)]
        public string? AgeRating { get; set; } // "13+", "18+"

        public bool IsPopular { get; set; } = false;

        public bool IsNew { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}