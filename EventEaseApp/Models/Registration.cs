using System.ComponentModel.DataAnnotations;

namespace EventEaseApp.Models
{
    public class Registration
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name must be less than 50 characters")]
        public string FirstName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name must be less than 50 characters")]
        public string LastName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100, ErrorMessage = "Email must be less than 100 characters")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20, ErrorMessage = "Phone number must be less than 20 characters")]
        public string Phone { get; set; } = string.Empty;
        
        [StringLength(100, ErrorMessage = "Company name must be less than 100 characters")]
        public string Company { get; set; } = string.Empty;
        
        [Range(1, 5, ErrorMessage = "Number of tickets must be between 1 and 5")]
        public int NumberOfTickets { get; set; } = 1;
        
        [StringLength(500, ErrorMessage = "Special requests must be less than 500 characters")]
        public string SpecialRequests { get; set; } = string.Empty;
        
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Confirmed";
    }
}