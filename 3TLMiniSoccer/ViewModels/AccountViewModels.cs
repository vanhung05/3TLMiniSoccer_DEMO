using System.ComponentModel.DataAnnotations;

namespace _3TLMiniSoccer.ViewModels
{
    // ViewModel cho danh sách tài khoản với pagination
    public class AccountListViewModel
    {
        public List<AccountItemViewModel> Accounts { get; set; } = new();
        public AccountFilterViewModel Filter { get; set; } = new();
        public AccountStatsViewModel Stats { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalAccounts { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    // ViewModel cho từng item trong danh sách
    public class AccountItemViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? Avatar { get; set; }
        public string? Address { get; set; }

        // Computed properties
        public string StatusText => IsActive ? (EmailConfirmed ? "Hoạt động" : "Chờ xác thực") : "Bị khóa";
        public string StatusClass => IsActive ? (EmailConfirmed ? "bg-green-100 text-green-800" : "bg-yellow-100 text-yellow-800") : "bg-red-100 text-red-800";
        public string RoleClass => RoleName.ToLower() switch
        {
            "admin" => "bg-purple-100 text-purple-800",
            "staff" => "bg-green-100 text-green-800", 
            "user" => "bg-blue-100 text-blue-800",
            _ => "bg-gray-100 text-gray-800"
        };
        public string FormattedCreatedAt => CreatedAt.ToString("dd/MM/yyyy");
        public string FormattedLastLogin => LastLoginAt?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa đăng nhập";
        public string DisplayAvatar => !string.IsNullOrEmpty(Avatar) ? Avatar : "/images/default-avatar.png";
        public bool CanDelete => RoleName.ToLower() != "admin"; // Không cho xóa admin
    }

    // ViewModel cho bộ lọc
    public class AccountFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }

    // ViewModel cho thống kê
    public class AccountStatsViewModel
    {
        public int TotalAccounts { get; set; }
        public int ActiveAccounts { get; set; }
        public int InactiveAccounts { get; set; }
        public int AdminAccounts { get; set; }
        public int PendingAccounts { get; set; }

        // Computed percentages
        public double ActivePercentage => TotalAccounts > 0 ? Math.Round((double)ActiveAccounts / TotalAccounts * 100, 1) : 0;
        public double InactivePercentage => TotalAccounts > 0 ? Math.Round((double)InactiveAccounts / TotalAccounts * 100, 1) : 0;
        public double AdminPercentage => TotalAccounts > 0 ? Math.Round((double)AdminAccounts / TotalAccounts * 100, 1) : 0;
        public double PendingPercentage => TotalAccounts > 0 ? Math.Round((double)PendingAccounts / TotalAccounts * 100, 1) : 0;
    }

    // ViewModel cho tạo tài khoản mới
    public class CreateAccountViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên đăng nhập không được vượt quá 50 ký tự")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ là bắt buộc")]
        [StringLength(50, ErrorMessage = "Họ không được vượt quá 50 ký tự")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên không được vượt quá 50 ký tự")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        public int RoleId { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;
        public bool EmailConfirmed { get; set; } = false;
    }

    // ViewModel cho cập nhật tài khoản
    public class UpdateAccountViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên đăng nhập không được vượt quá 50 ký tự")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ là bắt buộc")]
        [StringLength(50, ErrorMessage = "Họ không được vượt quá 50 ký tự")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên không được vượt quá 50 ký tự")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        public int RoleId { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
        public string? Address { get; set; }

        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }

        // Optional password change
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string? NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string? ConfirmNewPassword { get; set; }
    }

    // ViewModel cho dropdown roles
    public class RoleOptionViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
