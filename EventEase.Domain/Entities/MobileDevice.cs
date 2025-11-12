using EventEase.Domain.Common;

namespace EventEase.Domain.Entities;

/// <summary>
/// Represents a mobile device registered for push notifications
/// </summary>
public class MobileDevice : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }

    // Device Information
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // "ios", "android", "web"
    public string? DeviceModel { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }

    // Localization
    public string? Language { get; set; }
    public string? TimeZone { get; set; }

    // Status
    public bool IsActive { get; set; } = true;
    public int BadgeCount { get; set; } = 0;
    public DateTime LastSeenAt { get; set; }

    // Notification Preferences
    public bool EventReminders { get; set; } = true;
    public bool RegistrationUpdates { get; set; } = true;
    public bool EventUpdates { get; set; } = true;
    public bool NewInvitations { get; set; } = true;
    public bool MarketingNotifications { get; set; } = false;

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
