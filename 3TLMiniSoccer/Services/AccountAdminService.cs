using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using _3TLMiniSoccer.ViewModels;
using _3TLMiniSoccer.Services;

namespace _3TLMiniSoccer.Services
{
    public class AccountAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserService _userService;
        private readonly ILogger<AccountAdminService> _logger;

        public AccountAdminService(ApplicationDbContext context, UserService userService, ILogger<AccountAdminService> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        public async Task<AccountListViewModel> LayDanhSachTaiKhoanAsync(AccountFilterViewModel filter, int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _context.Users
                    .Include(u => u.Role)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    var searchTerm = filter.SearchTerm.ToLower();
                    query = query.Where(u => 
                        u.Username.ToLower().Contains(searchTerm) ||
                        u.Email.ToLower().Contains(searchTerm) ||
                        u.FirstName.ToLower().Contains(searchTerm) ||
                        u.LastName.ToLower().Contains(searchTerm) ||
                        (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm)));
                }

                if (!string.IsNullOrEmpty(filter.Role))
                {
                    query = query.Where(u => u.Role.RoleName.ToLower() == filter.Role.ToLower());
                }

                if (!string.IsNullOrEmpty(filter.Status))
                {
                    switch (filter.Status.ToLower())
                    {
                        case "active":
                            query = query.Where(u => u.IsActive && u.EmailConfirmed);
                            break;
                        case "inactive":
                            query = query.Where(u => !u.IsActive);
                            break;
                        case "pending":
                            query = query.Where(u => u.IsActive && !u.EmailConfirmed);
                            break;
                    }
                }

                if (filter.CreatedFrom.HasValue)
                {
                    query = query.Where(u => u.CreatedAt >= filter.CreatedFrom.Value);
                }

                if (filter.CreatedTo.HasValue)
                {
                    query = query.Where(u => u.CreatedAt <= filter.CreatedTo.Value.AddDays(1));
                }

                // Get total count
                var totalAccounts = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalAccounts / pageSize);

                // Apply pagination
                var accounts = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new AccountItemViewModel
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        Email = u.Email,
                        FullName = $"{u.FirstName} {u.LastName}",
                        PhoneNumber = u.PhoneNumber,
                        RoleName = u.Role.RoleName,
                        IsActive = u.IsActive,
                        EmailConfirmed = u.EmailConfirmed,
                        CreatedAt = u.CreatedAt,
                        LastLoginAt = u.LastLoginAt,
                        Avatar = u.Avatar,
                        Address = u.Address
                    })
                    .ToListAsync();

                var stats = await LayThongKeTaiKhoanAsync();

                return new AccountListViewModel
                {
                    Accounts = accounts,
                    Filter = filter,
                    Stats = stats,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalAccounts = totalAccounts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accounts list");
                return new AccountListViewModel
                {
                    Filter = filter,
                    Stats = new AccountStatsViewModel()
                };
            }
        }

        public async Task<AccountStatsViewModel> LayThongKeTaiKhoanAsync()
        {
            try
            {
                var totalAccounts = await _context.Users.CountAsync();
                var activeAccounts = await _context.Users.CountAsync(u => u.IsActive && u.EmailConfirmed);
                var inactiveAccounts = await _context.Users.CountAsync(u => !u.IsActive);
                var adminAccounts = await _context.Users
                    .Include(u => u.Role)
                    .CountAsync(u => u.Role.RoleName.ToLower() == "admin");
                var pendingAccounts = await _context.Users.CountAsync(u => u.IsActive && !u.EmailConfirmed);

                return new AccountStatsViewModel
                {
                    TotalAccounts = totalAccounts,
                    ActiveAccounts = activeAccounts,
                    InactiveAccounts = inactiveAccounts,
                    AdminAccounts = adminAccounts,
                    PendingAccounts = pendingAccounts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account stats");
                return new AccountStatsViewModel();
            }
        }

        public async Task<AccountItemViewModel?> LayTaiKhoanTheoIdAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null) return null;

                return new AccountItemViewModel
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = $"{user.FirstName} {user.LastName}",
                    PhoneNumber = user.PhoneNumber,
                    RoleName = user.Role.RoleName,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    Avatar = user.Avatar,
                    Address = user.Address
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account by ID: {UserId}", userId);
                return null;
            }
        }

        public async Task<List<RoleOptionViewModel>> LayDanhSachVaiTroAsync()
        {
            try
            {
                return await _context.Roles
                    .OrderBy(r => r.RoleName)
                    .Select(r => new RoleOptionViewModel
                    {
                        RoleId = r.RoleId,
                        RoleName = r.RoleName,
                        Description = r.Description ?? ""
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role options");
                return new List<RoleOptionViewModel>();
            }
        }

        public async Task<bool> TaoTaiKhoanAsync(CreateAccountViewModel model)
        {
            try
            {
                // Check if username or email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Email);

                if (existingUser != null)
                {
                    return false; // User already exists
                }

                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    RoleId = model.RoleId,
                    Address = model.Address,
                    IsActive = model.IsActive,
                    EmailConfirmed = model.EmailConfirmed,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // Hash password using UserService
                var createdUser = await _userService.TaoNguoiDungAsync(user, model.Password);
                
                return createdUser != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating account");
                return false;
            }
        }

        public async Task<bool> CapNhatTaiKhoanAsync(UpdateAccountViewModel model)
        {
            try
            {
                var user = await _context.Users.FindAsync(model.UserId);
                if (user == null) return false;

                // Check if username or email is taken by another user
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId != model.UserId && 
                                            (u.Username == model.Username || u.Email == model.Email));

                if (existingUser != null)
                {
                    return false; // Username or email already taken
                }

                // Update user properties
                user.Username = model.Username;
                user.Email = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.RoleId = model.RoleId;
                user.Address = model.Address;
                user.IsActive = model.IsActive;
                user.EmailConfirmed = model.EmailConfirmed;
                user.UpdatedAt = DateTime.Now;

                // Update password if provided
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    user.PasswordHash = _userService.MaHoaMatKhau(model.NewPassword);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account: {UserId}", model.UserId);
                return false;
            }
        }

        public async Task<bool> XoaTaiKhoanAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Bookings)
                    .Include(u => u.Orders)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null) return false;

                // Don't allow deleting admin users
                if (user.Role.RoleName.ToLower() == "admin")
                {
                    return false;
                }

                // Check if user has bookings or orders
                if (user.Bookings.Any() || user.Orders.Any())
                {
                    // Soft delete - just deactivate the account
                    user.IsActive = false;
                    user.UpdatedAt = DateTime.Now;
                }
                else
                {
                    // Hard delete - remove from database
                    _context.Users.Remove(user);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ChuyenDoiTrangThaiTaiKhoanAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null) return false;

                // Don't allow disabling admin users
                if (user.Role.RoleName.ToLower() == "admin" && user.IsActive)
                {
                    return false;
                }

                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling account status: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DatLaiMatKhauAsync(int userId, string newPassword)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                user.PasswordHash = _userService.MaHoaMatKhau(newPassword);
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password: {UserId}", userId);
                return false;
            }
        }

        public async Task<List<RoleOptionViewModel>> LayTuyChonVaiTroAsync()
        {
            return await _context.Roles
                .Select(r => new RoleOptionViewModel
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName,
                    Description = r.Description
                })
                .OrderBy(r => r.RoleName)
                .ToListAsync();
        }
    }
}
