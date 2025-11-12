using EventEase.Domain.Common;

namespace EventEase.Domain.Entities;

/// <summary>
/// Represents a guest/contact for invitation purposes
/// </summary>
public class Guest : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    // Basic Information
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }

    // Professional Details
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public string? Industry { get; set; }
    public string? Department { get; set; }

    // Location
    public string? City { get; set; }
    public string? Country { get; set; }

    // ML Features for Smart Guest Selection
    public string? Interests { get; set; } // JSON array
    public string? PreviousEventCategories { get; set; } // JSON array
    public int TotalEventsAttended { get; set; } = 0;
    public int TotalInvitationsSent { get; set; } = 0;
    public int TotalInvitationsAccepted { get; set; } = 0;
    public decimal EngagementScore { get; set; } = 0; // ML-calculated score
    public DateTime? LastInvitationSentAt { get; set; }
    public DateTime? LastEventAttendedAt { get; set; }

    // Preferences
    public bool OptedOutOfMarketing { get; set; } = false;
    public string? PreferredLanguage { get; set; }
    public string? TimeZone { get; set; }

    // Source
    public string? Source { get; set; } // "manual", "import", "crm", "registration"
    public string? ExternalCrmId { get; set; } // ID from integrated CRM

    // Tags & Metadata
    public string? Tags { get; set; } // JSON array
    public string? CustomFields { get; set; } // JSON object
    public string? Notes { get; set; }

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
}
