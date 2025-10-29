using System.ComponentModel.DataAnnotations;

namespace _3TLMiniSoccer.ViewModels
{
    // Main ViewModel for Categories page
    public class CategoryListViewModel
    {
        public CategoryOverviewStatsViewModel Stats { get; set; } = new CategoryOverviewStatsViewModel();
        public List<ProductCategoryItemViewModel> ProductCategories { get; set; } = new List<ProductCategoryItemViewModel>();
        public List<FieldCategoryItemViewModel> FieldCategories { get; set; } = new List<FieldCategoryItemViewModel>();
    }

    // Stats for quick overview
    public class CategoryOverviewStatsViewModel
    {
        public int TotalCategories { get; set; }
        public int ProductCategories { get; set; }
        public int FieldCategories { get; set; }
    }


    // Product Category Item (from string categories)
    public class ProductCategoryItemViewModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductCount { get; set; }
        public bool IsActive { get; set; }

        public string StatusText => IsActive ? "Hoạt động" : "Không hoạt động";
        public string StatusClass => IsActive ? "bg-green-100 text-green-800" : "bg-red-100 text-red-800";
    }

    // Field Category Item (FieldType)
    public class FieldCategoryItemViewModel
    {
        public int FieldTypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int PlayerCount { get; set; }
        public decimal BasePrice { get; set; }
        public int FieldCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string StatusText => IsActive ? "Hoạt động" : "Không hoạt động";
        public string StatusClass => IsActive ? "bg-green-100 text-green-800" : "bg-yellow-100 text-yellow-800";
        public string FormattedPrice => BasePrice.ToString("N0") + " VNĐ";
    }


    // Create Field Category (FieldType)
    public class CreateFieldCategoryViewModel
    {
        [Required(ErrorMessage = "Tên loại sân là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên loại sân không được vượt quá 50 ký tự")]
        public string TypeName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Mô tả không được vượt quá 200 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Số người chơi là bắt buộc")]
        [Range(1, 50, ErrorMessage = "Số người chơi phải từ 1 đến 50")]
        public int PlayerCount { get; set; }

        [Required(ErrorMessage = "Giá cơ bản là bắt buộc")]
        [Range(0, 10000000, ErrorMessage = "Giá cơ bản phải từ 0 đến 10,000,000")]
        public decimal BasePrice { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // Update Field Category (FieldType)
    public class UpdateFieldCategoryViewModel
    {
        public int FieldTypeId { get; set; }

        [Required(ErrorMessage = "Tên loại sân là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên loại sân không được vượt quá 50 ký tự")]
        public string TypeName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Mô tả không được vượt quá 200 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Số người chơi là bắt buộc")]
        [Range(1, 50, ErrorMessage = "Số người chơi phải từ 1 đến 50")]
        public int PlayerCount { get; set; }

        [Required(ErrorMessage = "Giá cơ bản là bắt buộc")]
        [Range(0, 10000000, ErrorMessage = "Giá cơ bản phải từ 0 đến 10,000,000")]
        public decimal BasePrice { get; set; }

        public bool IsActive { get; set; }
    }

    // Create Product Category (just string)
    public class CreateProductCategoryViewModel
    {
        [Required(ErrorMessage = "Tên thể loại là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên thể loại không được vượt quá 50 ký tự")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Mô tả không được vượt quá 200 ký tự")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
