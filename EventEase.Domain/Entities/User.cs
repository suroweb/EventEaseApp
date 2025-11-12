using EventEase.Domain.Common;
using EventEase.Domain.Enums;

namespace EventEase.Domain.Entities;

/// <summary>
/// Represents a user in the system
/// </summary>
public class User : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    // Identity
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PasswordSalt { get; set; }

    // Profile
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }

    // Role
    public UserRole Role { get; set; } = UserRole.User;

    // Status
    public bool IsActive { get; set; } = true;
    public bool EmailConfirmed { get; set; } = false;
    public DateTime? EmailConfirmedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Security
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiresAt { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }

    // Preferences
    public string? PreferredLanguage { get; set; } = "en";
    public string? TimeZone { get; set; }

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
    public virtual ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
    public virtual ICollection<AIAgentUsage> AIAgentUsages { get; set; } = new List<AIAgentUsage>();
}
