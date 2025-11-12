namespace EventEase.API.DTOs;

/// <summary>
/// Lightweight event response optimized for mobile apps
/// </summary>
public class MobileEventResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? TimeZone { get; set; }

    // Location (simplified)
    public string? Location { get; set; }
    public string? Venue { get; set; }
    public bool IsVirtual { get; set; }
    public string? VirtualMeetingUrl { get; set; }

    // Coordinates for map display
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // Capacity
    public int? MaxAttendees { get; set; }
    public int RegisteredCount { get; set; }
    public int AvailableSpots { get; set; }

    // Pricing
    public decimal? Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public bool IsFree { get; set; }

    // Media (optimized URLs)
    public string? ThumbnailUrl { get; set; }
    public string? ImageUrl { get; set; }

    // Registration info
    public bool RequiresApproval { get; set; }
    public bool IsRegistrationOpen { get; set; }
    public DateTime? RegistrationClosesAt { get; set; }

    // User-specific data
    public bool IsUserRegistered { get; set; }
    public Guid? UserRegistrationId { get; set; }
}

/// <summary>
/// Mobile event list item (even more lightweight)
/// </summary>
public class MobileEventListItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string? Venue { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int RegisteredCount { get; set; }
    public int AvailableSpots { get; set; }
    public bool IsFree { get; set; }
    public decimal? Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public bool IsUserRegistered { get; set; }
}
