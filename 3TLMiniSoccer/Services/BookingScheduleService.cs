using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using _3TLMiniSoccer.ViewModels;

namespace _3TLMiniSoccer.Services
{
    public class BookingScheduleService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookingScheduleService> _logger;

        public BookingScheduleService(ApplicationDbContext context, ILogger<BookingScheduleService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ViewModels.BookingScheduleStatistics> LayThongKeAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var totalBookings = await _context.Bookings.CountAsync();
            var todayBookings = await _context.Bookings
                .Where(b => b.BookingDate >= today && b.BookingDate < tomorrow)
                .CountAsync();
            var confirmedBookings = await _context.Bookings
                .Where(b => b.Status == "Confirmed")
                .CountAsync();
            var pendingBookings = await _context.Bookings
                .Where(b => b.Status == "Pending")
                .CountAsync();
            var cancelledBookings = await _context.Bookings
                .Where(b => b.Status == "Cancelled")
                .CountAsync();
            var completedBookings = await _context.Bookings
                .Where(b => b.Status == "Completed")
                .CountAsync();

            var totalRevenue = await _context.Bookings
                .Where(b => b.PaymentStatus == "Paid")
                .SumAsync(b => b.TotalPrice);
            var todayRevenue = await _context.Bookings
                .Where(b => b.BookingDate >= today && b.BookingDate < tomorrow && b.PaymentStatus == "Paid")
                .SumAsync(b => b.TotalPrice);

            return new ViewModels.BookingScheduleStatistics
            {
                TotalBookings = totalBookings,
                TodayBookings = todayBookings,
                ConfirmedBookings = confirmedBookings,
                PendingBookings = pendingBookings,
                CancelledBookings = cancelledBookings,
                CompletedBookings = completedBookings,
                TotalRevenue = totalRevenue,
                TodayRevenue = todayRevenue
            };
        }

        public async Task<ViewModels.BookingScheduleStatistics> LayThongKeTheoNgayAsync(DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            var totalBookings = await _context.Bookings
                .Where(b => b.BookingDate >= startDate && b.BookingDate < endDate)
                .CountAsync();
            var confirmedBookings = await _context.Bookings
                .Where(b => b.BookingDate >= startDate && b.BookingDate < endDate && b.Status == "Confirmed")
                .CountAsync();
            var pendingBookings = await _context.Bookings
                .Where(b => b.BookingDate >= startDate && b.BookingDate < endDate && b.Status == "Pending")
                .CountAsync();
            var cancelledBookings = await _context.Bookings
                .Where(b => b.BookingDate >= startDate && b.BookingDate < endDate && b.Status == "Cancelled")
                .CountAsync();
            var completedBookings = await _context.Bookings
                .Where(b => b.BookingDate >= startDate && b.BookingDate < endDate && b.Status == "Completed")
                .CountAsync();

            var totalRevenue = await _context.Bookings
                .Where(b => b.BookingDate >= startDate && b.BookingDate < endDate && b.PaymentStatus == "Paid")
                .SumAsync(b => b.TotalPrice);

            return new ViewModels.BookingScheduleStatistics
            {
                TotalBookings = totalBookings,
                TodayBookings = totalBookings,
                ConfirmedBookings = confirmedBookings,
                PendingBookings = pendingBookings,
                CancelledBookings = cancelledBookings,
                CompletedBookings = completedBookings,
                TotalRevenue = totalRevenue,
                TodayRevenue = totalRevenue
            };
        }

