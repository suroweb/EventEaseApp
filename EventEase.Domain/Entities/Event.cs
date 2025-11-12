using EventEase.Domain.Common;
using EventEase.Domain.Enums;

namespace EventEase.Domain.Entities;

/// <summary>
/// Represents an event created by a tenant
/// </summary>
public class Event : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CreatedByUserId { get; set; }

    // Basic Information
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Draft;

    // Date & Time
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? TimeZone { get; set; }

    // Location
    public string? Location { get; set; }
    public string? Venue { get; set; }
    public string? VenueAddress { get; set; }
    public string? VenueCity { get; set; }
    public string? VenueCountry { get; set; }
    public decimal? VenueLatitude { get; set; }
    public decimal? VenueLongitude { get; set; }
    public bool IsVirtual { get; set; } = false;
    public string? VirtualMeetingUrl { get; set; }

    // Capacity & Pricing
    public int? MaxAttendees { get; set; }
    public decimal? Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public bool IsFree { get; set; } = true;

    // Media
    public string? ImageUrl { get; set; }
    public string? BannerUrl { get; set; }

    // AI Generated Content
    public bool IsAIGenerated { get; set; } = false;
    public string? AIGenerationPrompt { get; set; }
    public Guid? AIAgentUsageId { get; set; }

    // Registration Settings
    public bool RequiresApproval { get; set; } = false;
    public DateTime? RegistrationOpensAt { get; set; }
    public DateTime? RegistrationClosesAt { get; set; }
    public bool AllowWaitlist { get; set; } = true;

    // Metadata
    public string? Tags { get; set; } // JSON array
    public string? CustomFields { get; set; } // JSON object

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
    public virtual Budget? Budget { get; set; }
}
