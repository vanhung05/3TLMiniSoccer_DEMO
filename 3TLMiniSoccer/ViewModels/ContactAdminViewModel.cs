using _3TLMiniSoccer.Models;

namespace _3TLMiniSoccer.ViewModels
{
    public class ContactAdminViewModel
    {
        public List<Contact> Contacts { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Type { get; set; }
        public DateTime? CreatedDate { get; set; }
        public ContactStatistics Statistics { get; set; } = new();
    }

    public class ContactStatistics
    {
        public int TotalContacts { get; set; }
        public int NewContacts { get; set; }
        public int ReadContacts { get; set; }
        public int RepliedContacts { get; set; }
        public int ClosedContacts { get; set; }
    }
}

