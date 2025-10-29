using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using _3TLMiniSoccer.ViewModels;

namespace _3TLMiniSoccer.Services
{
    public class ContactAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContactAdminService> _logger;

        public ContactAdminService(ApplicationDbContext context, ILogger<ContactAdminService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ContactAdminViewModel> LayDanhSachLienHeAsync(int page = 1, int pageSize = 10, string? search = null, string? status = null, string? type = null, DateTime? createdDate = null)
        {
            try
            {
                var query = _context.Contacts.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c => c.Name.Contains(search) || c.Email.Contains(search) || c.Message.Contains(search));
                }

                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    query = query.Where(c => c.Status == status);
                }

                if (!string.IsNullOrEmpty(type) && type != "all")
                {
                    // Map type to subject patterns
                    switch (type.ToLower())
                    {
                        case "booking":
                            query = query.Where(c => c.Subject.Contains("đặt sân") || c.Subject.Contains("booking"));
                            break;
                        case "complaint":
                            query = query.Where(c => c.Subject.Contains("khiếu nại") || c.Subject.Contains("complaint"));
                            break;
                        case "suggestion":
                            query = query.Where(c => c.Subject.Contains("góp ý") || c.Subject.Contains("suggestion"));
                            break;
                        case "other":
                            query = query.Where(c => !c.Subject.Contains("đặt sân") && !c.Subject.Contains("khiếu nại") && !c.Subject.Contains("góp ý"));
                            break;
                    }
                }

                if (createdDate.HasValue)
                {
                    var startDate = createdDate.Value.Date;
                    var endDate = startDate.AddDays(1);
                    query = query.Where(c => c.CreatedAt >= startDate && c.CreatedAt < endDate);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var contacts = await query
                    .Include(c => c.RespondedByUser)
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Get statistics
                var statistics = await LayThongKeLienHeAsync();

                return new ContactAdminViewModel
                {
                    Contacts = contacts,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Search = search,
                    Status = status,
                    Type = type,
                    CreatedDate = createdDate,
                    Statistics = statistics
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contacts");
                return new ContactAdminViewModel();
            }
        }

        public async Task<Contact?> LayLienHeTheoIdAsync(int id)
        {
            try
            {
                return await _context.Contacts
                    .Include(c => c.RespondedByUser)
                    .FirstOrDefaultAsync(c => c.ContactId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contact by id {Id}", id);
                return null;
            }
        }

        public async Task<bool> CapNhatTrangThaiLienHeAsync(int id, string status)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(id);
                if (contact == null) return false;

                contact.Status = status;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact status {Id}", id);
                return false;
            }
        }

        public async Task<bool> TraLoiLienHeAsync(int id, string response, int respondedBy)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(id);
                if (contact == null) return false;

                contact.Response = response;
                contact.RespondedAt = DateTime.Now;
                contact.RespondedBy = respondedBy;
                contact.Status = "Replied";
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replying to contact {Id}", id);
                return false;
            }
        }

        public async Task<bool> XoaLienHeAsync(int id)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(id);
                if (contact == null) return false;

                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact {Id}", id);
                return false;
            }
        }

        public async Task<bool> DanhDauDaDocAsync(int id)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(id);
                if (contact == null) return false;

                if (contact.Status == "New")
                {
                    contact.Status = "Read";
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking contact as read {Id}", id);
                return false;
            }
        }

        public async Task<ContactStatistics> LayThongKeLienHeAsync()
        {
            try
            {
                var totalContacts = await _context.Contacts.CountAsync();
                var newContacts = await _context.Contacts.CountAsync(c => c.Status == "New");
                var readContacts = await _context.Contacts.CountAsync(c => c.Status == "Read");
                var repliedContacts = await _context.Contacts.CountAsync(c => c.Status == "Replied");
                var closedContacts = await _context.Contacts.CountAsync(c => c.Status == "Closed");

                return new ContactStatistics
                {
                    TotalContacts = totalContacts,
                    NewContacts = newContacts,
                    ReadContacts = readContacts,
                    RepliedContacts = repliedContacts,
                    ClosedContacts = closedContacts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contact statistics");
                return new ContactStatistics();
            }
        }

        public async Task<bool> KiemTraLienHeTonTaiAsync(int id)
        {
            try
            {
                return await _context.Contacts.AnyAsync(c => c.ContactId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if contact exists {Id}", id);
                return false;
            }
        }

        public async Task<List<Contact>> LayTatCaLienHeAsync()
        {
            return await _context.Contacts
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}
