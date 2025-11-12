using System.ComponentModel.DataAnnotations;
using EventEase.Domain.Enums;

namespace EventEase.API.DTOs;

/// <summary>
/// Request model for creating a new event
/// </summary>
public class CreateEventRequest
{
    [Required(ErrorMessage = "Event name is required")]
    [StringLength(300, ErrorMessage = "Event name cannot exceed 300 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }

    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }

    [StringLength(100)]
    public string? TimeZone { get; set; }

    // Location
    [StringLength(200)]
    public string? Location { get; set; }

    [StringLength(200)]
    public string? Venue { get; set; }

    [StringLength(500)]
    public string? VenueAddress { get; set; }

    [StringLength(100)]
    public string? VenueCity { get; set; }

    [StringLength(100)]
    public string? VenueCountry { get; set; }

    [Range(-90, 90)]
    public decimal? VenueLatitude { get; set; }

    [Range(-180, 180)]
    public decimal? VenueLongitude { get; set; }

    public bool IsVirtual { get; set; } = false;

    [Url]
    [StringLength(500)]
    public string? VirtualMeetingUrl { get; set; }

    // Capacity & Pricing
    [Range(1, 100000, ErrorMessage = "Max attendees must be between 1 and 100,000")]
    public int? MaxAttendees { get; set; }

    [Range(0, 999999.99)]
    public decimal? Price { get; set; }

    [StringLength(3)]
    public string Currency { get; set; } = "EUR";

    public bool IsFree { get; set; } = true;

    // Media
    [Url]
    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [Url]
    [StringLength(500)]
    public string? BannerUrl { get; set; }

    // Registration Settings
    public bool RequiresApproval { get; set; } = false;

    public DateTime? RegistrationOpensAt { get; set; }

    public DateTime? RegistrationClosesAt { get; set; }

    public bool AllowWaitlist { get; set; } = true;

    // Metadata
    public List<string>? Tags { get; set; }

    public Dictionary<string, string>? CustomFields { get; set; }
}
