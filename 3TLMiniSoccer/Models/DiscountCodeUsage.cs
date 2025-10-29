using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _3TLMiniSoccer.Models
{
    public class DiscountCodeUsage
    {
        [Key]
        public int UsageId { get; set; }

        [Required]
        public int DiscountCodeId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; }

        public DateTime UsedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual DiscountCode DiscountCode { get; set; } = null!;
        public virtual Booking Booking { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
