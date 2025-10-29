using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _3TLMiniSoccer.Models
{
    public class BookingSession
    {
        [Key]
        public int SessionId { get; set; }

        [Required]
        public int BookingId { get; set; }

        public DateTime? CheckInTime { get; set; }        // Thời gian check-in thực tế
        public DateTime? CheckOutTime { get; set; }      // Thời gian check-out thực tế
        
        public int? StaffId { get; set; }                // Nhân viên check-in/out
        
        [StringLength(500)]
        public string? Notes { get; set; }               // Ghi chú

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Booking Booking { get; set; } = null!;
        public virtual User? Staff { get; set; }
        public virtual ICollection<SessionOrder> SessionOrders { get; set; } = new List<SessionOrder>();
    }

    public static class BookingSessionStatus
    {
        public const string ACTIVE = "Active";
        public const string COMPLETED = "Completed";
        public const string CANCELLED = "Cancelled";
    }
}
