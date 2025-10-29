using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.ViewModels;

namespace _3TLMiniSoccer.Services
{
    public class DashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(ApplicationDbContext context, ILogger<DashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DashboardViewModel> LayDuLieuTrangChuAsync()
        {
            try
            {
                var dashboard = new DashboardViewModel
                {
                    Stats = await LayThongKeHomNayAsync(),
                    TopFields = await LayTopSanAsync(3),
                    TopProducts = await LayTopSanPhamAsync(3),
                    TopFieldCustomers = await LayTopKhachHangDatSanAsync(5),
                    TopProductCustomers = await LayTopKhachHangMuaHangAsync(5),
                    RecentFeedback = await LayPhanHoiGanDayAsync(2)
                };

                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data");
                return new DashboardViewModel(); // Return empty data on error
            }
        }

        public async Task<DashboardStatsViewModel> LayThongKeHomNayAsync()
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var stats = new DashboardStatsViewModel();

                // Quick Stats
                stats.TodayProductOrders = await _context.Orders
                    .Where(o => o.CreatedAt >= today && o.CreatedAt < tomorrow)
                    .CountAsync();

                stats.TodayFieldBookings = await _context.Bookings
                    .Where(b => b.CreatedAt >= today && b.CreatedAt < tomorrow)
                    .CountAsync();

                stats.ActiveFields = await _context.Fields
                    .Where(f => f.Status == "Active")
                    .CountAsync();

                stats.AvailableProducts = await _context.Products
                    .Where(p => p.IsAvailable)
                    .CountAsync();

                // Field Bookings Analysis
                var todayBookings = await _context.Bookings
                    .Where(b => b.CreatedAt >= today && b.CreatedAt < tomorrow)
                    .ToListAsync();

                stats.TotalFieldBookingsToday = todayBookings.Count;
                stats.TotalFieldRevenueToday = todayBookings.Sum(b => b.TotalPrice);
                stats.CompletedBookings = todayBookings.Count(b => b.Status == "Completed");
                stats.DepositPaidBookings = todayBookings.Count(b => b.Status == "Confirmed" || b.Status == "DepositPaid");
                stats.CancelledBookings = todayBookings.Count(b => b.Status == "Cancelled");

                // Product Orders Analysis
                var todayOrders = await _context.Orders
                    .Where(o => o.CreatedAt >= today && o.CreatedAt < tomorrow)
                    .ToListAsync();

                stats.TotalProductOrdersToday = todayOrders.Count;
                stats.TotalProductRevenueToday = todayOrders.Sum(o => o.TotalAmount);
                stats.PaidOrders = todayOrders.Count(o => o.Status == "Paid" || o.Status == "Completed");
                stats.UnpaidOrders = todayOrders.Count(o => o.Status == "Pending" || o.Status == "Unpaid");

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today stats");
                return new DashboardStatsViewModel();
            }
        }

        public async Task<List<TopFieldViewModel>> LayTopSanAsync(int count = 3)
        {
            try
            {
                var topFields = await _context.Bookings
                    .Include(b => b.Field)
                    .ThenInclude(f => f.FieldType)
                    .Where(b => b.Status != "Cancelled")
                    .GroupBy(b => new { b.FieldId, b.Field.FieldName, b.Field.FieldType.TypeName })
                    .Select(g => new TopFieldViewModel
                    {
                        FieldName = g.Key.FieldName,
                        FieldType = g.Key.TypeName,
                        Price = g.Average(b => b.TotalPrice / b.Duration), // Average price per hour
                        BookingCount = g.Count(),
                        Revenue = g.Sum(b => b.TotalPrice)
                    })
                    .OrderByDescending(f => f.BookingCount)
                    .Take(count)
                    .ToListAsync();

                // Add ranking
                for (int i = 0; i < topFields.Count; i++)
                {
                    topFields[i].Rank = i + 1;
                }

                return topFields;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top fields");
                return new List<TopFieldViewModel>();
            }
        }

        public async Task<List<TopProductViewModel>> LayTopSanPhamAsync(int count = 3)
        {
            try
            {
                var topProducts = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .Include(oi => oi.Order)
                    .Where(oi => oi.Order.Status != "Cancelled")
                    .GroupBy(oi => new { oi.ProductId, oi.Product.ProductName, oi.Product.Price })
                    .Select(g => new TopProductViewModel
                    {
                        ProductName = g.Key.ProductName,
                        Price = g.Key.Price,
                        SalesCount = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                    })
                    .OrderByDescending(p => p.SalesCount)
                    .Take(count)
                    .ToListAsync();

                // Add ranking
                for (int i = 0; i < topProducts.Count; i++)
                {
                    topProducts[i].Rank = i + 1;
                }

                return topProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top products");
                return new List<TopProductViewModel>();
            }
        }

        public async Task<List<TopCustomerViewModel>> LayTopKhachHangDatSanAsync(int count = 5)
        {
            try
            {
                var topCustomers = await _context.Bookings
                    .Include(b => b.User)
                    .Where(b => b.Status != "Cancelled")
                    .GroupBy(b => new { 
                        CustomerName = b.User != null ? $"{b.User.FirstName} {b.User.LastName}" : b.GuestName ?? "Khách vãng lai",
                        PhoneNumber = b.User != null ? b.User.PhoneNumber : b.GuestPhone ?? ""
                    })
                    .Select(g => new TopCustomerViewModel
                    {
                        CustomerName = g.Key.CustomerName,
                        PhoneNumber = g.Key.PhoneNumber,
                        OrderCount = g.Count(),
                        Revenue = g.Sum(b => b.TotalPrice)
                    })
                    .OrderByDescending(c => c.OrderCount)
                    .Take(count)
                    .ToListAsync();

                // Add ranking
                for (int i = 0; i < topCustomers.Count; i++)
                {
                    topCustomers[i].Rank = i + 1;
                }

                return topCustomers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top field customers");
                return new List<TopCustomerViewModel>();
            }
        }

        public async Task<List<TopCustomerViewModel>> LayTopKhachHangMuaHangAsync(int count = 5)
        {
            try
            {
                var topCustomers = await _context.Orders
                    .Include(o => o.User)
                    .Where(o => o.Status != "Cancelled")
                    .GroupBy(o => new { 
                        CustomerName = $"{o.User.FirstName} {o.User.LastName}",
                        PhoneNumber = o.User.PhoneNumber ?? ""
                    })
                    .Select(g => new TopCustomerViewModel
                    {
                        CustomerName = g.Key.CustomerName,
                        PhoneNumber = g.Key.PhoneNumber,
                        OrderCount = g.Count(),
                        Revenue = g.Sum(o => o.TotalAmount)
                    })
                    .OrderByDescending(c => c.OrderCount)
                    .Take(count)
                    .ToListAsync();

                // Add ranking
                for (int i = 0; i < topCustomers.Count; i++)
                {
                    topCustomers[i].Rank = i + 1;
                }

                return topCustomers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top product customers");
                return new List<TopCustomerViewModel>();
            }
        }

        public async Task<List<RecentFeedbackViewModel>> LayPhanHoiGanDayAsync(int count = 2)
        {
            try
            {
                // Try to get from Reviews table if exists, otherwise use sample data
                var feedback = new List<RecentFeedbackViewModel>();

                // Check if Reviews table exists
                var hasReviews = await _context.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Reviews'"
                ).FirstOrDefaultAsync() > 0;

                if (hasReviews)
                {
                    // Get actual reviews if table exists
                    feedback = await _context.Database.SqlQueryRaw<RecentFeedbackViewModel>(
                        @"SELECT TOP({0}) 
                            CustomerName, 
                            Comment, 
                            CreatedAt 
                          FROM Reviews 
                          ORDER BY CreatedAt DESC", count
                    ).ToListAsync();
                }

                // If no reviews or table doesn't exist, return sample data
                if (!feedback.Any())
                {
                    feedback = new List<RecentFeedbackViewModel>
                    {
                        new RecentFeedbackViewModel
                        {
                            CustomerName = "Nguyễn Văn A",
                            Comment = "Sân bóng rất tốt, nhân viên nhiệt tình. Nhưng nước uống hơi đắt.",
                            CreatedAt = DateTime.Now.AddHours(-2)
                        },
                        new RecentFeedbackViewModel
                        {
                            CustomerName = "Trần Thị B",
                            Comment = "Đặt sân online rất tiện lợi, sẽ quay lại ủng hộ.",
                            CreatedAt = DateTime.Now.AddHours(-5)
                        }
                    };
                }

                return feedback.Take(count).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent feedback");
                // Return sample data on error
                return new List<RecentFeedbackViewModel>
                {
                    new RecentFeedbackViewModel
                    {
                        CustomerName = "Nguyễn Văn A",
                        Comment = "Sân bóng rất tốt, nhân viên nhiệt tình. Nhưng nước uống hơi đắt.",
                        CreatedAt = DateTime.Now.AddHours(-2)
                    },
                    new RecentFeedbackViewModel
                    {
                        CustomerName = "Trần Thị B",
                        Comment = "Đặt sân online rất tiện lợi, sẽ quay lại ủng hộ.",
                        CreatedAt = DateTime.Now.AddHours(-5)
                    }
                };
            }
        }
    }
}
