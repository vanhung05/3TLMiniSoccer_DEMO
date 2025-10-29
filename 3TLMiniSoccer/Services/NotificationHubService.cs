using Microsoft.AspNetCore.SignalR;
using _3TLMiniSoccer.Hubs;
using _3TLMiniSoccer.Services;
using System.Xml.Linq;
using System.Reflection;

namespace _3TLMiniSoccer.Services
{
    public class NotificationHubService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationHubService> _logger;
        public NotificationHubService(IHubContext<NotificationHub> hubContext, ILogger<NotificationHubService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task guiTBDatSanMoiAsync(int bookingId, string customerName, string fieldName, DateTime bookingDate, TimeSpan startTime)
        {
            try
            {
                var thongBao = new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "new_booking",
                    title = "Đặt Sân Mới",
                    message = $"Khách hàng {customerName} đã đặt sân {fieldName} vào ngày {bookingDate:dd/MM/yyyy} lúc {startTime:hh\\:mm}.",
                    bookingId = bookingId,
                    timestamp = DateTime.Now,
                    icon = "calendar-plus",
                    isRead = false
                };

                await _hubContext.Clients.Groups("AdminGroup").SendAsync("ReceiveAdminNotification", thongBao);
                _logger.LogInformation("Sent new booking notification {bookingId}: ", bookingId);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending new booking notification for booking {BookingId}", bookingId);
            }
        }

        public async Task guiTBDonHangMoiAsync(int orderId, string customerName, decimal totalAmount)
        {
            try
            {
                var thongBao = new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "new_order",
                    title = "Đơn hàng mới",
                    message = $"Khách hàng {customerName} vừa đặt đơn hàng {orderId} với tổng giá tiền: {totalAmount:NO} VNĐ",
                    orderId = orderId,
                    timestamp = DateTime.Now,
                    icon = "shopping-cart",
                    isRead = false
                };

                await _hubContext.Clients.Groups("AdminGroup").SendAsync("ReceiveAdminNotification", thongBao);
                _logger.LogInformation("Sent new order notification for order {OrderId}", orderId);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending new order notification for order {OrderId}", orderId);
            }
        }

        public async Task guiTBLienHeMoiAsync(int contactId, string customerName, string subject)
        {
            try
            {
                var thongBao = new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "new_contact",
                    title = "Tin nhắn liên hệ mới",
                    message = $"{customerName} đã gửi tin nhắn: {subject}",
                    contactId = contactId,
                    timestamp = DateTime.Now,
                    icon = "message-circle",
                    isRead = false
                };

                await _hubContext.Clients.Groups("AdminGroup").SendAsync("ReceiveAdminNotification", thongBao);
                _logger.LogInformation("Sent contact message notification for contact {ContactId}", contactId);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending contact message notification for contact {ContactId}", contactId);
            }
        }
    }
}
