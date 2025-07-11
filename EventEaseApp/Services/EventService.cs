using EventEaseApp.Models;

namespace EventEaseApp.Services
{
    public class EventService
    {
        private readonly List<Event> _events;

        public EventService()
        {
            _events = new List<Event>
            {
                new Event
                {
                    Id = 1,
                    Name = "Tech Innovation Summit 2024",
                    Description = "Join industry leaders for a day of insights into the latest technology trends, AI advancements, and digital transformation strategies.",
                    Date = DateTime.Now.AddDays(30),
                    Location = "San Francisco, CA",
                    Venue = "Moscone Center",
                    Price = 299.99m,
                    MaxAttendees = 500,
                    ImageUrl = "https://via.placeholder.com/400x200?text=Tech+Innovation+Summit",
                    Category = "Technology",
                    IsAvailable = true
                },
                new Event
                {
                    Id = 2,
                    Name = "Annual Corporate Gala",
                    Description = "An elegant evening of networking, fine dining, and entertainment. Perfect for building business relationships in a luxurious setting.",
                    Date = DateTime.Now.AddDays(45),
                    Location = "New York, NY",
                    Venue = "The Plaza Hotel",
                    Price = 450.00m,
                    MaxAttendees = 300,
                    ImageUrl = "https://via.placeholder.com/400x200?text=Corporate+Gala",
                    Category = "Corporate",
                    IsAvailable = true
                },
                new Event
                {
                    Id = 3,
                    Name = "Marketing Masterclass Workshop",
                    Description = "Intensive workshop covering digital marketing strategies, social media optimization, and content creation for modern businesses.",
                    Date = DateTime.Now.AddDays(15),
                    Location = "Chicago, IL",
                    Venue = "Marketing Hub Center",
                    Price = 199.99m,
                    MaxAttendees = 100,
                    ImageUrl = "https://via.placeholder.com/400x200?text=Marketing+Workshop",
                    Category = "Education",
                    IsAvailable = true
                },
                new Event
                {
                    Id = 4,
                    Name = "Green Business Conference",
                    Description = "Explore sustainable business practices, environmental technologies, and corporate social responsibility strategies.",
                    Date = DateTime.Now.AddDays(60),
                    Location = "Seattle, WA",
                    Venue = "Washington State Convention Center",
                    Price = 175.00m,
                    MaxAttendees = 400,
                    ImageUrl = "https://via.placeholder.com/400x200?text=Green+Business",
                    Category = "Environment",
                    IsAvailable = true
                },
                new Event
                {
                    Id = 5,
                    Name = "Startup Pitch Night",
                    Description = "Watch innovative startups pitch their ideas to investors. Network with entrepreneurs and venture capitalists.",
                    Date = DateTime.Now.AddDays(20),
                    Location = "Austin, TX",
                    Venue = "Capital Factory",
                    Price = 50.00m,
                    MaxAttendees = 200,
                    ImageUrl = "https://via.placeholder.com/400x200?text=Startup+Pitch",
                    Category = "Entrepreneurship",
                    IsAvailable = true
                },
                new Event
                {
                    Id = 6,
                    Name = "Healthcare Innovation Symposium",
                    Description = "Discover the latest advancements in medical technology, telemedicine, and healthcare management systems.",
                    Date = DateTime.Now.AddDays(40),
                    Location = "Boston, MA",
                    Venue = "Boston Convention Center",
                    Price = 325.00m,
                    MaxAttendees = 350,
                    ImageUrl = "https://via.placeholder.com/400x200?text=Healthcare+Innovation",
                    Category = "Healthcare",
                    IsAvailable = true
                },
                new Event
                {
                    Id = 7,
                    Name = "Finance & Investment Forum",
                    Description = "Expert panels on investment strategies, market analysis, and financial planning for businesses and individuals.",
                    Date = DateTime.Now.AddDays(25),
                    Location = "Miami, FL",
                    Venue = "Miami Beach Convention Center",
                    Price = 275.00m,
                    MaxAttendees = 250,
                    ImageUrl = "https://via.placeholder.com/400x200?text=Finance+Forum",
                    Category = "Finance",
                    IsAvailable = true
                },
                new Event
                {
                    Id = 8,
                    Name = "Team Building Adventure Day",
                    Description = "Strengthen team bonds through outdoor activities, challenges, and collaborative exercises designed for corporate groups.",
                    Date = DateTime.Now.AddDays(10),
                    Location = "Denver, CO",
                    Venue = "Rocky Mountain Adventure Park",
                    Price = 125.00m,
                    MaxAttendees = 50,
                    ImageUrl = "https://via.placeholder.com/400x200?text=Team+Building",
                    Category = "Corporate",
                    IsAvailable = true
                },
                new Event
                {
                    Id = 9,
                    Name = "AI & Machine Learning Summit",
                    Description = "Deep dive into artificial intelligence applications, machine learning algorithms, and their business implementations.",
                    Date = DateTime.Now.AddDays(35),
                    Location = "San Jose, CA",
                    Venue = "Silicon Valley Conference Center",
                    Price = 399.99m,
                    MaxAttendees = 600,
                    ImageUrl = "https://via.placeholder.com/400x200?text=AI+Summit",
                    Category = "Technology",
                    IsAvailable = true
                },
                new Event
                {
                    Id = 10,
                    Name = "Leadership Excellence Workshop",
                    Description = "Develop essential leadership skills, emotional intelligence, and strategic thinking for modern business challenges.",
                    Date = DateTime.Now.AddDays(18),
                    Location = "Atlanta, GA",
                    Venue = "Executive Leadership Center",
                    Price = 225.00m,
                    MaxAttendees = 80,
                    ImageUrl = "https://via.placeholder.com/400x200?text=Leadership+Workshop",
                    Category = "Education",
                    IsAvailable = true
                }
            };
        }

        public List<Event> GetAllEvents()
        {
            return _events.OrderBy(e => e.Date).ToList();
        }

        public Event? GetEventById(int id)
        {
            return _events.FirstOrDefault(e => e.Id == id);
        }

        public List<Event> GetUpcomingEvents(int count = 5)
        {
            return _events
                .Where(e => e.Date > DateTime.Now && e.IsAvailable)
                .OrderBy(e => e.Date)
                .Take(count)
                .ToList();
        }

        public List<Event> GetEventsByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return new List<Event>();
            }

            return _events
                .Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.Date)
                .ToList();
        }
    }
}