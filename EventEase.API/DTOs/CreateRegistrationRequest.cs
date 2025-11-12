using System.ComponentModel.DataAnnotations;

namespace EventEase.API.DTOs;

/// <summary>
/// Request model for creating an event registration
/// </summary>
public class CreateRegistrationRequest
{
    [Required(ErrorMessage = "Event ID is required")]
    public Guid EventId { get; set; }

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(200)]
    public string? Company { get; set; }

    [StringLength(100)]
    public string? JobTitle { get; set; }

    [Range(1, 10, ErrorMessage = "Number of tickets must be between 1 and 10")]
    public int NumberOfTickets { get; set; } = 1;

    [StringLength(500)]
    public string? SpecialRequests { get; set; }

    [StringLength(500)]
    public string? DietaryRestrictions { get; set; }

    public Dictionary<string, string>? CustomData { get; set; }
}
