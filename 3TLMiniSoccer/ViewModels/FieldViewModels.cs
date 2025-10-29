using _3TLMiniSoccer.Models;
using Microsoft.AspNetCore.Http;

namespace _3TLMiniSoccer.ViewModels
{
    public class FieldListViewModel
    {
        public List<FieldItemViewModel> Fields { get; set; } = new List<FieldItemViewModel>();
        public FieldFilterViewModel Filter { get; set; } = new FieldFilterViewModel();
        public FieldStatsViewModel Stats { get; set; } = new FieldStatsViewModel();
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = 12;
        public int TotalFields { get; set; }
    }

    public class FieldItemViewModel
    {
        public int FieldId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string FieldTypeName { get; set; } = string.Empty;
        public int FieldTypeId { get; set; }
        public int PlayerCount { get; set; }
        public decimal BasePrice { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public TimeSpan OpeningTime { get; set; }
        public TimeSpan ClosingTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Additional computed properties
        public string StatusText => Status switch
        {
            "Active" => "Hoạt động",
            "Maintenance" => "Bảo trì", 
            "Inactive" => "Không hoạt động",
            _ => "Không xác định"
        };
        
        public string StatusClass => Status switch
        {
            "Active" => "bg-green-100 text-green-800",
            "Maintenance" => "bg-yellow-100 text-yellow-800",
            "Inactive" => "bg-red-100 text-red-800",
            _ => "bg-gray-100 text-gray-800"
        };
        
        public string FormattedPrice => BasePrice.ToString("N0") + "₫/giờ";
        public string FieldTypeDisplay => FieldTypeName;
        public string SizeDisplay => PlayerCount switch
        {
            5 => "20m x 40m",
            7 => "30m x 50m", 
            11 => "68m x 105m",
            _ => "Tùy chỉnh"
        };
    }

    public class FieldFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public string? FieldType { get; set; }
        public string? Status { get; set; }
        public string? PriceRange { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }

    public class FieldStatsViewModel
    {
        public int TotalFields { get; set; }
        public int ActiveFields { get; set; }
        public int MaintenanceFields { get; set; }
        public int InactiveFields { get; set; }
        
        public double ActivePercentage => TotalFields > 0 ? (double)ActiveFields / TotalFields * 100 : 0;
        public double MaintenancePercentage => TotalFields > 0 ? (double)MaintenanceFields / TotalFields * 100 : 0;
        public double InactivePercentage => TotalFields > 0 ? (double)InactiveFields / TotalFields * 100 : 0;
    }

    public class CreateFieldViewModel
    {
        public string FieldName { get; set; } = string.Empty;
        public int FieldTypeId { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public IFormFile? ImageFile { get; set; }
        public TimeSpan OpeningTime { get; set; } = new TimeSpan(6, 0, 0);
        public TimeSpan ClosingTime { get; set; } = new TimeSpan(23, 0, 0);
    }

    public class UpdateFieldViewModel
    {
        public int FieldId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public int FieldTypeId { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public IFormFile? ImageFile { get; set; }
        public TimeSpan OpeningTime { get; set; } = new TimeSpan(6, 0, 0);
        public TimeSpan ClosingTime { get; set; } = new TimeSpan(23, 0, 0);
    }
}
