using Microsoft.AspNetCore.Mvc;
using _3TLMiniSoccer.Services;
using _3TLMiniSoccer.ViewModels;

namespace _3TLMiniSoccer.Controllers
{
    public class ShopController : Controller
    {
        private readonly ProductService _productService;
        private readonly ILogger<ShopController> _logger;

        public ShopController(ProductService productService, ILogger<ShopController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(ProductFilterViewModel filter)
        {
            try
            {
                // Set default values for shop page
                if (filter.PageSize == 0)
                    filter.PageSize = 12; // Show 12 products per page for shop
                
                if (filter.PageNumber == 0)
                    filter.PageNumber = 1;

                // Only show available products in shop
                filter.Status = "Available";

                var productData = await _productService.LayDanhSachSanPhamAsync(filter);
                var categories = await _productService.LayTatCaDanhMucAsync();
                
                ViewBag.Categories = categories;
                return View(productData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading shop products");
                return View(new ProductListViewModel());
            }
        }

        public async Task<IActionResult> Product(int id)
        {
            try
            {
                var product = await _productService.LaySanPhamTheoIdAsync(id);
                if (product == null)
                {
                    return NotFound();
                }
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product details for ID: {ProductId}", id);
                return NotFound();
            }
        }

        public IActionResult Cart()
        {
            return View();
        }

        public IActionResult Checkout()
        {
            return View();
        }
    }
}
