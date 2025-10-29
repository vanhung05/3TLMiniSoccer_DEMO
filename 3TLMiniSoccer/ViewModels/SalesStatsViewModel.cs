using _3TLMiniSoccer.Services;

namespace _3TLMiniSoccer.ViewModels
{
    public class SalesStatsViewModel
    {
        public SalesStatisticsSummary Summary { get; set; } = new();
        public List<SalesChartDataViewModel> SalesRevenueChartData { get; set; } = new();
        public List<ProductSalesChartDataViewModel> ProductSalesChartData { get; set; } = new();
        public List<TopSellingProductViewModel> TopSellingProducts { get; set; } = new();
        public List<TopCustomerViewModel> TopCustomers { get; set; } = new();
        public List<CategoryAnalysisViewModel> CategoryAnalysis { get; set; } = new();
        public SalesComparisonViewModel Comparison { get; set; } = new();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string ReportType { get; set; } = "daily";
    }

    public class SalesStatsFilterRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? ReportType { get; set; }
    }

    public class SalesStatisticsSummary
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalProductsSold { get; set; }
        public int TodayOrders { get; set; }
        public decimal TodayRevenue { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal GrowthRate { get; set; }
    }

    public class SalesChartDataViewModel
    {
        public string Label { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public DateTime Date { get; set; }
    }

    public class ProductSalesChartDataViewModel
    {
        public string Label { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public DateTime Date { get; set; }
    }

    public class TopSellingProductViewModel
    {
        public int Rank { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImage { get; set; }
        public string? Category { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class CategoryAnalysisViewModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Percentage { get; set; }
        public string? Description { get; set; }
    }

    public class SalesComparisonViewModel
    {
        public decimal CurrentPeriodRevenue { get; set; }
        public decimal PreviousPeriodRevenue { get; set; }
        public decimal RevenueChange { get; set; }
        public decimal RevenueChangePercentage { get; set; }
        public int CurrentPeriodOrders { get; set; }
        public int PreviousPeriodOrders { get; set; }
        public int OrderChange { get; set; }
        public decimal OrderChangePercentage { get; set; }
        public decimal OrderGrowthPercentage { get; set; }
        public decimal RevenueGrowthPercentage { get; set; }
        public decimal ProductGrowthPercentage { get; set; }
        public decimal RevenueGrowth { get; set; }
        public decimal OrderGrowth { get; set; }
        public int CurrentPeriodProducts { get; set; }
        public int PreviousPeriodProducts { get; set; }
        public decimal ProductGrowth { get; set; }
    }
}
