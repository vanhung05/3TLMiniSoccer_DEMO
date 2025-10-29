using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _3TLMiniSoccer.Models
{
    public class PaymentConfig
    {
        [Key]
        public int ConfigId { get; set; }

        [Required]
        [StringLength(50)]
        public string ConfigKey { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string ConfigValue { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string ConfigType { get; set; } = "General"; // VNPay, SEPay, General, System

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "bit")]
        public bool IsEncrypted { get; set; } = false; // For sensitive data like API keys

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UpdatedBy { get; set; }
    }

    // Configuration keys constants - Updated for SystemConfigs
    public static class PaymentConfigKeys
    {
        // SEPay Configuration
        public const string SEPAY_API_URL = "SEPAY_API_URL";
        public const string SEPAY_API_TOKEN = "SEPAY_API_TOKEN"; // Token để gọi API kiểm tra giao dịch
        public const string SEPAY_BANK_CODE = "SEPAY_BANK_CODE";
        public const string SEPAY_ACCOUNT_NUMBER = "SEPAY_ACCOUNT_NUMBER";
        public const string SEPAY_ACCOUNT_NAME = "SEPAY_ACCOUNT_NAME";

        // System Configuration
        public const string DEFAULT_PAYMENT_METHOD = "DEFAULT_PAYMENT_METHOD";
        public const string PAYMENT_TIMEOUT_MINUTES = "PAYMENT_TIMEOUT_MINUTES";
        public const string AUTO_CONFIRM_PAYMENT = "AUTO_CONFIRM_PAYMENT";
        public const string BANK_ACCOUNT_NUMBER = "BANK_ACCOUNT_NUMBER";
        public const string BANK_ACCOUNT_NAME = "BANK_ACCOUNT_NAME";
        public const string BANK_NAME = "BANK_NAME";
        public const string QR_CODE_SIZE = "QR_CODE_SIZE";
    }
}
