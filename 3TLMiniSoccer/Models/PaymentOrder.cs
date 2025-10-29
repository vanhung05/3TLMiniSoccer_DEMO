using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _3TLMiniSoccer.Models
{
    public class PaymentOrder
    {
        [Key]
        public int PaymentOrderId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string OrderCode { get; set; } = string.Empty;
        
        public int BookingId { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }
        
        [StringLength(20)]
        public string? Status { get; set; } = "Pending"; // Pending, Processing, Success, Failed, Cancelled
        
        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty; // SEPay, VietQR, Cash, BankTransfer
        
        public DateTime? ExpiredAt { get; set; }
        
        public DateTime? ProcessedAt { get; set; }
        
        [StringLength(100)]
        public string? PaymentReference { get; set; } // Mã tham chiếu từ gateway
        
        public string? PaymentData { get; set; } // JSON data từ payment gateway
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        // Navigation properties
        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; } = null!;
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
    
    public static class PaymentOrderStatus
    {
        public const string PENDING = "Pending";
        public const string PAID = "Success";
        public const string EXPIRED = "Cancelled";
        public const string FAILED = "Failed";
        public const string CANCELLED = "Cancelled";
    }
    
    public static class PaymentOrderMethod
    {
        public const string VIETQR = "VietQR";
        public const string SEPAY = "SEPay";
        public const string BANK_TRANSFER = "BankTransfer";
    }
}
