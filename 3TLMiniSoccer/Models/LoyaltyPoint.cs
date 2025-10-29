using System.ComponentModel.DataAnnotations;

namespace _3TLMiniSoccer.Models
{
    public class LoyaltyPoint
    {
        [Key]
        public int PointId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int Points { get; set; }

        [Required]
        [StringLength(20)]
        public string PointType { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        public int? BookingId { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Booking? Booking { get; set; }
    }
}
