using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using _3TLMiniSoccer.ViewModels;

namespace _3TLMiniSoccer.Services
{
    public class SalesStatisticsService
    {
        private readonly ApplicationDbContext _context;

        public SalesStatisticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SalesStatisticsSummary> LayThongKeBanHangTongQuanAsync(DateTime fromDate, DateTime toDate)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Tổng đơn hàng trong khoảng thời gian
            var totalOrders = await _context.Orders
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate)
                .CountAsync();

            // Đơn hàng hôm nay
            var todayOrders = await _context.Orders
                .Where(o => o.CreatedAt >= today && o.CreatedAt < tomorrow)
                .CountAsync();

            // Tổng doanh thu (chỉ tính đơn hàng đã giao)
            var totalRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && o.Status == "Delivered")
                .SumAsync(o => o.TotalAmount);

            // Doanh thu hôm nay
            var todayRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= today && o.CreatedAt < tomorrow && o.Status == "Delivered")
                .SumAsync(o => o.TotalAmount);

            // Tổng sản phẩm bán
            var totalProductsSold = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.CreatedAt >= fromDate && oi.Order.CreatedAt <= toDate && oi.Order.Status == "Delivered")
                .SumAsync(oi => oi.Quantity);

            // Thống kê trạng thái đơn hàng
            var pendingOrders = await _context.Orders
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && o.Status == "Pending")
                .CountAsync();

            var processingOrders = await _context.Orders
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && o.Status == "Processing")
                .CountAsync();

            var shippedOrders = await _context.Orders
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && o.Status == "Shipped")
                .CountAsync();

            var deliveredOrders = await _context.Orders
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && o.Status == "Delivered")
                .CountAsync();

            var cancelledOrders = await _context.Orders
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && o.Status == "Cancelled")
                .CountAsync();

            // Tính tỷ lệ tăng trưởng so với kỳ trước
            var previousPeriodStart = fromDate.AddDays(-(toDate - fromDate).Days - 1);
            var previousPeriodEnd = fromDate.AddDays(-1);

            var previousRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= previousPeriodStart && o.CreatedAt <= previousPeriodEnd && o.Status == "Delivered")
                .SumAsync(o => o.TotalAmount);

            var growthRate = previousRevenue > 0 ? ((totalRevenue - previousRevenue) / previousRevenue) * 100 : 0;

            return new SalesStatisticsSummary
            {
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                TotalProductsSold = totalProductsSold,
                GrowthRate = growthRate,
                TodayOrders = todayOrders,
                TodayRevenue = todayRevenue,
                PendingOrders = pendingOrders,
                ProcessingOrders = processingOrders,
                ShippedOrders = shippedOrders,
                DeliveredOrders = deliveredOrders,
                CancelledOrders = cancelledOrders
            };
        }

        public async Task<List<SalesChartDataViewModel>> LayDuLieuBieuDoDoanhThuBanHangAsync(DateTime fromDate, DateTime toDate, string reportType)
        {
            var data = new List<SalesChartDataViewModel>();

            switch (reportType.ToLower())
            {
                case "daily":
                    var dailyData = await _context.Orders
                        .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && o.Status == "Delivered")
                        .GroupBy(o => o.CreatedAt.Date)
                        .Select(g => new SalesChartDataViewModel
                        {
                            Date = g.Key,
                            Label = g.Key.ToString("dd/MM"),
                            Revenue = g.Sum(o => o.TotalAmount)
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync();
                    data = dailyData;
                    break;

                case "weekly":
                    var weeklyData = await _context.Orders
                        .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && o.Status == "Delivered")
                        .GroupBy(o => new { Year = o.CreatedAt.Year, Week = GetWeekOfYear(o.CreatedAt) })
                        .Select(g => new SalesChartDataViewModel
                        {
                            Date = g.Min(o => o.CreatedAt),
                            Label = $"Tuần {g.Key.Week}",
                            Revenue = g.Sum(o => o.TotalAmount)
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync();
                    data = weeklyData;
                    break;

                case "monthly":
                    var monthlyData = await _context.Orders
                        .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && o.Status == "Delivered")
                        .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                        .Select(g => new SalesChartDataViewModel
                        {
                            Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                            Label = $"{g.Key.Month}/{g.Key.Year}",
                            Revenue = g.Sum(o => o.TotalAmount)
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync();
                    data = monthlyData;
                    break;

                case "yearly":
                    var yearlyData = await _context.Orders
                        .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && o.Status == "Delivered")
                        .GroupBy(o => o.CreatedAt.Year)
                        .Select(g => new SalesChartDataViewModel
                        {
                            Date = new DateTime(g.Key, 1, 1),
                            Label = g.Key.ToString(),
                            Revenue = g.Sum(o => o.TotalAmount)
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync();
                    data = yearlyData;
                    break;
            }

            return data;
        }

        public async Task<List<ProductSalesChartDataViewModel>> LayDuLieuBieuDoBanSanPhamAsync(DateTime fromDate, DateTime toDate, string reportType)
        {
            var data = new List<ProductSalesChartDataViewModel>();

            switch (reportType.ToLower())
            {
                case "daily":
                    var dailyData = await _context.OrderItems
                        .Include(oi => oi.Order)
                        .Where(oi => oi.Order.CreatedAt >= fromDate && oi.Order.CreatedAt <= toDate && oi.Order.Status == "Delivered")
                        .GroupBy(oi => oi.Order.CreatedAt.Date)
                        .Select(g => new ProductSalesChartDataViewModel
                        {
                            Date = g.Key,
                            Label = g.Key.ToString("dd/MM"),
                            QuantitySold = g.Sum(oi => oi.Quantity)
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync();
                    data = dailyData;
                    break;

                case "weekly":
                    var weeklyData = await _context.OrderItems
                        .Include(oi => oi.Order)
                        .Where(oi => oi.Order.CreatedAt >= fromDate && oi.Order.CreatedAt <= toDate && oi.Order.Status == "Delivered")
                        .GroupBy(oi => new { Year = oi.Order.CreatedAt.Year, Week = GetWeekOfYear(oi.Order.CreatedAt) })
                        .Select(g => new ProductSalesChartDataViewModel
                        {
                            Date = g.Min(oi => oi.Order.CreatedAt),
                            Label = $"Tuần {g.Key.Week}",
                            QuantitySold = g.Sum(oi => oi.Quantity)
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync();
                    data = weeklyData;
                    break;

                case "monthly":
                    var monthlyData = await _context.OrderItems
                        .Include(oi => oi.Order)
                        .Where(oi => oi.Order.CreatedAt >= fromDate && oi.Order.CreatedAt <= toDate && oi.Order.Status == "Delivered")
                        .GroupBy(oi => new { oi.Order.CreatedAt.Year, oi.Order.CreatedAt.Month })
                        .Select(g => new ProductSalesChartDataViewModel
                        {
                            Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                            Label = $"{g.Key.Month}/{g.Key.Year}",
                            QuantitySold = g.Sum(oi => oi.Quantity)
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync();
                    data = monthlyData;
                    break;

                case "yearly":
                    var yearlyData = await _context.OrderItems
                        .Include(oi => oi.Order)
                        .Where(oi => oi.Order.CreatedAt >= fromDate && oi.Order.CreatedAt <= toDate && oi.Order.Status == "Delivered")
                        .GroupBy(oi => oi.Order.CreatedAt.Year)
                        .Select(g => new ProductSalesChartDataViewModel
                        {
                            Date = new DateTime(g.Key, 1, 1),
                            Label = g.Key.ToString(),
                            QuantitySold = g.Sum(oi => oi.Quantity)
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync();
                    data = yearlyData;
                    break;
            }

            return data;
        }

        public async Task<List<TopSellingProductViewModel>> LayTopSanPhamBanChayAsync(DateTime fromDate, DateTime toDate, int topCount)
        {
            var topProducts = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.CreatedAt >= fromDate && oi.Order.CreatedAt <= toDate && oi.Order.Status == "Delivered")
                .GroupBy(oi => new { oi.ProductId, oi.Product.ProductName, oi.Product.ImageUrl, oi.Product.ProductTypeId })
                .Select(g => new TopSellingProductViewModel
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    ProductImage = g.Key.ImageUrl,
                    Category = g.Key.ProductTypeId != null ? 
                        _context.ProductTypes.FirstOrDefault(pt => pt.ProductTypeId == g.Key.ProductTypeId).TypeName : null,
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(topCount)
                .ToListAsync();

            // Thêm rank
            for (int i = 0; i < topProducts.Count; i++)
            {
                topProducts[i].Rank = i + 1;
            }

            return topProducts;
        }

        public async Task<List<TopCustomerViewModel>> LayTopKhachHangBanHangAsync(DateTime fromDate, DateTime toDate, int topCount)
        {
            var topCustomers = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && o.Status == "Delivered")
                .GroupBy(o => new { o.UserId, o.User.Username, o.User.Email })
                .Select(g => new TopCustomerViewModel
                {
                    CustomerId = g.Key.UserId.ToString(),
                    CustomerName = g.Key.Username ?? "Khách hàng",
                    CustomerEmail = g.Key.Email,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(topCount)
                .ToListAsync();

            // Thêm rank
            for (int i = 0; i < topCustomers.Count; i++)
            {
                topCustomers[i].Rank = i + 1;
            }

            return topCustomers;
        }

        public async Task<List<CategoryAnalysisViewModel>> LayPhanTichDanhMucAsync(DateTime fromDate, DateTime toDate)
        {
            var categoryData = await _context.OrderItems
                .Include(oi => oi.Product)
                .ThenInclude(p => p.ProductType)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.CreatedAt >= fromDate && oi.Order.CreatedAt <= toDate && oi.Order.Status == "Delivered")
                .GroupBy(oi => oi.Product.ProductType != null ? oi.Product.ProductType.TypeName : "Khác")
                .Select(g => new CategoryAnalysisViewModel
                {
                    CategoryName = g.Key,
                    Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice),
                    QuantitySold = g.Sum(oi => oi.Quantity)
                })
                .ToListAsync();

            var totalRevenue = categoryData.Sum(c => c.Revenue);

            foreach (var category in categoryData)
            {
                category.Percentage = totalRevenue > 0 ? (category.Revenue / totalRevenue) * 100 : 0;
                
                // Thêm mô tả dựa trên phần trăm
                if (category.Percentage >= 30)
                    category.Description = "Danh mục bán chạy nhất";
                else if (category.Percentage >= 20)
                    category.Description = "Danh mục ổn định";
                else if (category.Percentage >= 10)
                    category.Description = "Có tiềm năng phát triển";
                else
                    category.Description = "Các sản phẩm khác";
            }

            return categoryData.OrderByDescending(c => c.Percentage).ToList();
        }

        public async Task<SalesComparisonViewModel> LaySoSanhBanHangVoiKyTruocAsync(DateTime fromDate, DateTime toDate, string reportType)
        {
            var periodDays = (toDate - fromDate).Days + 1;
            var previousPeriodStart = fromDate.AddDays(-periodDays);
            var previousPeriodEnd = fromDate.AddDays(-1);

            // Kỳ hiện tại
            var currentRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate && o.Status == "Delivered")
                .SumAsync(o => o.TotalAmount);

            var currentOrders = await _context.Orders
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate)
                .CountAsync();

            var currentProducts = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.CreatedAt >= fromDate && oi.Order.CreatedAt <= toDate && oi.Order.Status == "Delivered")
                .SumAsync(oi => oi.Quantity);

            // Kỳ trước
            var previousRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= previousPeriodStart && o.CreatedAt <= previousPeriodEnd && o.Status == "Delivered")
                .SumAsync(o => o.TotalAmount);

            var previousOrders = await _context.Orders
                .Where(o => o.CreatedAt >= previousPeriodStart && o.CreatedAt <= previousPeriodEnd)
                .CountAsync();

            var previousProducts = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.CreatedAt >= previousPeriodStart && oi.Order.CreatedAt <= previousPeriodEnd && oi.Order.Status == "Delivered")
                .SumAsync(oi => oi.Quantity);

            // Tính tăng trưởng
            var revenueGrowth = currentRevenue - previousRevenue;
            var revenueGrowthPercentage = previousRevenue > 0 ? (revenueGrowth / previousRevenue) * 100 : 0;

            var orderGrowth = currentOrders - previousOrders;
            var orderGrowthPercentage = previousOrders > 0 ? (orderGrowth / (decimal)previousOrders) * 100 : 0;

            var productGrowth = currentProducts - previousProducts;
            var productGrowthPercentage = previousProducts > 0 ? (productGrowth / (decimal)previousProducts) * 100 : 0;

            return new SalesComparisonViewModel
            {
                CurrentPeriodRevenue = currentRevenue,
                PreviousPeriodRevenue = previousRevenue,
                RevenueGrowth = revenueGrowth,
                RevenueGrowthPercentage = revenueGrowthPercentage,
                CurrentPeriodOrders = currentOrders,
                PreviousPeriodOrders = previousOrders,
                OrderGrowth = orderGrowth,
                OrderGrowthPercentage = orderGrowthPercentage,
                CurrentPeriodProducts = currentProducts,
                PreviousPeriodProducts = previousProducts,
                ProductGrowth = productGrowth,
                ProductGrowthPercentage = productGrowthPercentage
            };
        }

        private static int GetWeekOfYear(DateTime date)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            var calendar = culture.Calendar;
            return calendar.GetWeekOfYear(date, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
        }

        // Additional methods for AdminController
        public async Task<SalesStatisticsSummary> LayThongKeTongQuanBanHangAsync(DateTime fromDate, DateTime toDate)
        {
            return await LayThongKeBanHangTongQuanAsync(fromDate, toDate);
        }

        public async Task<List<ProductSalesChartDataViewModel>> LayDuLieuBieuDoBanHangSanPhamAsync(DateTime fromDate, DateTime toDate, string reportType)
        {
            return await LayDuLieuBieuDoBanSanPhamAsync(fromDate, toDate, reportType);
        }

        public async Task<List<TopCustomerViewModel>> LayTopKhachHangAsync(DateTime fromDate, DateTime toDate, int topCount)
        {
            return await LayTopKhachHangBanHangAsync(fromDate, toDate, topCount);
        }

        public async Task<SalesComparisonViewModel> LaySoSanhVoiKyTruocAsync(DateTime fromDate, DateTime toDate, string reportType)
        {
            return await LaySoSanhBanHangVoiKyTruocAsync(fromDate, toDate, reportType);
        }
    }
}
