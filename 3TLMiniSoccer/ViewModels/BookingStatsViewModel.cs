using _3TLMiniSoccer.Services;

namespace _3TLMiniSoccer.ViewModels
{
    public class BookingStatsViewModel
    {
        public BookingStatisticsSummaryViewModel Summary { get; set; } = new();
        public List<BookingChartDataViewModel> BookingChartData { get; set; } = new();
        public List<RevenueChartDataViewModel> RevenueChartData { get; set; } = new();
        public List<FieldPerformanceViewModel> FieldPerformance { get; set; } = new();
        public List<TopCustomerViewModel> TopCustomers { get; set; } = new();
        public List<TopFieldViewModel> TopFields { get; set; } = new();
        public TimeAnalysisViewModel TimeAnalysis { get; set; } = new();
        public ComparisonViewModel Comparison { get; set; } = new();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string ReportType { get; set; } = "daily";
    }

    public class BookingStatsFilterRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? ReportType { get; set; }
    }

    public class BookingStatisticsSummaryViewModel
    {
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public int NewCustomers { get; set; }
        public decimal GrowthRate { get; set; }
        public int TodayBookings { get; set; }
        public decimal TodayRevenue { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int CompletedBookings { get; set; }
    }

    public class BookingChartDataViewModel
    {
        public string Label { get; set; } = string.Empty;
        public int BookingCount { get; set; }
        public DateTime Date { get; set; }
    }

    public class RevenueChartDataViewModel
    {
        public string Label { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public DateTime Date { get; set; }
    }

    public class FieldPerformanceViewModel
    {
        public int FieldId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string? FieldType { get; set; }
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal UtilizationRate { get; set; }
    }

    public class TimeAnalysisViewModel
    {
        public Dictionary<string, int> HourlyDistribution { get; set; } = new();
        public Dictionary<string, int> DayOfWeekDistribution { get; set; } = new();
        public Dictionary<string, int> MonthlyDistribution { get; set; } = new();
        public string PeakHour { get; set; } = string.Empty;
        public string PeakDay { get; set; } = string.Empty;
        public decimal MorningPercentage { get; set; }
        public decimal AfternoonPercentage { get; set; }
        public decimal EveningPercentage { get; set; }
        public int MorningBookings { get; set; }
        public int AfternoonBookings { get; set; }
        public int EveningBookings { get; set; }
        public string MostPopularTimeSlot { get; set; } = string.Empty;
    }

    public class ComparisonViewModel
    {
        public decimal CurrentPeriodRevenue { get; set; }
        public decimal PreviousPeriodRevenue { get; set; }
        public decimal RevenueChange { get; set; }
        public decimal RevenueChangePercentage { get; set; }
        public int CurrentPeriodBookings { get; set; }
        public int PreviousPeriodBookings { get; set; }
        public int BookingChange { get; set; }
        public decimal BookingChangePercentage { get; set; }
        public decimal BookingGrowthRate { get; set; }
        public decimal RevenueGrowthRate { get; set; }
        public decimal CustomerGrowthRate { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;
    }
}
