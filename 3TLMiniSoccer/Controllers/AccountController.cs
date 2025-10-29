using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using _3TLMiniSoccer.Services;
using _3TLMiniSoccer.ViewModels;
using _3TLMiniSoccer.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;

namespace _3TLMiniSoccer.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserService _userService;
        private readonly NotificationService _notificationService;
        private readonly AuditService _auditService;
        private readonly OAuthService _oauthService;

        public AccountController(
            UserService userService,
            NotificationService notificationService,
            AuditService auditService,
            OAuthService oauthService)
        {
            _userService = userService;
            _notificationService = notificationService;
            _auditService = auditService;
            _oauthService = oauthService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userService.LayNguoiDungTheoTenDangNhapHoacEmailAsync(model.UsernameOrEmail);
                if (user != null && await _userService.DangNhapAsync(model.UsernameOrEmail, model.Password))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim("UserId", user.UserId.ToString())
                    };

                    // Add role claims
                    claims.Add(new Claim(ClaimTypes.Role, user.Role.RoleName));

                    var identity = new ClaimsIdentity(claims, "Cookies");
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync("Cookies", principal, new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe
                    });

                    await _auditService.GhiNhanHanhDongNguoiDungAsync(user.UserId, "Login", "Users", user.UserId);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                if (await _userService.KiemTraEmailTonTaiAsync(model.Email))
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng");
                    return View(model);
                }

                // Check if username already exists
                if (await _userService.KiemTraTenDangNhapTonTaiAsync(model.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập đã được sử dụng");
                    return View(model);
                }

                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    Address = model.Address
                };

                var createdUser = await _userService.TaoNguoiDungAsync(user, model.Password);
                await _userService.GanVaiTroAsync(createdUser.UserId, "User");
                await _auditService.GhiNhanHanhDongNguoiDungAsync(createdUser.UserId, "Register", "Users", createdUser.UserId);

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userService.LayNguoiDungTheoEmailAsync(model.Email);
                if (user != null)
                {
                    // TODO: Implement actual email sending logic
                    // For now, simulate successful email sending
                    TempData["SuccessMessage"] = $"Link khôi phục mật khẩu đã được gửi đến email {model.Email}";
                    return Json(new { success = true, message = "Email đã được gửi thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Email không tồn tại trong hệ thống" });
                }
            }
            return Json(new { success = false, message = "Email không hợp lệ" });
        }

        [HttpPost]
        public IActionResult VerifyResetCode(VerifyResetCodeViewModel model)
        {
            if (ModelState.IsValid)
            {
                // TODO: Implement verification logic
                // For now, simulate successful verification
                return Json(new { success = true, message = "Mã xác thực hợp lệ!" });
            }
            return Json(new { success = false, message = "Mã xác thực không hợp lệ" });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                // TODO: Implement actual password reset logic
                // For now, simulate successful password reset
                return Json(new { success = true, message = "Mật khẩu đã được đặt lại thành công!" });
            }
            return Json(new { success = false, message = "Mật khẩu không hợp lệ" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userService.LayNguoiDungHienTaiAsync();
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var viewModel = new UserEditViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Avatar = user.Avatar,
                Address = user.Address,
                IsActive = user.IsActive,
                CurrentRoles = new List<Role> { user.Role }
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(UserEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userService.LayNguoiDungHienTaiAsync();
                if (user != null)
                {
                    user.Username = model.Username;
                    user.Email = model.Email;
                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;
                    user.PhoneNumber = model.PhoneNumber;
                    user.DateOfBirth = model.DateOfBirth;
                    user.Gender = model.Gender;
                    user.Avatar = model.Avatar;
                    user.Address = model.Address;

                    await _userService.CapNhatNguoiDungAsync(user);
                    await _auditService.GhiNhanHanhDongNguoiDungAsync(user.UserId, "UpdateProfile", "Users", user.UserId);
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                }
            }
            return RedirectToAction("Profile");
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userService.LayNguoiDungHienTaiAsync();
                if (user != null)
                {
                    var success = await _userService.DoiMatKhauAsync(user.UserId, model.CurrentPassword, model.NewPassword);
                    if (success)
                    {
                        await _auditService.GhiNhanHanhDongNguoiDungAsync(user.UserId, "ChangePassword", "Users", user.UserId);
                        TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                        return RedirectToAction("Profile");
                    }
                    else
                    {
                        ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng");
                    }
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // OAuth Login Actions
        [HttpGet]
        public IActionResult GoogleLogin()
        {
            // Let ASP.NET Core OAuth middleware handle the redirect automatically
            // using the CallbackPath configured in Program.cs
            var properties = new AuthenticationProperties();
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public IActionResult FacebookLogin()
        {
            var redirectUrl = Url.Action("FacebookCallback", "Account");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, FacebookDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback()
        {
            try
            {
                var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
                
                if (!result.Succeeded || result.Principal == null)
                {
                    TempData["Error"] = "Đăng nhập Google thất bại. Vui lòng thử lại.";
                    return RedirectToAction("Login");
                }

                var userInfo = ExtractUserInfoFromClaims(result.Principal, "Google");
                var loginResult = await _oauthService.ProcessOAuthDangNhapAsync(userInfo);

                if (loginResult.Success)
                {
                    // Tạo claims cho user
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, userInfo.Id),
                        new Claim(ClaimTypes.Email, userInfo.Email),
                        new Claim(ClaimTypes.Name, userInfo.Name),
                        new Claim("Provider", userInfo.Provider)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    await HttpContext.SignInAsync("Cookies", claimsPrincipal);
                    
                    TempData["Success"] = loginResult.Message;
                    return Redirect(loginResult.RedirectUrl ?? "/");
                }
                else
                {
                    TempData["Error"] = loginResult.Message;
                    return RedirectToAction("Login");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi xử lý đăng nhập: {ex.Message}";
                return RedirectToAction("Login");
            }
        }

        [HttpGet]
        public async Task<IActionResult> FacebookCallback()
        {
            var result = await HttpContext.AuthenticateAsync(FacebookDefaults.AuthenticationScheme);
            
            if (!result.Succeeded)
            {
                TempData["Error"] = "Đăng nhập Facebook thất bại";
                return RedirectToAction("Login");
            }

            var userInfo = ExtractUserInfoFromClaims(result.Principal, "Facebook");
            var loginResult = await _oauthService.ProcessOAuthDangNhapAsync(userInfo);

            if (loginResult.Success)
            {
                // Tạo claims cho user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userInfo.Id),
                    new Claim(ClaimTypes.Email, userInfo.Email),
                    new Claim(ClaimTypes.Name, userInfo.Name),
                    new Claim("Provider", userInfo.Provider)
                };

                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                await HttpContext.SignInAsync("Cookies", claimsPrincipal);
                
                TempData["Success"] = loginResult.Message;
                return Redirect(loginResult.RedirectUrl ?? "/");
            }
            else
            {
                TempData["Error"] = loginResult.Message;
                return RedirectToAction("Login");
            }
        }

        private OAuthUserInfo ExtractUserInfoFromClaims(ClaimsPrincipal principal, string provider)
        {
            var claims = principal.Claims.ToList();
            
            return new OAuthUserInfo
            {
                Id = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "",
                Email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "",
                Name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "",
                FirstName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value ?? "",
                LastName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value ?? "",
                Picture = claims.FirstOrDefault(c => c.Type == "picture")?.Value ?? "",
                Provider = provider,
                Locale = claims.FirstOrDefault(c => c.Type == "locale")?.Value ?? "",
                EmailVerified = claims.FirstOrDefault(c => c.Type == "email_verified")?.Value == "true"
            };
        }
    }
}
