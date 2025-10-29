using System.ComponentModel.DataAnnotations;
using _3TLMiniSoccer.Models;

namespace _3TLMiniSoccer.ViewModels
{
    public class ProductViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự")]
        [Display(Name = "Tên sản phẩm")]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Giá là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Giá")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [StringLength(50, ErrorMessage = "Danh mục không được vượt quá 50 ký tự")]
        [Display(Name = "Danh mục")]
        public string? Category { get; set; }

        [StringLength(255, ErrorMessage = "URL hình ảnh không được vượt quá 255 ký tự")]
        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Có sẵn")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Ngày cập nhật")]
        public DateTime UpdatedAt { get; set; }

        [Display(Name = "Người tạo")]
        public int CreatedBy { get; set; }

        // Navigation properties for display
        public User? CreatedByUser { get; set; }
        public List<OrderItem>? OrderItems { get; set; }
    }

    public class ProductCreateViewModel
    {
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự")]
        [Display(Name = "Tên sản phẩm")]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Giá là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Giá")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [StringLength(50, ErrorMessage = "Danh mục không được vượt quá 50 ký tự")]
        [Display(Name = "Danh mục")]
        public string? Category { get; set; }

        [StringLength(255, ErrorMessage = "URL hình ảnh không được vượt quá 255 ký tự")]
        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Có sẵn")]
        public bool IsAvailable { get; set; } = true;

        // For display
        public List<string>? AvailableCategories { get; set; }
    }



    public class OrderCreateViewModel
    {
        [Required(ErrorMessage = "Đặt sân là bắt buộc")]
        [Display(Name = "Đặt sân")]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "Khách hàng là bắt buộc")]
        [Display(Name = "Khách hàng")]
        public int UserId { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Sản phẩm")]
        public List<OrderItemCreateViewModel> OrderItems { get; set; } = new List<OrderItemCreateViewModel>();

        // For display
        public List<Booking>? AvailableBookings { get; set; }
        public List<User>? AvailableUsers { get; set; }
        public List<Product>? AvailableProducts { get; set; }
    }

    public class OrderItemCreateViewModel
    {
        [Required(ErrorMessage = "Sản phẩm là bắt buộc")]
        [Display(Name = "Sản phẩm")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        [Display(Name = "Số lượng")]
        public int Quantity { get; set; }

        // For display
        public Product? Product { get; set; }
    }
}
