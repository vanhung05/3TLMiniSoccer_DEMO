using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _3TLMiniSoccer.Models
{
    public class PricingRule
    {
        [Key]
        public int PricingRuleId { get; set; }

        [Required]
        public int FieldTypeId { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public int DayOfWeek { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public bool IsPeakHour { get; set; } = false;

        [Column(TypeName = "decimal(3,2)")]
        public decimal PeakMultiplier { get; set; } = 1.0m;

        [Required]
        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual FieldType FieldType { get; set; } = null!;
    }
}
