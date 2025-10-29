using _3TLMiniSoccer.Models;

namespace _3TLMiniSoccer.ViewModels
{
    public class VoucherValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal? DiscountAmount { get; set; }
        public string? VoucherCode { get; set; }
        public int? VoucherId { get; set; }
        public string? DiscountType { get; set; }
        public DiscountCode? Voucher { get; set; }
    }
}

