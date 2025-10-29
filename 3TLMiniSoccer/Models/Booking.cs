using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _3TLMiniSoccer.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        [StringLength(20)]
        public string BookingCode { get; set; } = string.Empty;

        public int? UserId { get; set; }

        [StringLength(100)]
        public string? GuestName { get; set; }

        [StringLength(20)]
        public string? GuestPhone { get; set; }

        [StringLength(100)]
        public string? GuestEmail { get; set; }

        [Required]
        public int FieldId { get; set; }

        [Required]
        public DateTime BookingDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; } // Dynamic start time

        [Required]
        public TimeSpan EndTime { get; set; } // Dynamic end time

        [Required]
        public int Duration { get; set; } = 60; // Duration in minutes (60, 90, 120)

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Pending";

        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        [StringLength(100)]
        public string? PaymentReference { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public DateTime? ConfirmedAt { get; set; }

        public int? ConfirmedBy { get; set; }

        public DateTime? CancelledAt { get; set; }

        public int? CancelledBy { get; set; }

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual Field Field { get; set; } = null!;
        public virtual User? ConfirmedByUser { get; set; }
        public virtual User? CancelledByUser { get; set; }
        public virtual ICollection<BookingSession> BookingSessions { get; set; } = new List<BookingSession>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<FieldSchedule> FieldSchedules { get; set; } = new List<FieldSchedule>();
    }
}
