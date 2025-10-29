using _3TLMiniSoccer.Models;

namespace _3TLMiniSoccer.ViewModels
{
    public class VoucherAdminViewModel
    {
        public List<DiscountCode> Vouchers { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public string? Search { get; set; }
        public string? DiscountType { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public VoucherStatistics Statistics { get; set; } = new();
    }

    public class VoucherStatistics
    {
        public int TotalVouchers { get; set; }
        public int ActiveVouchers { get; set; }
        public int ExpiringSoonVouchers { get; set; }
        public int ExpiredVouchers { get; set; }
    }
}
