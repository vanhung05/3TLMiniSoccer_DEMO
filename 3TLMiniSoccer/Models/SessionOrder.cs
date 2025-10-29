using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _3TLMiniSoccer.Models
{
    public class SessionOrder
    {
        [Key]
        public int SessionOrderId { get; set; }

        [Required]
        public int SessionId { get; set; }

        public int? OrderId { get; set; }  // NULL nếu thanh toán ngay

        [Required]
        [StringLength(20)]
        public string PaymentType { get; set; } = string.Empty; // 'Immediate' hoặc 'Consolidated'

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual BookingSession Session { get; set; } = null!;
        public virtual Order? Order { get; set; }
        public virtual ICollection<SessionOrderItem> SessionOrderItems { get; set; } = new List<SessionOrderItem>();
    }

    public static class SessionOrderPaymentType
    {
        public const string IMMEDIATE = "Immediate";
        public const string CONSOLIDATED = "Consolidated";
    }

    public static class SessionOrderPaymentStatus
    {
        public const string PENDING = "Pending";
        public const string PAID = "Paid";
        public const string FAILED = "Failed";
        public const string CANCELLED = "Cancelled";
    }
}
