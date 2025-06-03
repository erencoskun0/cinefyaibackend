using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinefyAiServer.Entities
{
    /// <summary>
    /// AI chatbot için sohbet mesajlarını tutan entity
    /// </summary>
    public class ChatMessage
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Response { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Intent { get; set; }

        [MaxLength(255)]
        public string? Context { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsResolved { get; set; } = false;

        // Navigation Properties
        public virtual User User { get; set; } = null!;
    }
}