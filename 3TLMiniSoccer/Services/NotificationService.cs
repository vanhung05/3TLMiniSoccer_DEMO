using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;

namespace _3TLMiniSoccer.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Notification>> LayTatCaThongBaoAsync()
        {
            return await _context.Notifications
                .Include(n => n.User)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Notification>> LayThongBaoTheoNguoiDungAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Notification>> LayThongBaoChuaDocAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Notification?> LayThongBaoTheoIdAsync(int notificationId)
        {
            return await _context.Notifications
                .Include(n => n.User)
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId);
        }

        public async Task<Notification> TaoThongBaoAsync(Notification notification)
        {
            notification.CreatedAt = DateTime.Now;
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<bool> CapNhatThongBaoAsync(Notification notification)
        {
            try
            {
                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> XoaThongBaoAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                if (notification != null)
                {
                    _context.Notifications.Remove(notification);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DanhDauDaDocAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                if (notification != null)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DanhDauTatCaDaDocAsync(int userId)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> LaySoThongBaoChuaDocAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> GuiThongBaoDenNguoiDungAsync(int userId, string title, string content, string type)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Content = content,
                    Type = type,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> GuiThongBaoDenTatCaNguoiDungAsync(string title, string content, string type)
        {
            try
            {
                var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
                var notifications = users.Select(user => new Notification
                {
                    UserId = user.UserId,
                    Title = title,
                    Content = content,
                    Type = type,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                }).ToList();

                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> GuiThongBaoDatSanAsync(int bookingId, string type)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.User)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                if (booking?.User != null)
                {
                    string title = type switch
                    {
                        "Confirmed" => "Đặt sân đã được xác nhận",
                        "Cancelled" => "Đặt sân đã bị hủy",
                        "Reminder" => "Nhắc nhở đặt sân",
                        _ => "Thông báo đặt sân"
                    };

                    string content = type switch
                    {
                        "Confirmed" => $"Đặt sân {booking.BookingCode} đã được xác nhận thành công.",
                        "Cancelled" => $"Đặt sân {booking.BookingCode} đã bị hủy.",
                        "Reminder" => $"Nhắc nhở: Bạn có đặt sân {booking.BookingCode} sắp tới.",
                        _ => $"Thông báo về đặt sân {booking.BookingCode}."
                    };

                    return await GuiThongBaoDenNguoiDungAsync(booking.UserId ?? 0, title, content, "Booking");
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> GuiThongBaoThanhToanAsync(int transactionId, string type)
        {
            try
            {
                var transaction = await _context.Transactions
                    .Include(t => t.Booking)
                    .ThenInclude(b => b.User)
                    .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

                if (transaction?.Booking?.User != null)
                {
                    string title = type switch
                    {
                        "Success" => "Thanh toán thành công",
                        "Failed" => "Thanh toán thất bại",
                        "Refunded" => "Hoàn tiền thành công",
                        _ => "Thông báo thanh toán"
                    };

                    string content = type switch
                    {
                        "Success" => $"Thanh toán {transaction.Amount:C} cho đặt sân {transaction.Booking.BookingCode} đã thành công.",
                        "Failed" => $"Thanh toán {transaction.Amount:C} cho đặt sân {transaction.Booking.BookingCode} đã thất bại.",
                        "Refunded" => $"Hoàn tiền {transaction.Amount:C} cho đặt sân {transaction.Booking.BookingCode} đã thành công.",
                        _ => $"Thông báo về thanh toán {transaction.Amount:C} cho đặt sân {transaction.Booking.BookingCode}."
                    };

                    return await GuiThongBaoDenNguoiDungAsync(transaction.Booking.UserId ?? 0, title, content, "Payment");
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
