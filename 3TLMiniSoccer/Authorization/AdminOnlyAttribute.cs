using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using _3TLMiniSoccer.Services;
using System.Security.Claims;

namespace _3TLMiniSoccer.Authorization
{
    public class AdminOnlyAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Check if user is authenticated
            if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Get user service from DI
            var userService = context.HttpContext.RequestServices.GetRequiredService<UserService>();
            
            try
            {
                // Get current user
                var user = await userService.LayNguoiDungHienTaiAsync();
                
                if (user == null)
                {
                    context.Result = new RedirectToActionResult("Login", "Account", null);
                    return;
                }

                // Check if user is Admin (RoleId = 1)
                if (user.RoleId != 1)
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                    return;
                }
            }
            catch
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }
        }
    }
}
