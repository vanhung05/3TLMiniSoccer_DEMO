using System.ComponentModel.DataAnnotations;
using _3TLMiniSoccer.Models;

namespace _3TLMiniSoccer.ViewModels
{
    public class BookingViewModel
    {
        public int BookingId { get; set; }
        
        [Display(Name = "Mã đặt sân")]
        public string BookingCode { get; set; } = string.Empty;

        [Display(Name = "Khách hàng")]
        public int? UserId { get; set; }

        [Display(Name = "Tên khách")]
        public string? GuestName { get; set; }

        [Display(Name = "Số điện thoại")]
        public string? GuestPhone { get; set; }

        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? GuestEmail { get; set; }

        [Required(ErrorMessage = "Sân bóng là bắt buộc")]
        [Display(Name = "Sân bóng")]
        public int FieldId { get; set; }

        [Required(ErrorMessage = "Ngày đặt là bắt buộc")]
        [Display(Name = "Ngày đặt")]
        [DataType(DataType.Date)]
        public DateTime BookingDate { get; set; }


        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        [Display(Name = "Thời gian bắt đầu")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        [Display(Name = "Thời gian kết thúc")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Pending";

        [Required(ErrorMessage = "Thời gian là bắt buộc")]
        [Display(Name = "Thời gian (phút)")]
        [Range(1, 480, ErrorMessage = "Thời gian phải từ 1 đến 480 phút (8 giờ)")]
        public int Duration { get; set; }

        [Required(ErrorMessage = "Tổng tiền là bắt buộc")]
        [Display(Name = "Tổng tiền")]
        [DataType(DataType.Currency)]
        public decimal TotalPrice { get; set; }

        [Display(Name = "Mã giảm giá")]
        public string? VoucherCode { get; set; }

        [Display(Name = "Giá trị giảm giá")]
        public decimal VoucherDiscount { get; set; } = 0;

        [Display(Name = "Trạng thái thanh toán")]
        public string PaymentStatus { get; set; } = "Pending";

        [Display(Name = "Phương thức thanh toán")]
        public string? PaymentMethod { get; set; }

        [Display(Name = "Ghi chú")]
        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? Notes { get; set; }

        // Navigation properties for display
        public User? User { get; set; }
        public Field? Field { get; set; }
        public List<BookingSession>? BookingSessions { get; set; }
    }

    public class BookingCreateViewModel
    {
        [Required(ErrorMessage = "Sân bóng là bắt buộc")]
        [Display(Name = "Sân bóng")]
        public int FieldId { get; set; }

        [Required(ErrorMessage = "Ngày đặt là bắt buộc")]
        [Display(Name = "Ngày đặt")]
        [DataType(DataType.Date)]
        public DateTime BookingDate { get; set; }


        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        [Display(Name = "Thời gian bắt đầu")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        [Display(Name = "Thời gian kết thúc")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Tên khách")]
        public string? GuestName { get; set; }

        [Display(Name = "Số điện thoại")]
        public string? GuestPhone { get; set; }

        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? GuestEmail { get; set; }

        [Display(Name = "Ghi chú")]
        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? Notes { get; set; }

        // For display
        public List<Field>? AvailableFields { get; set; }
        public decimal? CalculatedPrice { get; set; }
    }

    public class BookingSearchViewModel
    {
        [Display(Name = "Từ ngày")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Đến ngày")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Sân bóng")]
        public int? FieldId { get; set; }

        [Display(Name = "Trạng thái")]
        public string? Status { get; set; }

        [Display(Name = "Tìm kiếm")]
        public string? SearchTerm { get; set; }

        // For display
        public List<Field>? Fields { get; set; }
        public List<string>? StatusOptions { get; set; }
    }
}
