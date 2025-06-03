using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CinefyAiServer.Entities
{
    /// <summary>
    /// Kullanıcı entity'si - ASP.NET Identity tabanlı
    /// </summary>
    public class User : IdentityUser<Guid>
    {
        [Required]
        [MaxLength(100)]
        public required string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public required string LastName { get; set; }

        public string? Avatar { get; set; }

        public UserRole UserRole { get; set; } = UserRole.User;

        [MaxLength(255)]
        public string? CompanyName { get; set; }

        [MaxLength(100)]
        public string? Position { get; set; }

        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string Country { get; set; } = "Türkiye";

        public bool IsActive { get; set; } = true;

        public DateTime? LastLogin { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Computed properties
        public string FullName => $"{FirstName} {LastName}";

        // Navigation Properties
        public virtual ICollection<Cinema> OwnedCinemas { get; set; } = new List<Cinema>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    }
}
