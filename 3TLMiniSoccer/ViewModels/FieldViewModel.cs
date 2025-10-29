using System.ComponentModel.DataAnnotations;
using _3TLMiniSoccer.Models;

namespace _3TLMiniSoccer.ViewModels
{
    public class FieldViewModel
    {
        public int FieldId { get; set; }

        [Required(ErrorMessage = "Tên sân là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên sân không được vượt quá 100 ký tự")]
        [Display(Name = "Tên sân")]
        public string FieldName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại sân là bắt buộc")]
        [Display(Name = "Loại sân")]
        public int FieldTypeId { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Active";

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [StringLength(255, ErrorMessage = "URL hình ảnh không được vượt quá 255 ký tự")]
        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }

        // Navigation properties for display
        public FieldType? FieldType { get; set; }
        public List<FieldSchedule>? FieldSchedules { get; set; }
    }

    public class FieldTypeViewModel
    {
        public int FieldTypeId { get; set; }

        [Required(ErrorMessage = "Tên loại sân là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên loại sân không được vượt quá 50 ký tự")]
        [Display(Name = "Tên loại sân")]
        public string TypeName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Mô tả không được vượt quá 200 ký tự")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Số người chơi là bắt buộc")]
        [Range(1, 22, ErrorMessage = "Số người chơi phải từ 1 đến 22")]
        [Display(Name = "Số người chơi")]
        public int PlayerCount { get; set; }

        [Required(ErrorMessage = "Giá cơ bản là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá cơ bản phải lớn hơn 0")]
        [Display(Name = "Giá cơ bản")]
        [DataType(DataType.Currency)]
        public decimal BasePrice { get; set; }

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;

        // Navigation properties for display
        public List<Field>? Fields { get; set; }
        public List<PricingRule>? PricingRules { get; set; }
    }


    public class TimeSlotViewModel
    {
        public int TimeSlotId { get; set; }

        [Required(ErrorMessage = "Giờ bắt đầu là bắt buộc")]
        [Display(Name = "Giờ bắt đầu")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Giờ kết thúc là bắt buộc")]
        [Display(Name = "Giờ kết thúc")]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Thời lượng là bắt buộc")]
        [Range(1, 480, ErrorMessage = "Thời lượng phải từ 1 đến 480 phút")]
        [Display(Name = "Thời lượng (phút)")]
        public int Duration { get; set; }

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;

        // Navigation properties for display
        public List<PricingRule>? PricingRules { get; set; }
        public List<FieldSchedule>? FieldSchedules { get; set; }
    }

    public class PricingRuleViewModel
    {
        public int PricingRuleId { get; set; }

        [Required(ErrorMessage = "Loại sân là bắt buộc")]
        [Display(Name = "Loại sân")]
        public int FieldTypeId { get; set; }

        [Required(ErrorMessage = "Giờ bắt đầu là bắt buộc")]
        [Display(Name = "Giờ bắt đầu")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Giờ kết thúc là bắt buộc")]
        [Display(Name = "Giờ kết thúc")]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Thứ trong tuần là bắt buộc")]
        [Range(1, 7, ErrorMessage = "Thứ trong tuần phải từ 1 đến 7")]
        [Display(Name = "Thứ trong tuần")]
        public int DayOfWeek { get; set; }

        [Required(ErrorMessage = "Giá là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Giá")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Display(Name = "Giờ cao điểm")]
        public bool IsPeakHour { get; set; } = false;

        [Range(1.0, 5.0, ErrorMessage = "Hệ số tăng giá phải từ 1.0 đến 5.0")]
        [Display(Name = "Hệ số tăng giá")]
        public decimal PeakMultiplier { get; set; } = 1.0m;

        [Required(ErrorMessage = "Ngày hiệu lực là bắt buộc")]
        [Display(Name = "Ngày hiệu lực")]
        [DataType(DataType.Date)]
        public DateTime EffectiveFrom { get; set; }

        [Display(Name = "Ngày hết hiệu lực")]
        [DataType(DataType.Date)]
        public DateTime? EffectiveTo { get; set; }

        // Navigation properties for display
        public FieldType? FieldType { get; set; }
        
        // Computed property for easy display
        public string FieldTypeName => FieldType?.TypeName ?? "N/A";
    }
}
