namespace EventEase.API.DTOs;

/// <summary>
/// Response model for event details
/// </summary>
public class EventResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string Status { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? TimeZone { get; set; }

    public string? Location { get; set; }
    public string? Venue { get; set; }
    public string? VenueAddress { get; set; }
    public string? VenueCity { get; set; }
    public string? VenueCountry { get; set; }
    public decimal? VenueLatitude { get; set; }
    public decimal? VenueLongitude { get; set; }
    public bool IsVirtual { get; set; }
    public string? VirtualMeetingUrl { get; set; }

    public int? MaxAttendees { get; set; }
    public int CurrentAttendees { get; set; }
    public int AvailableSeats => MaxAttendees.HasValue ? MaxAttendees.Value - CurrentAttendees : int.MaxValue;
    public bool IsFull => MaxAttendees.HasValue && CurrentAttendees >= MaxAttendees.Value;

    public decimal? Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public bool IsFree { get; set; }

    public string? ImageUrl { get; set; }
    public string? BannerUrl { get; set; }

    public bool IsAIGenerated { get; set; }
    public string? AIGenerationPrompt { get; set; }

    public bool RequiresApproval { get; set; }
    public DateTime? RegistrationOpensAt { get; set; }
    public DateTime? RegistrationClosesAt { get; set; }
    public bool AllowWaitlist { get; set; }
    public bool IsRegistrationOpen { get; set; }

    public List<string>? Tags { get; set; }
    public Dictionary<string, string>? CustomFields { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
