using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace _3TLMiniSoccer.Services
{
    public class BookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly FieldService _fieldService;

        public BookingService(ApplicationDbContext context, FieldService fieldService)
        {
            _context = context;
            _fieldService = fieldService;
        }

        #region Booking Management

        public async Task<List<Booking>> LayTatCaDatSanAsync()
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Field)
                .ThenInclude(f => f.FieldType)
                .Include(b => b.ConfirmedByUser)
                .Include(b => b.CancelledByUser)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<Booking?> LayDatSanTheoIdAsync(int bookingId)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Field)
                .ThenInclude(f => f.FieldType)
                .Include(b => b.ConfirmedByUser)
                .Include(b => b.CancelledByUser)
                .Include(b => b.BookingSessions)
                .Include(b => b.Transactions)
                .ThenInclude(t => t.PaymentMethod)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
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

        public async Task<List<Booking>> LayDatSanTheoNguoiDungAsync(int userId)
        {
            return await _context.Bookings
                .Include(b => b.Field)
                .ThenInclude(f => f.FieldType)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<List<Booking>> LayDatSanTheoSanAsync(int fieldId)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Where(b => b.FieldId == fieldId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<List<Booking>> LayDatSanTheoNgayAsync(DateTime date)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Field)
                .ThenInclude(f => f.FieldType)
                .Where(b => b.BookingDate.Date == date.Date)
                .OrderBy(b => b.StartTime)
                .ToListAsync();
        }

        public async Task<List<Booking>> LayDatSanTheoKhoangThoiGianAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Field)
                .ThenInclude(f => f.FieldType)
                .Where(b => b.BookingDate.Date >= startDate.Date && b.BookingDate.Date <= endDate.Date)
                .OrderBy(b => b.BookingDate)
                .ThenBy(b => b.StartTime)
                .ToListAsync();
        }

        public async Task<List<Booking>> LayDatSanTheoTrangThaiAsync(string status)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Field)
                .ThenInclude(f => f.FieldType)
                .Where(b => b.Status == status)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<Booking> TaoDatSanAsync(Booking booking)
        {
            booking.BookingCode = await TaoMaDatSanAsync();
            booking.CreatedAt = DateTime.Now;
            booking.UpdatedAt = DateTime.Now;

            _context.Bookings.Add(booking);
            
            try
            {
                // Save changes without using OUTPUT clause to avoid trigger conflicts
                await _context.SaveChangesAsync();
                
                // Reload the booking to get the generated ID
                await _context.Entry(booking).ReloadAsync();

                // Update field schedule
                await UpdateFieldScheduleAsync(booking.FieldId, booking.BookingDate, booking.StartTime, booking.EndTime, "Booked", booking.BookingId);

                return booking;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException?.Message?.Contains("database triggers") == true || 
                                                                        ex.InnerException?.Message?.Contains("OUTPUT clause") == true)
            {
                // Handle the trigger conflict by using raw SQL
                var bookingCode = booking.BookingCode;
                var userId = booking.UserId;
                var guestName = booking.GuestName;
                var guestPhone = booking.GuestPhone;
                var guestEmail = booking.GuestEmail;
                var fieldId = booking.FieldId;
                var bookingDate = booking.BookingDate;
                var startTime = booking.StartTime;
                var endTime = booking.EndTime;
                var duration = booking.Duration;
                var status = booking.Status;
                var totalPrice = booking.TotalPrice;
                var paymentStatus = booking.PaymentStatus;
                var paymentMethod = booking.PaymentMethod;
                var paymentReference = booking.PaymentReference;
                var notes = booking.Notes;
                var createdAt = booking.CreatedAt;
                var updatedAt = booking.UpdatedAt;

                // Use raw SQL to insert without OUTPUT clause
                var sql = @"
                    INSERT INTO Bookings (BookingCode, UserId, GuestName, GuestPhone, GuestEmail, FieldId, BookingDate, StartTime, EndTime, Duration, Status, TotalPrice, PaymentStatus, PaymentMethod, PaymentReference, Notes, CreatedAt, UpdatedAt)
                    VALUES (@BookingCode, @UserId, @GuestName, @GuestPhone, @GuestEmail, @FieldId, @BookingDate, @StartTime, @EndTime, @Duration, @Status, @TotalPrice, @PaymentStatus, @PaymentMethod, @PaymentReference, @Notes, @CreatedAt, @UpdatedAt);";

                await _context.Database.ExecuteSqlRawAsync(sql,
                    new Microsoft.Data.SqlClient.SqlParameter("@BookingCode", bookingCode),
                    new Microsoft.Data.SqlClient.SqlParameter("@UserId", userId ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@GuestName", guestName ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@GuestPhone", guestPhone ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@GuestEmail", guestEmail ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@FieldId", fieldId),
                    new Microsoft.Data.SqlClient.SqlParameter("@BookingDate", bookingDate),
                    new Microsoft.Data.SqlClient.SqlParameter("@StartTime", startTime),
                    new Microsoft.Data.SqlClient.SqlParameter("@EndTime", endTime),
                    new Microsoft.Data.SqlClient.SqlParameter("@Duration", duration),
                    new Microsoft.Data.SqlClient.SqlParameter("@Status", status),
                    new Microsoft.Data.SqlClient.SqlParameter("@TotalPrice", totalPrice),
                    new Microsoft.Data.SqlClient.SqlParameter("@PaymentStatus", paymentStatus),
                    new Microsoft.Data.SqlClient.SqlParameter("@PaymentMethod", paymentMethod ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@PaymentReference", paymentReference ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@Notes", notes ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@CreatedAt", createdAt),
                    new Microsoft.Data.SqlClient.SqlParameter("@UpdatedAt", updatedAt));

                // Get the inserted booking
                var insertedBooking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);

                if (insertedBooking != null)
                {
                    // Update field schedule
                    await UpdateFieldScheduleAsync(insertedBooking.FieldId, insertedBooking.BookingDate, insertedBooking.StartTime, insertedBooking.EndTime, "Booked", insertedBooking.BookingId);
                }

                return insertedBooking ?? booking;
            }
        }

        public async Task<bool> CapNhatDatSanAsync(Booking booking)
        {
            try
            {
                booking.UpdatedAt = DateTime.Now;
                _context.Bookings.Update(booking);
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
                if (booking != null)
                {
                    // Update field schedule
                    await UpdateFieldScheduleAsync(booking.FieldId, booking.BookingDate, booking.StartTime, booking.EndTime, "Available", null);

                    _context.Bookings.Remove(booking);
                    await _context.SaveChangesAsync();
                }
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
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking != null && booking.Status == "Pending")
                {
                    booking.Status = "Confirmed";
                    booking.ConfirmedAt = DateTime.Now;
                    booking.ConfirmedBy = confirmedBy;
                    booking.UpdatedAt = DateTime.Now;

                    // Update field schedule
                    await UpdateFieldScheduleAsync(booking.FieldId, booking.BookingDate, booking.StartTime, booking.EndTime, "Booked", booking.BookingId);

                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> HuyDatSanAsync(int bookingId, int cancelledBy, string reason)
        {
            try
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking != null && booking.Status != "Completed")
                {
                    booking.Status = "Cancelled";
                    booking.CancelledAt = DateTime.Now;
                    booking.CancelledBy = cancelledBy;
                    booking.Notes = reason;
                    booking.UpdatedAt = DateTime.Now;

                    // Update field schedule
                    await UpdateFieldScheduleAsync(booking.FieldId, booking.BookingDate, booking.StartTime, booking.EndTime, "Available", null);

                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> TaoMaDatSanAsync()
        {
            var today = DateTime.Now.ToString("yyyyMMdd");
            var counter = 1;

            while (counter <= 999)
            {
                var code = $"BK{today}{counter:D3}";
                var exists = await _context.Bookings.AnyAsync(b => b.BookingCode == code);
                if (!exists)
                    return code;
                counter++;
            }

            return $"BK{today}{DateTime.Now.Ticks % 1000:D3}";
        }

        public async Task<bool> KiemTraDatSanHopLeAsync(int fieldId, DateTime date, TimeSpan startTime, TimeSpan endTime)
        {
            return await _fieldService.KiemTraSanCoSanAsync(fieldId, date, startTime, endTime);
        }

        #endregion

        #region Booking Sessions

        public async Task<List<BookingSession>> LayPhienDatSanAsync(int bookingId)
        {
            return await _context.BookingSessions
                .Where(bs => bs.BookingId == bookingId)
                .OrderByDescending(bs => bs.CreatedAt)
                .ToListAsync();
        }

        public async Task<BookingSession?> LayPhienDatSanTheoIdAsync(int sessionId)
        {
            return await _context.BookingSessions
                .Include(bs => bs.Booking)
                .FirstOrDefaultAsync(bs => bs.SessionId == sessionId);
        }

        public async Task<BookingSession> TaoPhienDatSanAsync(BookingSession session)
        {
            session.CreatedAt = DateTime.Now;
            _context.BookingSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<bool> CapNhatPhienDatSanAsync(BookingSession session)
        {
            try
            {
                _context.BookingSessions.Update(session);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CheckInAsync(int bookingId, int staffId)
        {
            try
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null || booking.Status != "Confirmed")
                    return false;

                // Update booking status to Playing
                booking.Status = "Playing";
                booking.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CheckOutAsync(int bookingId, int staffId, int? actualDuration = null, decimal overtimeFee = 0)
        {
            try
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null)
                    return false;

                // Update booking status to Completed
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

        public async Task<bool> KiemTraCheckInAsync(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            return booking != null && booking.Status == "Playing";
        }

        #endregion

        #region Search and Filter

        public async Task<List<Booking>> TimKiemDatSanAsync(string searchTerm)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Field)
                .ThenInclude(f => f.FieldType)
                .Where(b => b.BookingCode.Contains(searchTerm) ||
                           (b.User != null && (b.User.FirstName.Contains(searchTerm) || b.User.LastName.Contains(searchTerm))) ||
                           (b.GuestName != null && b.GuestName.Contains(searchTerm)) ||
                           (b.GuestPhone != null && b.GuestPhone.Contains(searchTerm)))
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Booking>> LocDatSanAsync(DateTime? startDate, DateTime? endDate, int? fieldId, string? status)
        {
            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Field)
                .ThenInclude(f => f.FieldType)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(b => b.BookingDate.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(b => b.BookingDate.Date <= endDate.Value.Date);

            if (fieldId.HasValue)
                query = query.Where(b => b.FieldId == fieldId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.Status == status);

            return await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
        }

        public async Task<List<Booking>> LayDatSanHomNayAsync()
        {
            return await LayDatSanTheoNgayAsync(DateTime.Today);
        }

        public async Task<List<Booking>> LayDatSanSapToiAsync(int days = 7)
        {
            var endDate = DateTime.Today.AddDays(days);
            return await LayDatSanTheoKhoangThoiGianAsync(DateTime.Today, endDate);
        }

        public async Task<List<Booking>> LayDatSanQuaHanAsync()
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Field)
                .ThenInclude(f => f.FieldType)
                .Where(b => b.BookingDate.Date < DateTime.Today && 
                           (b.Status == "Confirmed" || b.Status == "Playing"))
                .OrderBy(b => b.BookingDate)
                .ToListAsync();
        }

        #endregion

        #region Statistics

        public async Task<int> LayTongSoDatSanAsync()
        {
            return await _context.Bookings.CountAsync();
        }

        public async Task<int> LaySoDatSanTheoTrangThaiAsync(string status)
        {
            return await _context.Bookings.CountAsync(b => b.Status == status);
        }

        public async Task<decimal> LayTongDoanhThuAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Bookings.Where(b => b.Status == "Confirmed" || b.Status == "Completed");

            if (startDate.HasValue)
                query = query.Where(b => b.BookingDate.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(b => b.BookingDate.Date <= endDate.Value.Date);

            return await query.SumAsync(b => b.TotalPrice);
        }

        public async Task<decimal> LayDoanhThuTheoSanAsync(int fieldId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Bookings.Where(b => b.FieldId == fieldId && 
                                                    (b.Status == "Confirmed" || b.Status == "Completed"));

            if (startDate.HasValue)
                query = query.Where(b => b.BookingDate.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(b => b.BookingDate.Date <= endDate.Value.Date);

            return await query.SumAsync(b => b.TotalPrice);
        }

        public async Task<List<dynamic>> LayThongKeDatSanTheoNgayAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Bookings
                .Where(b => b.BookingDate.Date >= startDate.Date && b.BookingDate.Date <= endDate.Date)
                .GroupBy(b => b.BookingDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalBookings = g.Count(),
                    TotalRevenue = g.Sum(b => b.TotalPrice),
                    ConfirmedBookings = g.Count(b => b.Status == "Confirmed"),
                    CompletedBookings = g.Count(b => b.Status == "Completed")
                })
                .OrderBy(x => x.Date)
                .ToListAsync<dynamic>();
        }

        public async Task<List<dynamic>> LayThongKeDatSanTheoSanAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Bookings
                .Include(b => b.Field)
                .ThenInclude(f => f.FieldType)
                .Where(b => b.BookingDate.Date >= startDate.Date && b.BookingDate.Date <= endDate.Date)
                .GroupBy(b => new { b.FieldId, b.Field.FieldName, b.Field.FieldType.TypeName })
                .Select(g => new
                {
                    FieldId = g.Key.FieldId,
                    FieldName = g.Key.FieldName,
                    FieldType = g.Key.TypeName,
                    TotalBookings = g.Count(),
                    TotalRevenue = g.Sum(b => b.TotalPrice),
                    ConfirmedBookings = g.Count(b => b.Status == "Confirmed"),
                    CompletedBookings = g.Count(b => b.Status == "Completed")
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToListAsync<dynamic>();
        }

        #endregion

        #region Helper Methods

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

        #endregion

        public async Task<List<Booking>> LayDatSanGanDayAsync(int count)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Field)
                .ThenInclude(f => f.FieldType)
                .Where(b => b.Status == "Completed")
                .OrderByDescending(b => b.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Booking>> LayKhungGioDaDatAsync(int fieldId, DateTime date)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Where(b => b.FieldId == fieldId && 
                           b.BookingDate.Date == date.Date && 
                           (b.Status == "Confirmed" || b.Status == "Pending"))
                .OrderBy(b => b.StartTime)
                .ToListAsync();
        }

        #region Dashboard Statistics

        public async Task<int> LaySoDatSanHomNayAsync()
        {
            return await _context.Bookings
                .CountAsync(b => b.BookingDate.Date == DateTime.Today);
        }

        public async Task<decimal> LayDoanhThuDatSanHomNayAsync()
        {
            return await _context.Bookings
                .Where(b => b.BookingDate.Date == DateTime.Today && 
                           (b.Status == "Confirmed" || b.Status == "Completed"))
                .SumAsync(b => b.TotalPrice);
        }

        public async Task<List<dynamic>> LayThongKeTrangThaiDatSanHomNayAsync()
        {
            var today = DateTime.Today;
            var stats = await _context.Bookings
                .Where(b => b.BookingDate.Date == today)
                .GroupBy(b => b.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count(),
                    Percentage = 0.0 // Will be calculated in controller
                })
                .ToListAsync<dynamic>();

            return stats;
        }

        public async Task<List<dynamic>> LayTopSanTheoDatSanAsync(int count)
        {
            return await _context.Bookings
                .Include(b => b.Field)
                .ThenInclude(f => f.FieldType)
                .Where(b => b.Status == "Confirmed" || b.Status == "Completed")
                .GroupBy(b => new { b.FieldId, b.Field.FieldName, b.Field.FieldType.TypeName, b.Field.FieldType.BasePrice })
                .Select(g => new
                {
                    FieldId = g.Key.FieldId,
                    FieldName = g.Key.FieldName,
                    FieldType = g.Key.TypeName,
                    Price = g.Key.BasePrice,
                    BookingCount = g.Count(),
                    Revenue = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(x => x.BookingCount)
                .Take(count)
                .ToListAsync<dynamic>();
        }

        public async Task<List<dynamic>> LayTopKhachHangTheoDatSanAsync(int count)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Where(b => b.UserId != null && (b.Status == "Confirmed" || b.Status == "Completed"))
                .GroupBy(b => new { b.UserId, b.User.FirstName, b.User.LastName, b.User.PhoneNumber })
                .Select(g => new
                {
                    UserId = g.Key.UserId,
                    FullName = g.Key.FirstName + " " + g.Key.LastName,
                    PhoneNumber = g.Key.PhoneNumber,
                    BookingCount = g.Count(),
                    Revenue = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(x => x.BookingCount)
                .Take(count)
                .ToListAsync<dynamic>();
        }

        public async Task<List<dynamic>> LayPhanHoiKhachHangGanDayAsync(int count)
        {
            // This would need a Contact or Feedback table
            // For now, return empty list as we don't have feedback system yet
            return new List<dynamic>();
        }

        /// <summary>
        /// Create booking using stored procedure sp_BookDynamicField
        /// Phiên bản mới: chỉ cần 11 params (không cần @EndTime, @VoucherCode, @Notes, v.v.)
        /// </summary>
        public async Task<BookingResult> TaoDatSanDynamicAsync(BookingCreateRequest request)
        {
            try
            {
                // SP mới chỉ cần: @FieldId, @BookingDate, @StartTime, @Duration, @UserId, @GuestName, @GuestPhone, @GuestEmail
                // Output: @TotalPrice, @BookingId, @OverlapError
                var parameters = new[]
                {
                    new SqlParameter("@FieldId", request.FieldId),
                    new SqlParameter("@BookingDate", request.BookingDate),
                    new SqlParameter("@StartTime", request.StartTime),
                    new SqlParameter("@Duration", request.Duration),
                    new SqlParameter("@UserId", request.UserId ?? (object)DBNull.Value),
                    new SqlParameter("@GuestName", request.GuestName ?? (object)DBNull.Value),
                    new SqlParameter("@GuestPhone", request.GuestPhone ?? (object)DBNull.Value),
                    new SqlParameter("@GuestEmail", request.GuestEmail ?? (object)DBNull.Value),
                    new SqlParameter("@TotalPrice", SqlDbType.Decimal) { Direction = ParameterDirection.Output, Precision = 10, Scale = 2 },
                    new SqlParameter("@BookingId", SqlDbType.Int) { Direction = ParameterDirection.Output },
                    new SqlParameter("@OverlapError", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output }
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_BookDynamicField @FieldId, @BookingDate, @StartTime, @Duration, @UserId, @GuestName, @GuestPhone, @GuestEmail, @TotalPrice OUTPUT, @BookingId OUTPUT, @OverlapError OUTPUT",
                    parameters);

                var overlapError = parameters.First(p => p.ParameterName == "@OverlapError").Value?.ToString();
                
                // Nếu có OverlapError => fail
                if (!string.IsNullOrEmpty(overlapError))
                {
                    return new BookingResult
                    {
                        IsSuccess = false,
                        Message = overlapError
                    };
                }

                // Success
                var bookingId = parameters.First(p => p.ParameterName == "@BookingId").Value;
                var totalPrice = parameters.First(p => p.ParameterName == "@TotalPrice").Value;

                if (bookingId != DBNull.Value && totalPrice != DBNull.Value)
                {
                    return new BookingResult
                    {
                        IsSuccess = true,
                        BookingId = (int)bookingId,
                        BookingCode = $"BK{request.BookingDate:yyyyMMdd}", // Simplified, SP generates actual code
                        TotalPrice = (decimal)totalPrice,
                        VoucherDiscount = 0, // SP mới không hỗ trợ voucher
                        FinalPrice = (decimal)totalPrice,
                        Message = "Đặt sân thành công!"
                    };
                }
                else
                {
                    return new BookingResult
                    {
                        IsSuccess = false,
                        Message = "Lỗi: Không tạo được booking"
                    };
                }
            }
            catch (Exception ex)
            {
                return new BookingResult
                {
                    IsSuccess = false,
                    Message = "Lỗi tạo đặt sân: " + ex.Message
                };
            }
        }

        #endregion
    }

    /// <summary>
    /// Request class for creating booking
    /// </summary>
    public class BookingCreateRequest
    {
        public int FieldId { get; set; }
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Duration { get; set; }
        public int? UserId { get; set; }
        public string? GuestName { get; set; }
        public string? GuestPhone { get; set; }
        public string? GuestEmail { get; set; }
        public string? VoucherCode { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Result class for booking creation
    /// </summary>
    public class BookingResult
    {
        public bool IsSuccess { get; set; }
        public int? BookingId { get; set; }
        public string? BookingCode { get; set; }
        public decimal? TotalPrice { get; set; }
        public decimal? VoucherDiscount { get; set; }
        public decimal? FinalPrice { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
