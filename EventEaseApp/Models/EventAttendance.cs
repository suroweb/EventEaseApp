namespace EventEaseApp.Models
{
    public class EventAttendance
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public List<Registration> Registrations { get; set; } = new();
        public int TotalRegistrations => Registrations.Sum(r => r.NumberOfTickets);
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}