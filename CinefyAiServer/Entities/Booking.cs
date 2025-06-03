using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CinefyAiServer.Entities.DTOs;

namespace CinefyAiServer.Entities
{
    /// <summary>
    /// Rezervasyon entity'si
    /// </summary>
    public class Booking
    {
        public Guid Id { get; set; }

        [Required]
        public Guid SessionId { get; set; }

        public Guid? UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public required string CustomerName { get; set; }

        [Required]
        [MaxLength(255)]
        public required string CustomerEmail { get; set; }

        [MaxLength(20)]
        public string? CustomerPhone { get; set; }

        /// <summary>
        /// Seçilen koltuklar JSON formatında - [{"row": "A", "number": 1, "type": "Standard"}]
        /// </summary>
        public List<SeatSelection> SelectedSeats { get; set; } = new();

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal FinalAmount { get; set; }

        [MaxLength(50)]
        public string? DiscountType { get; set; } // "Öğrenci", "65+ yaş", "Çarşamba"

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        [MaxLength(255)]
        public string? TransactionId { get; set; }

        [Required]
        [MaxLength(20)]
        public required string BookingCode { get; set; }

        public string? QrCode { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Session Session { get; set; } = null!;
        public virtual User? User { get; set; }
    }
}