        public async Task<BookingScheduleViewModel> LayDanhSachDatSanAsync(int page = 1, int pageSize = 10, string? search = null, int? fieldId = null, DateTime? date = null, string? status = null)
        {
            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Field)
                .ThenInclude(f => f.FieldType)
                .Include(b => b.ConfirmedByUser)
                .Include(b => b.CancelledByUser)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => 
                    b.BookingCode.Contains(search) ||
                    (b.User != null && (b.User.FirstName.Contains(search) || b.User.LastName.Contains(search))) ||
                    (b.GuestName != null && b.GuestName.Contains(search)) ||
                    (b.GuestPhone != null && b.GuestPhone.Contains(search)) ||
                    (b.GuestEmail != null && b.GuestEmail.Contains(search)));
            }

            if (fieldId.HasValue)
            {
                query = query.Where(b => b.FieldId == fieldId.Value);
            }

            if (date.HasValue)
            {
                var startDate = date.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(b => b.BookingDate >= startDate && b.BookingDate < endDate);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Status == status);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Convert to view model
            _logger.LogInformation("Converting {Count} bookings to view model", bookings.Count);
            var bookingItems = bookings.Select(b => {
                _logger.LogInformation("Processing booking: ID={BookingId}, Code={BookingCode}, Status={Status}", 
                    b.BookingId, b.BookingCode, b.Status);
                
                var bookingItem = new BookingItem
                {
                    BookingId = b.BookingId,
                    BookingCode = b.BookingCode,
                    CustomerName = b.User != null ? $"{b.User.FirstName} {b.User.LastName}" : b.GuestName ?? "",
                    CustomerPhone = b.User?.PhoneNumber ?? b.GuestPhone ?? "",
                    CustomerEmail = b.User?.Email ?? b.GuestEmail ?? "",
                    FieldName = $"{b.Field.FieldName} ({b.Field.FieldType.TypeName})",
                    FieldType = b.Field.FieldType.TypeName,
                    BookingDate = b.BookingDate,
                    StartTime = b.StartTime.ToString(@"hh\:mm"),
                    EndTime = b.EndTime.ToString(@"hh\:mm"),
                    Duration = b.Duration,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status,
                    StatusText = GetStatusText(b.Status),
                    StatusColor = GetStatusColor(b.Status),
                    PaymentStatus = b.PaymentStatus,
                    PaymentMethod = b.PaymentMethod ?? "",
                    Notes = b.Notes,
                    CreatedAt = b.CreatedAt,
                    ConfirmedAt = b.ConfirmedAt,
                    ConfirmedBy = b.ConfirmedByUser != null ? $"{b.ConfirmedByUser.FirstName} {b.ConfirmedByUser.LastName}" : null,
                    CancelledAt = b.CancelledAt,
                    CancelledBy = b.CancelledByUser != null ? $"{b.CancelledByUser.FirstName} {b.CancelledByUser.LastName}" : null
                };
                
                _logger.LogInformation("Created BookingItem: ID={BookingId}, Code={BookingCode}", 
                    bookingItem.BookingId, bookingItem.BookingCode);
                return bookingItem;
            }).ToList();

            // Get statistics
            var statistics = await LayThongKeAsync();

            // Get available fields and time slots
            var availableFields = await LayDanhSachSanKhaDungAsync();
            return new BookingScheduleViewModel
            {
                Bookings = bookingItems,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                Statistics = statistics,
                AvailableFields = availableFields
            };
        }

        public async Task<Booking?> LayDatSanTheoIdAsync(int bookingId)
        {
            _logger.LogInformation("Looking for booking with ID: {BookingId}", bookingId);
            
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Field)
                .ThenInclude(f => f.FieldType)
                .Include(b => b.ConfirmedByUser)
                .Include(b => b.CancelledByUser)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
                
            if (booking == null)
            {
                _logger.LogWarning("Booking not found with ID: {BookingId}", bookingId);
                
                // Log all existing booking IDs for debugging
                var allBookingIds = await _context.Bookings
                    .Select(b => b.BookingId)
                    .OrderBy(id => id)
                    .ToListAsync();
                _logger.LogInformation("Available booking IDs: {BookingIds}", string.Join(", ", allBookingIds));
            }
            else
            {
                _logger.LogInformation("Found booking {BookingId} with status: {Status}", bookingId, booking.Status);
            }
            
            return booking;
        }

        public async Task<Booking?> LayDatSanTheoMaAsync(string bookingCode)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Field)
                .ThenInclude(f => f.FieldType)
                .Include(b => b.ConfirmedByUser)
                .Include(b => b.CancelledByUser)
                .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);
        }

        public async Task<bool> TaoDatSanAsync(CreateBookingRequest request)
        {
            try
            {
                // Generate booking code
                var bookingCode = await TaoMaDatSanAsync();

                var booking = new Booking
                {
                    BookingCode = bookingCode,
                    UserId = request.UserId,
                    GuestName = request.GuestName,
                    GuestPhone = request.GuestPhone,
                    GuestEmail = request.GuestEmail,
                    FieldId = request.FieldId,
                    BookingDate = request.BookingDate,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    Duration = request.Duration,
                    Status = request.Status,
                    Notes = request.Notes,
                    TotalPrice = await TinhGiaAsync(request.FieldId, request.StartTime, request.EndTime, request.BookingDate),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CapNhatDatSanAsync(int bookingId, UpdateBookingRequest request)
        {
            try
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null) return false;

                booking.UserId = request.UserId;
                booking.GuestName = request.GuestName;
                booking.GuestPhone = request.GuestPhone;
                booking.GuestEmail = request.GuestEmail;
                booking.FieldId = request.FieldId;
                booking.BookingDate = request.BookingDate;
                booking.StartTime = request.StartTime;
                booking.EndTime = request.EndTime;
                booking.Duration = request.Duration;
                booking.Status = request.Status;
                booking.Notes = request.Notes;
                booking.TotalPrice = await TinhGiaAsync(request.FieldId, request.StartTime, request.EndTime, request.BookingDate);
                booking.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> XoaDatSanAsync(int bookingId)
        {
            try
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null) return false;

                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> XacNhanDatSanAsync(int bookingId, int confirmedBy)
        {
            try
            {
                _logger.LogInformation("Starting confirmation process for booking {BookingId} by user {ConfirmedBy}", bookingId, confirmedBy);
                
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null) 
                {
                    _logger.LogWarning("Booking not found: {BookingId}", bookingId);
                    
                    // Log all existing booking IDs for debugging
                    var allBookingIds = await _context.Bookings
                        .Select(b => b.BookingId)
                        .OrderBy(id => id)
                        .ToListAsync();
                    _logger.LogInformation("Available booking IDs: {BookingIds}", string.Join(", ", allBookingIds));
                    
                    return false;
                }

                _logger.LogInformation("Found booking {BookingId} with status: {Status}", bookingId, booking.Status);

                // Chỉ xác nhận khi booking đang ở trạng thái Pending
                if (booking.Status != "Pending")
                {
                    _logger.LogWarning("Cannot confirm booking {BookingId} with status: {Status}. Only Pending bookings can be confirmed.", bookingId, booking.Status);
                    return false;
                }

                _logger.LogInformation("Updating booking {BookingId} to Confirmed status", bookingId);
                
                booking.Status = "Confirmed";
                booking.ConfirmedAt = DateTime.Now;
                booking.ConfirmedBy = confirmedBy;
                booking.UpdatedAt = DateTime.Now;

                _logger.LogInformation("Updated booking {BookingId} status to Confirmed", bookingId);

                // Cập nhật field schedule
                await UpdateFieldScheduleAsync(booking.FieldId, booking.BookingDate, booking.StartTime, booking.EndTime, "Booked", booking.BookingId);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully confirmed booking {BookingId}", bookingId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming booking {BookingId}: {Message}", bookingId, ex.Message);
                return false;
            }
        }

        public async Task<bool> HuyDatSanAsync(int bookingId, int cancelledBy, string? reason = null)
        {
            try
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null)
                {
                    _logger.LogWarning("Booking not found: {BookingId}", bookingId);
                    return false;
                }

                // Kiểm tra trạng thái booking
                if (booking.Status == "Cancelled")
                {
                    _logger.LogWarning("Booking already cancelled: {BookingId}", bookingId);
                    return false;
                }

                if (booking.Status == "Completed")
                {
                    _logger.LogWarning("Cannot cancel completed booking: {BookingId}", bookingId);
                    return false;
                }

                // Log trạng thái hiện tại để debug
                _logger.LogInformation("Cancelling booking {BookingId} with status: {Status}", bookingId, booking.Status);

                // Cập nhật trạng thái booking
                booking.Status = "Cancelled";
                booking.CancelledAt = DateTime.Now;
                booking.CancelledBy = cancelledBy;
                if (!string.IsNullOrEmpty(reason))
                {
                    booking.Notes = (booking.Notes ?? "") + $"\nLý do hủy: {reason}";
                }
                booking.UpdatedAt = DateTime.Now;

                // Cập nhật field schedule để giải phóng slot
                await UpdateFieldScheduleAsync(booking.FieldId, booking.BookingDate, booking.StartTime, booking.EndTime, "Available", null);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Booking cancelled successfully: {BookingId}", bookingId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling booking {BookingId}: {Message}", bookingId, ex.Message);
                return false;
            }
        }

        public async Task<bool> HoanThanhDatSanAsync(int bookingId)
        {
            try
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null) return false;

                booking.Status = "Completed";
                booking.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Field>> LayDanhSachSanKhaDungAsync()
        {
            return await _context.Fields
                .Include(f => f.FieldType)
                .Where(f => f.Status == "Active")
                .OrderBy(f => f.FieldName)
                .ToListAsync();
        }


        public async Task<bool> KiemTraSanKhaDungAsync(int fieldId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeBookingId = null)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            var query = _context.Bookings
                .Where(b => b.FieldId == fieldId && 
                           b.BookingDate >= startDate && 
                           b.BookingDate < endDate && 
                           b.Status != "Cancelled");

            if (excludeBookingId.HasValue)
            {
                query = query.Where(b => b.BookingId != excludeBookingId.Value);
            }

            var conflictingBookings = await query.ToListAsync();

            // Check for time conflicts
            foreach (var booking in conflictingBookings)
            {
                if (startTime < booking.EndTime && endTime > booking.StartTime)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<decimal> TinhGiaAsync(int fieldId, TimeSpan startTime, TimeSpan endTime, DateTime date)
        {
            // Get field pricing
            var field = await _context.Fields
                .Include(f => f.FieldType)
                .FirstOrDefaultAsync(f => f.FieldId == fieldId);

            if (field == null) return 0;

            // Calculate duration in hours
            var duration = (endTime - startTime).TotalHours;

            // For now, use base price from field type
            // In a real implementation, you might want to add pricing rules based on time ranges
            return field.FieldType.BasePrice * (decimal)duration;
        }

        private async Task<string> TaoMaDatSanAsync()
        {
            var today = DateTime.Today;
            var count = await _context.Bookings
                .Where(b => b.CreatedAt >= today)
                .CountAsync();

            return $"BK{today:yyyyMMdd}{(count + 1):D3}";
        }

        private string GetStatusText(string status)
        {
            return status switch
            {
                "Pending" => "Chờ xác nhận",
                "Confirmed" => "Đã xác nhận",
                "Playing" => "Đang chơi",
                "Completed" => "Hoàn thành",
                "Cancelled" => "Đã hủy",
                _ => status
            };
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "Pending" => "bg-yellow-100 text-yellow-800",
                "Confirmed" => "bg-green-100 text-green-800",
                "Playing" => "bg-blue-100 text-blue-800",
                "Completed" => "bg-gray-100 text-gray-800",
                "Cancelled" => "bg-red-100 text-red-800",
                _ => "bg-gray-100 text-gray-800"
            };
        }

        // Additional methods for AdminController
        public async Task<List<Booking>> LayTatCaDatSanAsync()
        {
            return await _context.Bookings
                .Include(b => b.Field)
                .Include(b => b.User)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> KiemTraSanCoSanAsync(int fieldId, DateTime date, TimeSpan startTime, TimeSpan endTime)
        {
            return await KiemTraSanKhaDungAsync(fieldId, date, startTime, endTime);
        }

        public async Task<List<Field>> LaySanCoSanAsync()
        {
            return await _context.Fields
                .Where(f => f.Status == "Active")
                .Include(f => f.FieldType)
                .OrderBy(f => f.FieldName)
                .ToListAsync();
        }

        private async Task UpdateFieldScheduleAsync(int fieldId, DateTime date, TimeSpan startTime, TimeSpan endTime, string status, int? bookingId)
        {
            try
            {
                var fieldSchedule = await _context.FieldSchedules
                    .FirstOrDefaultAsync(fs => fs.FieldId == fieldId && 
                                              fs.Date.Date == date.Date && 
                                              fs.StartTime == startTime && 
                                              fs.EndTime == endTime);

                if (fieldSchedule == null)
                {
                    fieldSchedule = new FieldSchedule
                    {
                        FieldId = fieldId,
                        Date = date.Date,
                        StartTime = startTime,
                        EndTime = endTime,
                        Status = status,
                        BookingId = bookingId,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    _context.FieldSchedules.Add(fieldSchedule);
                }
                else
                {
                    fieldSchedule.Status = status;
                    fieldSchedule.BookingId = bookingId;
                    fieldSchedule.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException?.Message?.Contains("database triggers") == true || 
                                                                        ex.InnerException?.Message?.Contains("OUTPUT clause") == true)
            {
                // Handle trigger conflict by using raw SQL
                var dateOnly = date.Date;
                var createdAt = DateTime.Now;
                var updatedAt = DateTime.Now;

                // Check if record exists using a different approach
                var existingSchedule = await _context.FieldSchedules
                    .FirstOrDefaultAsync(fs => fs.FieldId == fieldId && 
                                              fs.Date.Date == dateOnly && 
                                              fs.StartTime == startTime && 
                                              fs.EndTime == endTime);

                if (existingSchedule == null)
                {
                    // Insert new record
                    var insertSql = @"
                        INSERT INTO FieldSchedules (FieldId, Date, StartTime, EndTime, Status, BookingId, CreatedAt, UpdatedAt)
                        VALUES (@FieldId, @Date, @StartTime, @EndTime, @Status, @BookingId, @CreatedAt, @UpdatedAt)";

                    await _context.Database.ExecuteSqlRawAsync(insertSql,
                        new Microsoft.Data.SqlClient.SqlParameter("@FieldId", fieldId),
                        new Microsoft.Data.SqlClient.SqlParameter("@Date", dateOnly),
                        new Microsoft.Data.SqlClient.SqlParameter("@StartTime", startTime),
                        new Microsoft.Data.SqlClient.SqlParameter("@EndTime", endTime),
                        new Microsoft.Data.SqlClient.SqlParameter("@Status", status),
                        new Microsoft.Data.SqlClient.SqlParameter("@BookingId", bookingId ?? (object)DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@CreatedAt", createdAt),
                        new Microsoft.Data.SqlClient.SqlParameter("@UpdatedAt", updatedAt));
                }
                else
                {
                    // Update existing record
                    var updateSql = @"
                        UPDATE FieldSchedules 
                        SET Status = @Status, BookingId = @BookingId, UpdatedAt = @UpdatedAt
                        WHERE FieldId = @FieldId AND Date = @Date AND StartTime = @StartTime AND EndTime = @EndTime";

                    await _context.Database.ExecuteSqlRawAsync(updateSql,
                        new Microsoft.Data.SqlClient.SqlParameter("@Status", status),
                        new Microsoft.Data.SqlClient.SqlParameter("@BookingId", bookingId ?? (object)DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@UpdatedAt", updatedAt),
                        new Microsoft.Data.SqlClient.SqlParameter("@FieldId", fieldId),
                        new Microsoft.Data.SqlClient.SqlParameter("@Date", dateOnly),
                        new Microsoft.Data.SqlClient.SqlParameter("@StartTime", startTime),
                        new Microsoft.Data.SqlClient.SqlParameter("@EndTime", endTime));
                }
            }
        }
    }
}
