using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _3TLMiniSoccer.Models
{
    public class SessionOrderItem
    {
        [Key]
        public int SessionOrderItemId { get; set; }

        [Required]
        public int SessionOrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        // Navigation properties
        public virtual SessionOrder SessionOrder { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
