using Microsoft.AspNetCore.Mvc;
using _3TLMiniSoccer.Services;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using Microsoft.EntityFrameworkCore;

namespace _3TLMiniSoccer.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationHubService _notificationHubService;

        public ContactController(ApplicationDbContext context, NotificationHubService notificationHubService)
        {
            _context = context;
            _notificationHubService = notificationHubService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string name, string phone, string email, string subject, string message, bool newsletter)
        {
            try
            {
                // Tạo contact mới
                var contact = new Contact
                {
                    Name = name,
                    Email = email,
                    Phone = phone,
                    Subject = subject,
                    Message = message,
                    Status = "New",
                    CreatedAt = DateTime.Now
                };

                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();

                // Gửi thông báo realtime cho admin
                try
                {
                    await _notificationHubService.guiTBLienHeMoiAsync(
                        contact.ContactId,
                        contact.Name,
                        contact.Subject
                    );
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the contact submission
                    System.Diagnostics.Debug.WriteLine($"Error sending notification: {ex.Message}");
                }

                return Json(new { success = true, message = "Tin nhắn đã được gửi thành công!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating contact: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi tin nhắn!" });
            }
        }
    }
}
