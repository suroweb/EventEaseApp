using EventEase.Domain.Common;
using EventEase.Domain.Enums;

namespace EventEase.Domain.Entities;

/// <summary>
/// Represents a registration for an event
/// </summary>
public class EventRegistration : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid? UserId { get; set; } // Null if external guest

    // Attendee Information
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Company { get; set; }
    public string? JobTitle { get; set; }

    // Registration Details
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;
    public int NumberOfTickets { get; set; } = 1;
    public decimal TotalAmount { get; set; } = 0;
    public string? SpecialRequests { get; set; }
    public string? DietaryRestrictions { get; set; }

    // Timestamps
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? CheckedInAt { get; set; }

    // Source
    public Guid? InvitationId { get; set; } // If registered via invitation
    public string? RegistrationSource { get; set; } // "web", "email", "invitation", etc.

    // Metadata
    public string? CustomData { get; set; } // JSON object for custom fields

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Event Event { get; set; } = null!;
    public virtual User? User { get; set; }
    public virtual Invitation? Invitation { get; set; }
}
