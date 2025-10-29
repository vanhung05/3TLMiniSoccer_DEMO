using System.ComponentModel.DataAnnotations;
using _3TLMiniSoccer.Models;

namespace _3TLMiniSoccer.ViewModels
{
    public class ProductListViewModel
    {
        public List<ProductItemViewModel> Products { get; set; } = new List<ProductItemViewModel>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public ProductFilterViewModel Filter { get; set; } = new ProductFilterViewModel();
    }

    public class ProductItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? Category { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public string SKU => $"PRD-{ProductId:D3}";
        public string StatusText => IsAvailable ? "Hoạt động" : "Không hoạt động";
        public string StatusClass => IsAvailable ? "bg-green-100 text-green-800" : "bg-red-100 text-red-800";
        public string FormattedPrice => Price.ToString("N0") + " đ";
        public int StockQuantity { get; set; } = 0; // Tạm thời để 0, có thể mở rộng sau
    }

    public class ProductFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class CreateProductViewModel
    {
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự")]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0")]
        public decimal Price { get; set; }

        [StringLength(50, ErrorMessage = "Thể loại không được vượt quá 50 ký tự")]
        public string? Category { get; set; }

        public bool IsAvailable { get; set; } = true;

        public IFormFile? ImageFile { get; set; }
    }

    public class UpdateProductViewModel
    {
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự")]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0")]
        public decimal Price { get; set; }

        [StringLength(50, ErrorMessage = "Thể loại không được vượt quá 50 ký tự")]
        public string? Category { get; set; }

        public bool IsAvailable { get; set; }

        public IFormFile? ImageFile { get; set; }
        public string? CurrentImageUrl { get; set; }
    }

    public class ProductStatsViewModel
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int InactiveProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public List<CategoryStatsViewModel> CategoryStats { get; set; } = new List<CategoryStatsViewModel>();
    }

    public class CategoryStatsViewModel
    {
        public string Category { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }
}
