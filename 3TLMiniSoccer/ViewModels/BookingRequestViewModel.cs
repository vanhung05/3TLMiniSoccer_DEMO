namespace _3TLMiniSoccer.ViewModels
{
    public class CreateBookingRequest
    {
        public int UserId { get; set; }
        public string? GuestName { get; set; }
        public string? GuestPhone { get; set; }
        public string? GuestEmail { get; set; }
        public int FieldId { get; set; }
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; } = "Pending";
        public string? Notes { get; set; }
    }

    public class UpdateBookingRequest
    {
        public int UserId { get; set; }
        public string? GuestName { get; set; }
        public string? GuestPhone { get; set; }
        public string? GuestEmail { get; set; }
        public int FieldId { get; set; }
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; } = "Pending";
        public string? Notes { get; set; }
    }
}

