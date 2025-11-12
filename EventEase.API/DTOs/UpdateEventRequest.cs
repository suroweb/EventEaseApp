using System.ComponentModel.DataAnnotations;

namespace EventEase.API.DTOs;

/// <summary>
/// Request model for updating an existing event
/// </summary>
public class UpdateEventRequest
{
    [Required(ErrorMessage = "Event name is required")]
    [StringLength(300, ErrorMessage = "Event name cannot exceed 300 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(5000)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [StringLength(100)]
    public string? TimeZone { get; set; }

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

    public bool IsVirtual { get; set; }

    [Url]
    [StringLength(500)]
    public string? VirtualMeetingUrl { get; set; }

    [Range(1, 100000)]
    public int? MaxAttendees { get; set; }

    [Range(0, 999999.99)]
    public decimal? Price { get; set; }

    [StringLength(3)]
    public string Currency { get; set; } = "EUR";

    public bool IsFree { get; set; }

    [Url]
    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [Url]
    [StringLength(500)]
    public string? BannerUrl { get; set; }

    public bool RequiresApproval { get; set; }

    public DateTime? RegistrationOpensAt { get; set; }

    public DateTime? RegistrationClosesAt { get; set; }

    public bool AllowWaitlist { get; set; }

    public List<string>? Tags { get; set; }

    public Dictionary<string, string>? CustomFields { get; set; }
}
