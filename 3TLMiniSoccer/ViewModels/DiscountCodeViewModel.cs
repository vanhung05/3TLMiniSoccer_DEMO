using System.ComponentModel.DataAnnotations;
using _3TLMiniSoccer.Models;

namespace _3TLMiniSoccer.ViewModels
{
    public class DiscountCodeViewModel
    {
        public int DiscountCodeId { get; set; }

        [Required(ErrorMessage = "Mã khuyến mãi là bắt buộc")]
        [StringLength(50, ErrorMessage = "Mã khuyến mãi không được vượt quá 50 ký tự")]
        [Display(Name = "Mã khuyến mãi")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên mã khuyến mãi là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên mã khuyến mãi không được vượt quá 100 ký tự")]
        [Display(Name = "Tên mã khuyến mãi")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Loại giảm giá là bắt buộc")]
        [StringLength(20, ErrorMessage = "Loại giảm giá không được vượt quá 20 ký tự")]
        [Display(Name = "Loại giảm giá")]
        public string DiscountType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá trị giảm giá là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0")]
        [Display(Name = "Giá trị giảm giá")]
        [DataType(DataType.Currency)]
        public decimal DiscountValue { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá trị đơn hàng tối thiểu phải lớn hơn 0")]
        [Display(Name = "Giá trị đơn hàng tối thiểu")]
        [DataType(DataType.Currency)]
        public decimal? MinOrderAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá trị giảm giá tối đa phải lớn hơn 0")]
        [Display(Name = "Giá trị giảm giá tối đa")]
        [DataType(DataType.Currency)]
        public decimal? MaxDiscountAmount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Giới hạn sử dụng phải lớn hơn 0")]
        [Display(Name = "Giới hạn sử dụng")]
        public int? UsageLimit { get; set; }

        [Display(Name = "Số lần đã sử dụng")]
        public int UsedCount { get; set; } = 0;

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.DateTime)]
        public DateTime ValidFrom { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.DateTime)]
        public DateTime ValidTo { get; set; }

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Ngày cập nhật")]
        public DateTime UpdatedAt { get; set; }

        [Display(Name = "Người tạo")]
        public int? CreatedBy { get; set; }

        // Navigation properties for display
        public User? CreatedByUser { get; set; }
        public List<DiscountCodeUsage>? DiscountCodeUsages { get; set; }
    }

    public class DiscountCodeCreateViewModel
    {
        [Required(ErrorMessage = "Mã khuyến mãi là bắt buộc")]
        [StringLength(50, ErrorMessage = "Mã khuyến mãi không được vượt quá 50 ký tự")]
        [Display(Name = "Mã khuyến mãi")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên mã khuyến mãi là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên mã khuyến mãi không được vượt quá 100 ký tự")]
        [Display(Name = "Tên mã khuyến mãi")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Loại giảm giá là bắt buộc")]
        [Display(Name = "Loại giảm giá")]
        public string DiscountType { get; set; } = "Percentage";

        [Required(ErrorMessage = "Giá trị giảm giá là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0")]
        [Display(Name = "Giá trị giảm giá")]
        [DataType(DataType.Currency)]
        public decimal DiscountValue { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá trị đơn hàng tối thiểu phải lớn hơn 0")]
        [Display(Name = "Giá trị đơn hàng tối thiểu")]
        [DataType(DataType.Currency)]
        public decimal? MinOrderAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá trị giảm giá tối đa phải lớn hơn 0")]
        [Display(Name = "Giá trị giảm giá tối đa")]
        [DataType(DataType.Currency)]
        public decimal? MaxDiscountAmount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Giới hạn sử dụng phải lớn hơn 0")]
        [Display(Name = "Giới hạn sử dụng")]
        public int? UsageLimit { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.DateTime)]
        public DateTime ValidFrom { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.DateTime)]
        public DateTime ValidTo { get; set; }

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;

        // For display
        public List<string>? AvailableDiscountTypes { get; set; }
    }

    public class DiscountCodeUsageViewModel
    {
        public int UsageId { get; set; }

        [Required(ErrorMessage = "Mã khuyến mãi là bắt buộc")]
        [Display(Name = "Mã khuyến mãi")]
        public int DiscountCodeId { get; set; }

        [Required(ErrorMessage = "Đặt sân là bắt buộc")]
        [Display(Name = "Đặt sân")]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "Khách hàng là bắt buộc")]
        [Display(Name = "Khách hàng")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Số tiền giảm giá là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Số tiền giảm giá phải lớn hơn 0")]
        [Display(Name = "Số tiền giảm giá")]
        [DataType(DataType.Currency)]
        public decimal DiscountAmount { get; set; }

        [Display(Name = "Ngày sử dụng")]
        public DateTime UsedAt { get; set; }

        // Navigation properties for display
        public DiscountCode? DiscountCode { get; set; }
        public Booking? Booking { get; set; }
        public User? User { get; set; }
    }

    public class ContactViewModel
    {
        public int ContactId { get; set; }

        [Required(ErrorMessage = "Tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        [Display(Name = "Tên")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        [Display(Name = "Tiêu đề")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung là bắt buộc")]
        [Display(Name = "Nội dung")]
        public string Message { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Trạng thái không được vượt quá 20 ký tự")]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "New";

        [Display(Name = "Phản hồi")]
        public string? Response { get; set; }

        [Display(Name = "Ngày phản hồi")]
        public DateTime? RespondedAt { get; set; }

        [Display(Name = "Người phản hồi")]
        public int? RespondedBy { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; }

        // Navigation properties for display
        public User? RespondedByUser { get; set; }
    }

    public class ContactResponseViewModel
    {
        public int ContactId { get; set; }

        [Required(ErrorMessage = "Phản hồi là bắt buộc")]
        [Display(Name = "Phản hồi")]
        public string Response { get; set; } = string.Empty;
    }
}
