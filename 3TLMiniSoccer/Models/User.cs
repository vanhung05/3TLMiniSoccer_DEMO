using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _3TLMiniSoccer.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(255)]
        public string? Avatar { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [Required]
        public int RoleId { get; set; } = 3; // Mặc định là User

        public bool IsActive { get; set; } = true;

        public bool EmailConfirmed { get; set; } = false;

        public bool PhoneConfirmed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public DateTime? LastLoginAt { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public int? AssignedBy { get; set; }

        // Navigation properties
        public virtual Role Role { get; set; } = null!;
        public virtual User? AssignedByUser { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<LoyaltyPoint> LoyaltyPoints { get; set; } = new List<LoyaltyPoint>();
        public virtual ICollection<SocialLogin> SocialLogins { get; set; } = new List<SocialLogin>();
        public virtual ICollection<User> AssignedUsers { get; set; } = new List<User>();
    }
}
