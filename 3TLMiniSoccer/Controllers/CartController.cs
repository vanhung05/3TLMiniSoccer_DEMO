using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3TLMiniSoccer.Services;
using _3TLMiniSoccer.Models;
using _3TLMiniSoccer.Data;
using Microsoft.EntityFrameworkCore;

namespace _3TLMiniSoccer.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly CartService _cartService;
        private readonly ApplicationDbContext _context;
        private readonly NotificationHubService _notificationHubService;

        public CartController(CartService cartService, ApplicationDbContext context, NotificationHubService notificationHubService)
        {
            _cartService = cartService;
            _context = context;
            _notificationHubService = notificationHubService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var cartItems = await _cartService.LayGioHangNguoiDungAsync(userId);
            var cartTotal = await _cartService.LayTongGioHangAsync(userId);
            
            ViewBag.CartTotal = cartTotal;
            return View(cartItems);
        }

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var cartCount = await _cartService.LaySoLuongGioHangAsync(userId);
                return Json(new { count = cartCount });
            }
            catch (Exception)
            {
                return Json(new { count = 0 });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                await _cartService.ThemVaoGioHangAsync(userId, productId, quantity);
                
                // Check if this is an AJAX request
                if (Request.Headers["X-Requested-With"].ToString() == "XMLHttpRequest" || 
                    Request.Headers["Content-Type"].ToString().Contains("application/x-www-form-urlencoded"))
                {
                    return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng." });
                }
                
                TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Check if this is an AJAX request
                if (Request.Headers["X-Requested-With"].ToString() == "XMLHttpRequest" || 
                    Request.Headers["Content-Type"].ToString().Contains("application/x-www-form-urlencoded"))
                {
                    return Json(new { success = false, message = "Có lỗi xảy ra khi thêm vào giỏ hàng." });
                }
                
                TempData["Error"] = "Có lỗi xảy ra khi thêm vào giỏ hàng.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var result = await _cartService.XoaKhoiGioHangAsync(cartItemId);
            
            // Check if this is an AJAX request
            if (Request.Headers["X-Requested-With"].ToString() == "XMLHttpRequest")
            {
                if (result)
                {
                    return Json(new { success = true, message = "Đã xóa sản phẩm khỏi giỏ hàng." });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa sản phẩm khỏi giỏ hàng." });
                }
            }
            
            if (result)
            {
                TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            }
            else
            {
                TempData["Error"] = "Không thể xóa sản phẩm khỏi giỏ hàng.";
            }
            
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var result = await _cartService.XoaGioHangNguoiDungAsync(userId);
            
            // Check if this is an AJAX request
            if (Request.Headers["X-Requested-With"].ToString() == "XMLHttpRequest")
            {
                if (result)
                {
                    return Json(new { success = true, message = "Đã xóa tất cả sản phẩm khỏi giỏ hàng." });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa giỏ hàng." });
                }
            }
            
            if (result)
            {
                TempData["Success"] = "Đã xóa tất cả sản phẩm khỏi giỏ hàng.";
            }
            else
            {
                TempData["Error"] = "Không thể xóa giỏ hàng.";
            }
            
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var result = await _cartService.CapNhatGioHangAsync(cartItemId, quantity);
            
            // Check if this is an AJAX request
            if (Request.Headers["X-Requested-With"].ToString() == "XMLHttpRequest")
            {
                if (result)
                {
                    return Json(new { success = true, message = "Đã cập nhật số lượng sản phẩm." });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể cập nhật số lượng sản phẩm." });
                }
            }
            
            if (result)
            {
                TempData["Success"] = "Đã cập nhật số lượng sản phẩm.";
            }
            else
            {
                TempData["Error"] = "Không thể cập nhật số lượng sản phẩm.";
            }
            
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(int? bookingId = null, string? notes = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var cartItems = await _cartService.LayGioHangNguoiDungAsync(userId);
                
                if (!cartItems.Any())
                {
                    TempData["Error"] = "Giỏ hàng trống!";
                    return RedirectToAction("Index");
                }

                // Tạo đơn hàng
                var orderCode = $"ORD_{DateTime.Now:yyyyMMddHHmmss}";
                var totalAmount = cartItems.Sum(ci => ci.Quantity * ci.Product.Price);
                
                var order = new Order
                {
                    OrderCode = orderCode,
                    BookingId = bookingId ?? 0, // Có thể null nếu không liên kết với booking
                    UserId = userId,
                    TotalAmount = totalAmount,
                    Status = "Pending",
                    PaymentStatus = "Pending",
                    Notes = notes,
                    CreatedAt = DateTime.Now
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Tạo order items
                foreach (var cartItem in cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.Product.Price,
                        TotalPrice = cartItem.Quantity * cartItem.Product.Price
                    };
                    _context.OrderItems.Add(orderItem);
                }

                await _context.SaveChangesAsync();

                // Gửi thông báo realtime cho admin
                try
                {
                    var user = await _context.Users.FindAsync(userId);
                    var customerName = user != null ? $"{user.FirstName} {user.LastName}" : "Khách hàng";
                    
                    await _notificationHubService.guiTBDonHangMoiAsync(
                        order.OrderId,
                        customerName,
                        order.TotalAmount
                    );
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the order
                    System.Diagnostics.Debug.WriteLine($"Error sending notification: {ex.Message}");
                }

                // Xóa giỏ hàng
                await _cartService.XoaGioHangNguoiDungAsync(userId);

                TempData["Success"] = "Đặt hàng thành công!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during checkout: {ex.Message}");
                TempData["Error"] = "Có lỗi xảy ra khi đặt hàng!";
                return RedirectToAction("Index");
            }
        }
    }
}
