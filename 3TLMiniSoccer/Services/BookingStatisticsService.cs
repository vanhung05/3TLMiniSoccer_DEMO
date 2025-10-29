using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using _3TLMiniSoccer.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace _3TLMiniSoccer.Services
{
    public class BookingStatisticsService
    {
        private readonly ApplicationDbContext _context;

        public BookingStatisticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BookingStatisticsSummaryViewModel> LayThongKeTongQuanDatSanAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Bookings.AsQueryable();

            // Áp dụng filter ngày nếu có
            if (fromDate.HasValue)
                query = query.Where(b => b.BookingDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(b => b.BookingDate <= toDate.Value);

            var totalBookings = await query.CountAsync();
                        var totalRevenue = await query.SumAsync(b => b.TotalPrice);

            var newCustomers = await query
                .Where(b => b.BookingDate >= DateTime.Today.AddDays(-30))
                .Select(b => b.UserId)
                .Distinct()
                .CountAsync();

            // Thống kê hôm nay
            var todayBookings = await _context.Bookings
                .Where(b => b.BookingDate.Date == DateTime.Today)
                .CountAsync();
            var todayRevenue = await _context.Bookings
                .Where(b => b.BookingDate.Date == DateTime.Today)
                .SumAsync(b => b.TotalPrice);

            // Thống kê theo trạng thái
            var pendingBookings = await query.Where(b => b.Status == "Pending").CountAsync();
            var confirmedBookings = await query.Where(b => b.Status == "Confirmed").CountAsync();
            var cancelledBookings = await query.Where(b => b.Status == "Cancelled").CountAsync();
            var completedBookings = await query.Where(b => b.Status == "Completed").CountAsync();

            // Tính tỷ lệ tăng trưởng so với tháng trước
            var previousMonthStart = DateTime.Today.AddMonths(-1).AddDays(-DateTime.Today.Day + 1);
            var previousMonthEnd = DateTime.Today.AddDays(-DateTime.Today.Day);
            var currentMonthStart = DateTime.Today.AddDays(-DateTime.Today.Day + 1);

            var previousMonthBookings = await _context.Bookings
                .Where(b => b.BookingDate >= previousMonthStart && b.BookingDate <= previousMonthEnd)
                .CountAsync();
            var currentMonthBookings = await _context.Bookings
                .Where(b => b.BookingDate >= currentMonthStart)
                .CountAsync();

            var growthRate = previousMonthBookings > 0 
                ? ((currentMonthBookings - previousMonthBookings) / (double)previousMonthBookings) * 100 
                : 0;

            return new BookingStatisticsSummaryViewModel
            {
                TotalBookings = totalBookings,
                TotalRevenue = totalRevenue,
                NewCustomers = newCustomers,
                GrowthRate = (decimal)growthRate,
                TodayBookings = todayBookings,
                TodayRevenue = todayRevenue,
                PendingBookings = pendingBookings,
                ConfirmedBookings = confirmedBookings,
                CancelledBookings = cancelledBookings,
                CompletedBookings = completedBookings
            };
        }

        public async Task<List<BookingChartDataViewModel>> LayDuLieuBieuDoDatSanAsync(DateTime fromDate, DateTime toDate, string reportType = "daily")
        {
            var query = _context.Bookings
                .Where(b => b.BookingDate >= fromDate && b.BookingDate <= toDate);

            List<BookingChartDataViewModel> result = new();

            switch (reportType.ToLower())
            {
                case "daily":
                    result = await query
                        .GroupBy(b => b.BookingDate.Date)
                        .Select(g => new BookingChartDataViewModel
                        {
                            Label = g.Key.ToString("dd/MM"),
                            BookingCount = g.Count(),
                            Date = g.Key
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync();
                    break;

                case "weekly":
                    result = await query
                        .GroupBy(b => new { 
                            Year = b.BookingDate.Year, 
                            Week = EF.Functions.DateDiffWeek(DateTime.MinValue, b.BookingDate) 
                        })
                        .Select(g => new BookingChartDataViewModel
                        {
                            Label = $"Tuần {g.Key.Week}",
                            BookingCount = g.Count(),
                            Date = g.Min(b => b.BookingDate)
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync();
                    break;

                case "monthly":
                    result = await query
                        .GroupBy(b => new { b.BookingDate.Year, b.BookingDate.Month })
                        .Select(g => new BookingChartDataViewModel
                        {
                            Label = $"{g.Key.Month}/{g.Key.Year}",
                            BookingCount = g.Count(),
                            Date = new DateTime(g.Key.Year, g.Key.Month, 1)
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync();
                    break;

                case "yearly":
                    result = await query
                        .GroupBy(b => b.BookingDate.Year)
                        .Select(g => new BookingChartDataViewModel
                        {
                            Label = g.Key.ToString(),
                            BookingCount = g.Count(),
                            Date = new DateTime(g.Key, 1, 1)
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync();
                    break;
            }

            return result;
        }

        public async Task<List<RevenueChartDataViewModel>> LayDuLieuBieuDoDoanhThuAsync(DateTime fromDate, DateTime toDate, string reportType = "daily")
        {
            var query = _context.Bookings
                .Where(b => b.BookingDate >= fromDate && b.BookingDate <= toDate);

            List<RevenueChartDataViewModel> result = new();

            switch (reportType.ToLower())
            {
                case "daily":
                    result = await query
                        .GroupBy(b => b.BookingDate.Date)
                        .Select(g => new RevenueChartDataViewModel
                        {
                            Label = g.Key.ToString("dd/MM"),
                            Revenue = g.Sum(b => b.TotalPrice),
                            Date = g.Key
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync();
                    break;

                case "weekly":
                    result = await query
                        .GroupBy(b => new { 
                            Year = b.BookingDate.Year, 
                            Week = EF.Functions.DateDiffWeek(DateTime.MinValue, b.BookingDate) 
                        })
                        .Select(g => new RevenueChartDataViewModel
                        {
                            Label = $"Tuần {g.Key.Week}",
                            Revenue = g.Sum(b => b.TotalPrice),
                            Date = g.Min(b => b.BookingDate)
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync();
                    break;

                case "monthly":
                    result = await query
                        .GroupBy(b => new { b.BookingDate.Year, b.BookingDate.Month })
                        .Select(g => new RevenueChartDataViewModel
                        {
                            Label = $"{g.Key.Month}/{g.Key.Year}",
                            Revenue = g.Sum(b => b.TotalPrice),
                            Date = new DateTime(g.Key.Year, g.Key.Month, 1)
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync();
                    break;

                case "yearly":
                    result = await query
                        .GroupBy(b => b.BookingDate.Year)
                        .Select(g => new RevenueChartDataViewModel
                        {
                            Label = g.Key.ToString(),
                            Revenue = g.Sum(b => b.TotalPrice),
                            Date = new DateTime(g.Key, 1, 1)
                        })
                        .OrderBy(x => x.Date)
                        .ToListAsync();
                    break;
            }

            return result;
        }

        public async Task<List<FieldPerformanceViewModel>> LayHieuSuatSanAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Bookings
                .Include(b => b.Field)
                .ThenInclude(f => f.FieldType)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(b => b.BookingDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(b => b.BookingDate <= toDate.Value);

            var result = await query
                .GroupBy(b => new { b.FieldId, b.Field.FieldName, b.Field.FieldType.TypeName })
                .Select(g => new FieldPerformanceViewModel
                {
                    FieldId = g.Key.FieldId,
                    FieldName = g.Key.FieldName,
                    FieldType = g.Key.TypeName,
                    BookingCount = g.Count(),
                    Revenue = g.Sum(b => b.TotalPrice),
                    UtilizationRate = 0 // Sẽ tính sau
                })
                .OrderByDescending(x => x.BookingCount)
                .ToListAsync();

            // Tính tỷ lệ sử dụng (cần thêm logic phức tạp hơn)
            foreach (var field in result)
            {
                field.UtilizationRate = Math.Min(100, (field.BookingCount * 10)); // Giả sử mỗi booking = 10% utilization
            }

            return result;
        }

        public async Task<List<TopCustomerViewModel>> LayTopKhachHangAsync(DateTime? fromDate = null, DateTime? toDate = null, int topCount = 5)
        {
            var query = _context.Bookings
                .Include(b => b.User)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(b => b.BookingDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(b => b.BookingDate <= toDate.Value);

            var result = await query
                .GroupBy(b => new { b.UserId, b.User.FirstName, b.User.LastName, b.User.Email, b.User.PhoneNumber })
                .Select(g => new TopCustomerViewModel
                {
                    Rank = 0, // Will be set later
                    CustomerName = (g.Key.FirstName + " " + g.Key.LastName).Trim(),
                    PhoneNumber = g.Key.PhoneNumber ?? "N/A",
                    OrderCount = g.Count(),
                    Revenue = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(topCount)
                .ToListAsync();

            return result;
        }

        public async Task<List<TopFieldViewModel>> LayTopSanAsync(DateTime? fromDate = null, DateTime? toDate = null, int topCount = 5)
        {
            var query = _context.Bookings
                .Include(b => b.Field)
                .ThenInclude(f => f.FieldType)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(b => b.BookingDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(b => b.BookingDate <= toDate.Value);

            var result = await query
                .GroupBy(b => new { b.FieldId, b.Field.FieldName, b.Field.FieldType.TypeName })
                .Select(g => new TopFieldViewModel
                {
                    Rank = 0, // Will be set later
                    FieldName = g.Key.FieldName,
                    FieldType = g.Key.TypeName,
                    Price = g.Average(b => b.TotalPrice),
                    BookingCount = g.Count(),
                    Revenue = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(topCount)
                .ToListAsync();

            // Set rank
            for (int i = 0; i < result.Count; i++)
            {
                result[i].Rank = i + 1;
            }

            return result;
        }

        public async Task<TimeAnalysisViewModel> LayPhanTichTheoThoiGianAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Bookings
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(b => b.BookingDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(b => b.BookingDate <= toDate.Value);

            var bookings = await query.ToListAsync();

            // Giả sử TimeSlot có StartTime hoặc sử dụng logic khác
            // Tạm thời sử dụng random distribution
            var totalBookings = bookings.Count;
            var morningBookings = (int)(totalBookings * 0.2); // 20%
            var afternoonBookings = (int)(totalBookings * 0.35); // 35%
            var eveningBookings = (int)(totalBookings * 0.45); // 45%

            var morningPercentage = totalBookings > 0 ? (morningBookings / (double)totalBookings) * 100 : 0;
            var afternoonPercentage = totalBookings > 0 ? (afternoonBookings / (double)totalBookings) * 100 : 0;
            var eveningPercentage = totalBookings > 0 ? (eveningBookings / (double)totalBookings) * 100 : 0;

            var mostPopularTimeSlot = new[] { 
                ("Buổi sáng", morningPercentage), 
                ("Buổi chiều", afternoonPercentage), 
                ("Buổi tối", eveningPercentage) 
            }.OrderByDescending(x => x.Item2).First().Item1;

            return new TimeAnalysisViewModel
            {
                MorningPercentage = (decimal)morningPercentage,
                AfternoonPercentage = (decimal)afternoonPercentage,
                EveningPercentage = (decimal)eveningPercentage,
                MorningBookings = morningBookings,
                AfternoonBookings = afternoonBookings,
                EveningBookings = eveningBookings,
                MostPopularTimeSlot = mostPopularTimeSlot
            };
        }

        public async Task<ComparisonViewModel> LaySoSanhVoiKyTruocAsync(DateTime fromDate, DateTime toDate, string reportType = "daily")
        {
            var currentPeriodBookings = await _context.Bookings
                .Where(b => b.BookingDate >= fromDate && b.BookingDate <= toDate)
                .CountAsync();
            var currentPeriodRevenue = await _context.Bookings
                .Where(b => b.BookingDate >= fromDate && b.BookingDate <= toDate)
                .SumAsync(b => b.TotalPrice);

            // Tính kỳ trước
            DateTime previousFromDate, previousToDate;
            var periodLength = (toDate - fromDate).Days + 1;

            switch (reportType.ToLower())
            {
                case "daily":
                    previousFromDate = fromDate.AddDays(-1);
                    previousToDate = toDate.AddDays(-1);
                    break;
                case "weekly":
                    previousFromDate = fromDate.AddDays(-7);
                    previousToDate = toDate.AddDays(-7);
                    break;
                case "monthly":
                    previousFromDate = fromDate.AddMonths(-1);
                    previousToDate = toDate.AddMonths(-1);
                    break;
                case "yearly":
                    previousFromDate = fromDate.AddYears(-1);
                    previousToDate = toDate.AddYears(-1);
                    break;
                default:
                    previousFromDate = fromDate.AddDays(-periodLength);
                    previousToDate = toDate.AddDays(-periodLength);
                    break;
            }

            var previousPeriodBookings = await _context.Bookings
                .Where(b => b.BookingDate >= previousFromDate && b.BookingDate <= previousToDate)
                .CountAsync();
            var previousPeriodRevenue = await _context.Bookings
                .Where(b => b.BookingDate >= previousFromDate && b.BookingDate <= previousToDate)
                .SumAsync(b => b.TotalPrice);

            var bookingGrowthRate = previousPeriodBookings > 0 
                ? ((currentPeriodBookings - previousPeriodBookings) / (double)previousPeriodBookings) * 100 
                : 0;
            var revenueGrowthRate = previousPeriodRevenue > 0 
                ? ((double)(currentPeriodRevenue - previousPeriodRevenue) / (double)previousPeriodRevenue) * 100 
                : 0;

            return new ComparisonViewModel
            {
                BookingGrowthRate = (decimal)bookingGrowthRate,
                RevenueGrowthRate = (decimal)revenueGrowthRate,
                CustomerGrowthRate = 0, // Có thể tính thêm
                PeriodLabel = $"So với {reportType} trước",
                PreviousPeriodBookings = previousPeriodBookings,
                CurrentPeriodBookings = currentPeriodBookings,
                PreviousPeriodRevenue = previousPeriodRevenue,
                CurrentPeriodRevenue = currentPeriodRevenue
            };
        }

        // Additional methods for AdminController
        public async Task<TimeAnalysisViewModel> LayPhanTichThoiGianAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            return await LayPhanTichTheoThoiGianAsync(fromDate, toDate);
        }
    }
}
