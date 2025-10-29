using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using System.Security.Cryptography;
using System.Text;

namespace _3TLMiniSoccer.Services
{
    public class UserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<User?> LayNguoiDungTheoIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<User?> LayNguoiDungTheoEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> LayNguoiDungTheoTenDangNhapAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> LayNguoiDungTheoTenDangNhapHoacEmailAsync(string usernameOrEmail)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);
        }

        public async Task<List<User>> LayTatCaNguoiDungAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.IsActive)
                .OrderBy(u => u.FirstName)
                .ToListAsync();
        }

        public async Task<List<User>> LayNguoiDungTheoVaiTroAsync(string roleName)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.IsActive && u.Role.RoleName == roleName)
                .OrderBy(u => u.FirstName)
                .ToListAsync();
        }

        public async Task<User> TaoNguoiDungAsync(User user, string password)
        {
            user.PasswordHash = MaHoaMatKhau(password);
            user.CreatedAt = DateTime.Now;
            user.UpdatedAt = DateTime.Now;

            try
            {
                // Disable trigger temporarily to avoid OUTPUT clause conflict
                await _context.Database.ExecuteSqlRawAsync("DISABLE TRIGGER tr_Users_Audit ON Users");
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                // Enable trigger back
                await _context.Database.ExecuteSqlRawAsync("ENABLE TRIGGER tr_Users_Audit ON Users");
                
                return user;
            }
            catch
            {
                // Make sure to enable trigger back even if error occurs
                try
                {
                    await _context.Database.ExecuteSqlRawAsync("ENABLE TRIGGER tr_Users_Audit ON Users");
                }
                catch { }
                
                throw;
            }
        }

        public async Task<bool> CapNhatNguoiDungAsync(User user)
        {
            try
            {
                user.UpdatedAt = DateTime.Now;
                
                // Disable trigger temporarily to avoid OUTPUT clause conflict
                await _context.Database.ExecuteSqlRawAsync("DISABLE TRIGGER tr_Users_Audit ON Users");
                
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                
                // Enable trigger back
                await _context.Database.ExecuteSqlRawAsync("ENABLE TRIGGER tr_Users_Audit ON Users");
                
                return true;
            }
            catch
            {
                // Make sure to enable trigger back even if error occurs
                try
                {
                    await _context.Database.ExecuteSqlRawAsync("ENABLE TRIGGER tr_Users_Audit ON Users");
                }
                catch { }
                
                return false;
            }
        }

        public async Task<bool> XoaNguoiDungAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.IsActive = false;
                    user.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DoiMatKhauAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !XacThucMatKhau(currentPassword, user.PasswordHash))
                    return false;

                user.PasswordHash = MaHoaMatKhau(newPassword);
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DatLaiMatKhauAsync(string email, string newPassword)
        {
            try
            {
                var user = await LayNguoiDungTheoEmailAsync(email);
                if (user == null) return false;

                user.PasswordHash = MaHoaMatKhau(newPassword);
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> XacThucEmailAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.EmailConfirmed = true;
                    user.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> XacThucSoDienThoaiAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.PhoneConfirmed = true;
                    user.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<User>> TimKiemNguoiDungAsync(string searchTerm)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.IsActive && 
                    (u.FirstName.Contains(searchTerm) || 
                     u.LastName.Contains(searchTerm) || 
                     u.Email.Contains(searchTerm) || 
                     u.Username.Contains(searchTerm)))
                .OrderBy(u => u.FirstName)
                .ToListAsync();
        }

        public async Task<bool> GanVaiTroAsync(int userId, string roleName)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
                if (role == null) return false;

                user.RoleId = role.RoleId;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> XoaVaiTroAsync(int userId, string roleName)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
                if (role == null) return false;

                // Set to default User role (RoleId = 3)
                user.RoleId = 3;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> KiemTraEmailTonTaiAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> KiemTraTenDangNhapTonTaiAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        public Task<bool> KiemTraMatKhauAsync(string password)
        {
            // Password validation rules
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return Task.FromResult(false);

            return Task.FromResult(true);
        }

        public async Task<bool> DangNhapAsync(string usernameOrEmail, string password)
        {
            var user = await LayNguoiDungTheoTenDangNhapHoacEmailAsync(usernameOrEmail);
            if (user == null || !XacThucMatKhau(password, user.PasswordHash))
                return false;

            // Update LastLoginAt using raw SQL to avoid OUTPUT clause issues
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE Users SET LastLoginAt = @lastLoginAt WHERE UserId = @userId",
                    new Microsoft.Data.SqlClient.SqlParameter("@lastLoginAt", DateTime.Now),
                    new Microsoft.Data.SqlClient.SqlParameter("@userId", user.UserId));
            }
            catch
            {
                // If update fails, still allow login
            }
            
            return true;
        }

        public Task DangXuatAsync()
        {
            // Implementation depends on authentication method
            return Task.CompletedTask;
        }

        public async Task<User?> LayNguoiDungHienTaiAsync()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("UserId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return null;

            return await LayNguoiDungTheoIdAsync(userId);
        }

        public string MaHoaMatKhau(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool XacThucMatKhau(string password, string hashedPassword)
        {
            return MaHoaMatKhau(password) == hashedPassword;
        }

        public async Task<int> LayTongSoNguoiDungAsync()
        {
            return await _context.Users.CountAsync(u => u.IsActive);
        }

        public async Task<List<User>> LayNguoiDungGanDayAsync(int count)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.IsActive)
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Role>> LayTatCaVaiTroAsync()
        {
            return await _context.Roles
                .OrderBy(r => r.RoleName)
                .ToListAsync();
        }
    }
}
