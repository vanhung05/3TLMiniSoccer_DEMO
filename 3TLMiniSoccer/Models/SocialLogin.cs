using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _3TLMiniSoccer.Models
{
    public class SocialLogin
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Provider { get; set; } = string.Empty; // "Google", "Facebook"

        [Required]
        [StringLength(255)]
        public string ProviderKey { get; set; } = string.Empty; // OAuth provider user ID

        [StringLength(100)]
        public string? ProviderDisplayName { get; set; }

        [StringLength(500)]
        public string? ProfilePictureUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}