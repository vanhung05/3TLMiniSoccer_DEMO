using Microsoft.AspNetCore.Mvc;
using _3TLMiniSoccer.Services;
using System.Security.Claims;

namespace _3TLMiniSoccer.ViewComponents
{
    public class CartCountViewComponent : ViewComponent
    {
        private readonly CartService _cartService;

        public CartCountViewComponent(CartService cartService)
        {
            _cartService = cartService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var claimsPrincipal = User as ClaimsPrincipal;
                var userIdClaim = claimsPrincipal?.FindFirst("UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    var cartCount = await _cartService.LaySoLuongGioHangAsync(userId);
                    return View(cartCount);
                }
            }
            
            return View(0);
        }
    }
}
