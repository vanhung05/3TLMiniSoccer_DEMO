using _3TLMiniSoccer.Models;

namespace _3TLMiniSoccer.ViewModels
{
    public class FieldDetailsViewModel
    {
        public Field Field { get; set; } = new Field();
        public List<Booking> BookedTimeSlots { get; set; } = new List<Booking>();
        public List<Booking> RecentBookings { get; set; } = new List<Booking>();
        public BookingViewModel BookingForm { get; set; } = new BookingViewModel();
    }
}
