using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinefyAiServer.Entities
{
    /// <summary>
    /// Sinema entity'si
    /// </summary>
    public class Cinema
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Name { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Brand { get; set; }

        [Required]
        public required string Address { get; set; }

        [Required]
        [MaxLength(100)]
        public required string City { get; set; }

        [Required]
        [MaxLength(100)]
        public required string District { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(255)]
        public string? Email { get; set; }

        public Guid? OwnerId { get; set; }

        public string? Description { get; set; }

        /// <summary>
        /// Sinema olanakları JSON formatında - ["Otopark", "Restoran", "Wi-Fi", "Alışveriş"]
        /// </summary>
        public List<string> Facilities { get; set; } = new();

        /// <summary>
        /// Sinema özellikleri JSON formatında - ["IMAX", "4DX", "VIP", "Dolby Atmos"]
        /// </summary>
        public List<string> Features { get; set; } = new();

        [Column(TypeName = "decimal(2,1)")]
        public decimal Rating { get; set; } = 0;

        public int ReviewCount { get; set; } = 0;

        public int Capacity { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "decimal(10,8)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(11,8)")]
        public decimal? Longitude { get; set; }

        /// <summary>
        /// Açılış saatleri JSON formatında
        /// </summary>
        public Dictionary<string, string> OpeningHours { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual User? Owner { get; set; }
        public virtual ICollection<Hall> Halls { get; set; } = new List<Hall>();
        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Analytics> Analytics { get; set; } = new List<Analytics>();
    }
}