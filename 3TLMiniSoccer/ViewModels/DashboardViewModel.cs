namespace _3TLMiniSoccer.ViewModels
{
    public class DashboardViewModel
    {
        public DashboardStatsViewModel Stats { get; set; } = new();
        public List<TopFieldViewModel> TopFields { get; set; } = new();
        public List<TopProductViewModel> TopProducts { get; set; } = new();
        public List<TopCustomerViewModel> TopFieldCustomers { get; set; } = new();
        public List<TopCustomerViewModel> TopProductCustomers { get; set; } = new();
        public List<RecentFeedbackViewModel> RecentFeedback { get; set; } = new();
    }

    public class DashboardStatsViewModel
    {
        // Quick Stats
        public int TodayProductOrders { get; set; }
        public int TodayFieldBookings { get; set; }
        public int ActiveFields { get; set; }
        public int AvailableProducts { get; set; }

        // Field Bookings Analysis
        public int TotalFieldBookingsToday { get; set; }
        public decimal TotalFieldRevenueToday { get; set; }
        public int CompletedBookings { get; set; }
        public int DepositPaidBookings { get; set; }
        public int CancelledBookings { get; set; }
        public double CompletedPercentage => TotalFieldBookingsToday > 0 ? (double)CompletedBookings / TotalFieldBookingsToday * 100 : 0;
        public double DepositPaidPercentage => TotalFieldBookingsToday > 0 ? (double)DepositPaidBookings / TotalFieldBookingsToday * 100 : 0;
        public double CancelledPercentage => TotalFieldBookingsToday > 0 ? (double)CancelledBookings / TotalFieldBookingsToday * 100 : 0;

        // Product Orders Analysis
        public int TotalProductOrdersToday { get; set; }
        public decimal TotalProductRevenueToday { get; set; }
        public int PaidOrders { get; set; }
        public int UnpaidOrders { get; set; }
        public double PaidPercentage => TotalProductOrdersToday > 0 ? (double)PaidOrders / TotalProductOrdersToday * 100 : 0;
        public double UnpaidPercentage => TotalProductOrdersToday > 0 ? (double)UnpaidOrders / TotalProductOrdersToday * 100 : 0;
    }

    public class TopFieldViewModel
    {
        public int Rank { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopProductViewModel
    {
        public int Rank { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int SalesCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopCustomerViewModel
    {
        public int Rank { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class RecentFeedbackViewModel
    {
        public string CustomerName { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string TimeAgo => GetTimeAgo();

        private string GetTimeAgo()
        {
            var timeSpan = DateTime.Now - CreatedAt;
            
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} phút trước";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} giờ trước";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} ngày trước";
            
            return CreatedAt.ToString("dd/MM/yyyy");
        }
    }
}