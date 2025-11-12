namespace EventEase.API.DTOs;

/// <summary>
/// Response model for event registration
/// </summary>
public class RegistrationResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public Guid? UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Company { get; set; }
    public string? JobTitle { get; set; }

    public string Status { get; set; } = string.Empty;
    public int NumberOfTickets { get; set; }
    public decimal TotalAmount { get; set; }
    public string? SpecialRequests { get; set; }
    public string? DietaryRestrictions { get; set; }

    public DateTime RegisteredAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? CheckedInAt { get; set; }

    public Guid? InvitationId { get; set; }
    public string? RegistrationSource { get; set; }

    public Dictionary<string, string>? CustomData { get; set; }
}
