using System.ComponentModel.DataAnnotations;

namespace _3TLMiniSoccer.Models
{
    public class FieldSchedule
    {
        [Key]
        public int ScheduleId { get; set; }

        [Required]
        public int FieldId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; } // Dynamic start time

        [Required]
        public TimeSpan EndTime { get; set; } // Dynamic end time

        [StringLength(20)]
        public string Status { get; set; } = "Available";

        public int? BookingId { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Field Field { get; set; } = null!;
        public virtual Booking? Booking { get; set; }
    }
}
