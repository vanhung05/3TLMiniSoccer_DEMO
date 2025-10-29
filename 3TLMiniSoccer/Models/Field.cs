using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _3TLMiniSoccer.Models
{
    public class Field
    {
        [Key]
        public int FieldId { get; set; }

        [Required]
        [StringLength(100)]
        public string FieldName { get; set; } = string.Empty;

        [Required]
        public int FieldTypeId { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }


        // Field operating hours for dynamic booking
        public TimeSpan OpeningTime { get; set; } = new TimeSpan(6, 0, 0); // 06:00

        public TimeSpan ClosingTime { get; set; } = new TimeSpan(23, 0, 0); // 23:00

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual FieldType FieldType { get; set; } = null!;
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<FieldSchedule> FieldSchedules { get; set; } = new List<FieldSchedule>();
    }
}
