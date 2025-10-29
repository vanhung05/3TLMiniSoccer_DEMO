using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using Microsoft.EntityFrameworkCore;

namespace _3TLMiniSoccer.Services
{
    public class SessionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SessionService> _logger;

        public SessionService(ApplicationDbContext context, ILogger<SessionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Check-in cho booking (simplified version for AdminController)
        public async Task<bool> CheckInAsync(int bookingId, string? notes = null)
        {
            // Tìm admin user đầu tiên
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 1);
            if (adminUser == null)
            {
                return false;
            }
            
            var result = await CheckInAsync(bookingId, adminUser.UserId, notes);
            return result.Success;
        }

        // Check-in cho booking (original version)
        public async Task<(bool Success, string Message, BookingSession? Session)> CheckInAsync(int bookingId, int userId, string? notes = null)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Field)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                if (booking == null)
                {
                    return (false, "Không tìm thấy booking", null);
                }

                if (booking.Status == "Cancelled")
                {
                    return (false, "Booking đã bị hủy", null);
                }

                // Kiểm tra xem đã check-in chưa
                var existingSession = await _context.BookingSessions
                    .FirstOrDefaultAsync(s => s.BookingId == bookingId && s.CheckInTime != null);

                if (existingSession != null)
                {
                    return (false, "Booking đã được check-in", existingSession);
                }

                // Tạo session mới
                var session = new BookingSession
                {
                    BookingId = bookingId,
                    CheckInTime = DateTime.Now,
                    StaffId = userId, // Sử dụng userId làm StaffId
                    Notes = notes,
                    Status = BookingSessionStatus.ACTIVE,
                    CreatedAt = DateTime.Now
                };

                _context.BookingSessions.Add(session);

                // Cập nhật trạng thái booking
                booking.Status = "Confirmed";
                booking.ConfirmedAt = DateTime.Now;
                booking.ConfirmedBy = userId; // Sử dụng userId
                booking.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Check-in thành công cho Booking {bookingId} bởi User {userId}");

                return (true, "Check-in thành công", session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi check-in Booking {bookingId}");
                return (false, $"Lỗi: {ex.Message}", null);
            }
        }

        // Check-out cho session
        public async Task<(bool Success, string Message, decimal FinalAmount)> CheckOutAsync(int sessionId, int staffId, string? notes = null)
        {
            try
            {
                var session = await _context.BookingSessions
                    .Include(s => s.Booking)
                    .Include(s => s.SessionOrders)
                        .ThenInclude(so => so.SessionOrderItems)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                {
                    return (false, "Không tìm thấy session", 0);
                }

                if (session.CheckOutTime != null)
                {
                    return (false, "Session đã được check-out", 0);
                }

                // Cập nhật check-out time
                session.CheckOutTime = DateTime.Now;
                session.Status = BookingSessionStatus.COMPLETED;
                if (!string.IsNullOrEmpty(notes))
                {
                    session.Notes = string.IsNullOrEmpty(session.Notes) ? notes : $"{session.Notes}; {notes}";
                }

                // Tính toán hóa đơn tổng kết
                var finalAmount = CalculateFinalBill(session);

                // Cập nhật trạng thái booking
                session.Booking.Status = "Completed";
                session.Booking.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Check-out thành công cho Session {sessionId}, Tổng tiền: {finalAmount}");

                return (true, "Check-out thành công", finalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi check-out Session {sessionId}");
                return (false, $"Lỗi: {ex.Message}", 0);
            }
        }

        // Thêm sản phẩm vào session
        public async Task<(bool Success, string Message, SessionOrder? Order)> AddProductsAsync(
            int sessionId, 
            List<(int ProductId, int Quantity)> items, 
            string paymentType = SessionOrderPaymentType.CONSOLIDATED)
        {
            try
            {
                var session = await _context.BookingSessions
                    .Include(s => s.Booking)
                    .Include(s => s.SessionOrders)
                        .ThenInclude(so => so.SessionOrderItems)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                {
                    return (false, "Không tìm thấy session", null);
                }

                if (session.CheckOutTime != null)
                {
                    return (false, "Session đã check-out, không thể thêm sản phẩm", null);
                }

                // Lấy thông tin sản phẩm
                var productIds = items.Select(i => i.ProductId).ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.ProductId) && p.IsAvailable)
                    .ToListAsync();

                if (products.Count != items.Count)
                {
                    return (false, "Một số sản phẩm không tồn tại hoặc không khả dụng", null);
                }

                // Tạo SessionOrder
                var sessionOrder = new SessionOrder
                {
                    SessionId = sessionId,
                    PaymentType = paymentType,
                    PaymentStatus = paymentType == SessionOrderPaymentType.IMMEDIATE 
                        ? SessionOrderPaymentStatus.PENDING 
                        : SessionOrderPaymentStatus.PENDING,
                    CreatedAt = DateTime.Now
                };

                // Tạo SessionOrderItems
                decimal totalAmount = 0;
                foreach (var item in items)
                {
                    var product = products.First(p => p.ProductId == item.ProductId);
                    var itemTotal = product.Price * item.Quantity;
                    totalAmount += itemTotal;

                    sessionOrder.SessionOrderItems.Add(new SessionOrderItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = itemTotal
                    });
                }

                sessionOrder.TotalAmount = totalAmount;

                _context.SessionOrders.Add(sessionOrder);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Thêm {items.Count} sản phẩm vào Session {sessionId}, Tổng: {totalAmount}");

                return (true, "Thêm sản phẩm thành công", sessionOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi thêm sản phẩm vào Session {sessionId}");
                return (false, $"Lỗi: {ex.Message}", null);
            }
        }

        // Thanh toán cho SessionOrder (thanh toán ngay)
        public async Task<(bool Success, string Message)> PaySessionOrderAsync(int sessionOrderId)
        {
            try
            {
                var sessionOrder = await _context.SessionOrders
                    .FirstOrDefaultAsync(so => so.SessionOrderId == sessionOrderId);

                if (sessionOrder == null)
                {
                    return (false, "Không tìm thấy đơn hàng");
                }

                if (sessionOrder.PaymentStatus == SessionOrderPaymentStatus.PAID)
                {
                    return (false, "Đơn hàng đã được thanh toán");
                }

                sessionOrder.PaymentStatus = SessionOrderPaymentStatus.PAID;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Thanh toán SessionOrder {sessionOrderId} thành công");

                return (true, "Thanh toán thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi thanh toán SessionOrder {sessionOrderId}");
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        // Lấy thông tin session
        public async Task<BookingSession?> GetSessionByIdAsync(int sessionId)
        {
            return await _context.BookingSessions
                .Include(s => s.Booking)
                    .ThenInclude(b => b.Field)
                .Include(s => s.Booking)
                    .ThenInclude(b => b.User)
                .Include(s => s.Staff)
                .Include(s => s.SessionOrders)
                    .ThenInclude(so => so.SessionOrderItems)
                        .ThenInclude(soi => soi.Product)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        }

        // Lấy session theo booking
        public async Task<BookingSession?> GetSessionByBookingIdAsync(int bookingId)
        {
            return await _context.BookingSessions
                .Include(s => s.Booking)
                    .ThenInclude(b => b.Field)
                .Include(s => s.Booking)
                    .ThenInclude(b => b.User)
                .Include(s => s.Staff)
                .Include(s => s.SessionOrders)
                    .ThenInclude(so => so.SessionOrderItems)
                        .ThenInclude(soi => soi.Product)
                .FirstOrDefaultAsync(s => s.BookingId == bookingId);
        }

        // Lấy danh sách session đang active
        public async Task<List<BookingSession>> GetActiveSessionsAsync()
        {
            return await _context.BookingSessions
                .Include(s => s.Booking)
                    .ThenInclude(b => b.Field)
                .Include(s => s.Booking)
                    .ThenInclude(b => b.User)
                .Where(s => s.Status == BookingSessionStatus.ACTIVE && s.CheckOutTime == null)
                .OrderByDescending(s => s.CheckInTime)
                .ToListAsync();
        }

        // Tính toán hóa đơn tổng kết (KHÔNG CÓ OVERTIME)
        private decimal CalculateFinalBill(BookingSession session)
        {
            var fieldPrice = session.Booking.TotalPrice;
            var isFieldPaid = session.Booking.PaymentStatus == "Paid";
            
            // Tính tổng tiền đã thanh toán ngay
            var immediatePayments = session.SessionOrders
                .Where(so => so.PaymentType == SessionOrderPaymentType.IMMEDIATE && 
                            so.PaymentStatus == SessionOrderPaymentStatus.PAID)
                .Sum(so => so.TotalAmount);
            
            // Tính tổng tiền gộp chung
            var consolidatedItems = session.SessionOrders
                .Where(so => so.PaymentType == SessionOrderPaymentType.CONSOLIDATED)
                .Sum(so => so.TotalAmount);
            
            // Không tính phí overtime theo yêu cầu
            return isFieldPaid 
                ? consolidatedItems - immediatePayments
                : fieldPrice + consolidatedItems - immediatePayments;
        }

        // Lấy chi tiết hóa đơn
        public async Task<(decimal FieldPrice, decimal ProductsTotal, decimal ImmediatePaid, decimal FinalAmount)> GetBillDetailsAsync(int sessionId)
        {
            var session = await GetSessionByIdAsync(sessionId);
            
            if (session == null)
            {
                return (0, 0, 0, 0);
            }

            var fieldPrice = session.Booking.TotalPrice;
            var isFieldPaid = session.Booking.PaymentStatus == "Paid";
            
            var immediatePayments = session.SessionOrders
                .Where(so => so.PaymentType == SessionOrderPaymentType.IMMEDIATE && 
                            so.PaymentStatus == SessionOrderPaymentStatus.PAID)
                .Sum(so => so.TotalAmount);
            
            var consolidatedItems = session.SessionOrders
                .Where(so => so.PaymentType == SessionOrderPaymentType.CONSOLIDATED)
                .Sum(so => so.TotalAmount);

            var productsTotal = session.SessionOrders.Sum(so => so.TotalAmount);
            
            var finalAmount = isFieldPaid 
                ? consolidatedItems - immediatePayments
                : fieldPrice + consolidatedItems - immediatePayments;

            return (
                isFieldPaid ? 0 : fieldPrice,
                productsTotal,
                immediatePayments,
                finalAmount
            );
        }

        // Additional methods for AdminController
        public async Task<List<BookingSession>> LayTatCaSessionAsync()
        {
            return await _context.BookingSessions
                .Include(s => s.Booking)
                    .ThenInclude(b => b.User)
                .Include(s => s.Booking)
                    .ThenInclude(b => b.Field)
                        .ThenInclude(f => f.FieldType)
                .Include(s => s.SessionOrders)
                    .ThenInclude(so => so.SessionOrderItems)
                        .ThenInclude(soi => soi.Product)
                .OrderByDescending(s => s.CheckInTime)
                .ToListAsync();
        }

        public async Task<BookingSession?> LaySessionTheoIdAsync(int sessionId)
        {
            return await _context.BookingSessions
                .Include(s => s.Booking)
                    .ThenInclude(b => b.User)
                .Include(s => s.Booking)
                    .ThenInclude(b => b.Field)
                        .ThenInclude(f => f.FieldType)
                .Include(s => s.SessionOrders)
                    .ThenInclude(so => so.SessionOrderItems)
                        .ThenInclude(soi => soi.Product)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        }

        public async Task<bool> CheckOutAsync(int sessionId)
        {
            // Tìm admin user đầu tiên
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 1);
            if (adminUser == null)
            {
                return false;
            }
            
            var result = await CheckOutAsync(sessionId, adminUser.UserId, null);
            return result.Success;
        }

        public async Task<bool> ThemSanPhamVaoSessionAsync(int sessionId, int productId, int quantity)
        {
            try
            {
                var session = await _context.BookingSessions
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null || session.Status != BookingSessionStatus.ACTIVE)
                {
                    return false;
                }

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == productId);

                if (product == null || !product.IsAvailable)
                {
                    return false;
                }

                var items = new List<(int ProductId, int Quantity)>
                {
                    (productId, quantity)
                };

                var result = await AddProductsAsync(sessionId, items, SessionOrderPaymentType.CONSOLIDATED);
                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi thêm sản phẩm vào Session {sessionId}");
                return false;
            }
        }

        public async Task<bool> XoaSanPhamKhoiSessionAsync(int orderItemId)
        {
            try
            {
                var orderItem = await _context.SessionOrderItems
                    .Include(oi => oi.SessionOrder)
                    .FirstOrDefaultAsync(oi => oi.SessionOrderItemId == orderItemId);

                if (orderItem == null)
                {
                    return false;
                }

                var sessionOrder = orderItem.SessionOrder;
                
                // Xóa order item
                _context.SessionOrderItems.Remove(orderItem);

                // Cập nhật tổng tiền của session order
                sessionOrder.TotalAmount = sessionOrder.SessionOrderItems
                    .Where(oi => oi.SessionOrderItemId != orderItemId)
                    .Sum(oi => oi.TotalPrice);

                // Nếu không còn item nào, xóa session order
                if (sessionOrder.SessionOrderItems.Count <= 1)
                {
                    _context.SessionOrders.Remove(sessionOrder);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xóa sản phẩm khỏi Session {orderItemId}");
                return false;
            }
        }

        public async Task<List<Product>> LaySanPhamCoSanAsync()
        {
            return await _context.Products
                .Where(p => p.IsAvailable)
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }
    }
}

