using System.ComponentModel.DataAnnotations;

namespace _3TLMiniSoccer.Models
{
    public class SystemConfig
    {
        [Key]
        public int ConfigId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ConfigKey { get; set; } = string.Empty;
        
        [Required]
        public string ConfigValue { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ConfigType { get; set; } = string.Empty; // SEPay, System, General, etc.
        
        [StringLength(50)]
        public string DataType { get; set; } = "String"; // String, Number, Boolean, JSON
        
        public bool IsActive { get; set; } = true;
        
        public bool IsEncrypted { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        public int? UpdatedBy { get; set; }
    }
}
