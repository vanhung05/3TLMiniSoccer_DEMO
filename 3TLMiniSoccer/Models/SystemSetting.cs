using System.ComponentModel.DataAnnotations;

namespace _3TLMiniSoccer.Models
{
    public class SystemSetting
    {
        [Key]
        public int SettingId { get; set; }

        [Required]
        [StringLength(100)]
        public string SettingKey { get; set; } = string.Empty;

        [Required]
        public string SettingValue { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string DataType { get; set; } = "String";

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public int? UpdatedBy { get; set; }

        // Navigation properties
        public virtual User? UpdatedByUser { get; set; }
    }
}
