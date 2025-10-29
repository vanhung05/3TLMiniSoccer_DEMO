using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using Microsoft.EntityFrameworkCore;

namespace _3TLMiniSoccer.Services
{
    public class OAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OAuthService> _logger;

        public OAuthService(ApplicationDbContext context, ILogger<OAuthService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<OAuthLoginResult> ProcessOAuthLoginAsync(OAuthUserInfo userInfo)
        {
            try
            {
                // Tìm user theo provider key
                var existingUser = await FindUserByProviderKeyAsync(userInfo.Provider, userInfo.Id);
                
                if (existingUser != null)
                {
                    // Cập nhật thông tin user
                    await UpdateUserFromOAuthAsync(existingUser, userInfo);
                    
                    _logger.LogInformation($"OAuth login successful for existing user: {existingUser.Email}");
                    return new OAuthLoginResult
                    {
                        Success = true,
                        Message = "Đăng nhập thành công",
                        UserInfo = userInfo,
                        RedirectUrl = "/"
                    };
                }

                // Kiểm tra xem email đã tồn tại chưa
                var userByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == userInfo.Email);

                if (userByEmail != null)
                {
                    // Liên kết OAuth account với user hiện tại
                    await LinkOAuthAccountAsync(userByEmail.UserId, userInfo);
                    
                    _logger.LogInformation($"OAuth account linked to existing user: {userByEmail.Email}");
                    return new OAuthLoginResult
                    {
                        Success = true,
                        Message = "Tài khoản OAuth đã được liên kết",
                        UserInfo = userInfo,
                        RedirectUrl = "/"
                    };
                }

                // Tạo user mới
                var newUser = await CreateUserFromOAuthAsync(userInfo);
                
                _logger.LogInformation($"New user created from OAuth: {newUser.Email}");
                return new OAuthLoginResult
                {
                    Success = true,
                    Message = "Tài khoản mới đã được tạo",
                    UserInfo = userInfo,
                    RedirectUrl = "/"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing OAuth login: {ex.Message}");
                return new OAuthLoginResult
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi đăng nhập"
                };
            }
        }

        public async Task<User?> FindUserByProviderKeyAsync(string provider, string providerKey)
        {
            var socialLogin = await _context.SocialLogins
                .Include(sl => sl.User)
                .FirstOrDefaultAsync(sl => sl.Provider == provider && sl.ProviderKey == providerKey);

            return socialLogin?.User;
        }

        public async Task<User> CreateUserFromOAuthAsync(OAuthUserInfo userInfo)
        {
            var user = new User
            {
                Username = userInfo.Email, // Sử dụng email làm username
                Email = userInfo.Email,
                PasswordHash = string.Empty, // OAuth user không cần password
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                Avatar = userInfo.Picture,
                IsActive = true,
                EmailConfirmed = userInfo.EmailVerified,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                LastLoginAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Tạo SocialLogin record
            await LinkOAuthAccountAsync(user.UserId, userInfo);

            return user;
        }

        public async Task<SocialLogin> LinkOAuthAccountAsync(int userId, OAuthUserInfo userInfo)
        {
            var socialLogin = new SocialLogin
            {
                UserId = userId,
                Provider = userInfo.Provider,
                ProviderKey = userInfo.Id,
                ProviderDisplayName = userInfo.Name,
                ProfilePictureUrl = userInfo.Picture,
                CreatedAt = DateTime.Now,
                LastLoginAt = DateTime.Now
            };

            _context.SocialLogins.Add(socialLogin);
            await _context.SaveChangesAsync();

            return socialLogin;
        }

        public async Task UpdateUserFromOAuthAsync(User user, OAuthUserInfo userInfo)
        {
            // Cập nhật thông tin cơ bản
            if (string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(userInfo.FirstName))
                user.FirstName = userInfo.FirstName;
            
            if (string.IsNullOrEmpty(user.LastName) && !string.IsNullOrEmpty(userInfo.LastName))
                user.LastName = userInfo.LastName;
            
            if (string.IsNullOrEmpty(user.Avatar) && !string.IsNullOrEmpty(userInfo.Picture))
                user.Avatar = userInfo.Picture;

            // Cập nhật email confirmation nếu OAuth provider xác nhận
            if (userInfo.EmailVerified && !user.EmailConfirmed)
                user.EmailConfirmed = true;

            user.UpdatedAt = DateTime.Now;
            user.LastLoginAt = DateTime.Now;

            // Cập nhật SocialLogin record
            var socialLogin = await _context.SocialLogins
                .FirstOrDefaultAsync(sl => sl.UserId == user.UserId && sl.Provider == userInfo.Provider);

            if (socialLogin != null)
            {
                socialLogin.ProviderDisplayName = userInfo.Name;
                socialLogin.ProfilePictureUrl = userInfo.Picture;
                socialLogin.LastLoginAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        // Additional methods for AdminController
        public async Task<OAuthLoginResult> ProcessOAuthDangNhapAsync(OAuthUserInfo userInfo)
        {
            return await ProcessOAuthLoginAsync(userInfo);
        }
    }
}
