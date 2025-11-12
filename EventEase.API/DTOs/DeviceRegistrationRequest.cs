namespace EventEase.API.DTOs;

/// <summary>
/// Device registration request for push notifications
/// </summary>
public class DeviceRegistrationRequest
{
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // "ios", "android", "web"
    public string? DeviceModel { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
    public string? Language { get; set; }
    public string? TimeZone { get; set; }
    public List<string> NotificationCategories { get; set; } = new(); // ["events", "registrations", "reminders"]
}

/// <summary>
/// Device registration response
/// </summary>
public class DeviceRegistrationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? DeviceId { get; set; }
    public DateTime RegisteredAt { get; set; }
}

/// <summary>
/// Push notification request
/// </summary>
public class PushNotificationRequest
{
    public List<Guid>? UserIds { get; set; }
    public List<Guid>? DeviceIds { get; set; }
    public string? Platform { get; set; } // Optional filter by platform
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
    public string? Category { get; set; }
    public int? Badge { get; set; }
    public string? Sound { get; set; }
    public DateTime? ScheduledAt { get; set; }
}

/// <summary>
/// Push notification response
/// </summary>
public class PushNotificationResponse
{
    public bool Success { get; set; }
    public int TotalRecipients { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Badge count update request
/// </summary>
public class BadgeCountRequest
{
    public int Count { get; set; }
    public string? DeviceToken { get; set; }
}

/// <summary>
/// Update notification preferences request
/// </summary>
public class NotificationPreferencesRequest
{
    public bool EventReminders { get; set; } = true;
    public bool RegistrationUpdates { get; set; } = true;
    public bool EventUpdates { get; set; } = true;
    public bool NewInvitations { get; set; } = true;
    public bool MarketingNotifications { get; set; } = false;
    public int ReminderHoursBefore { get; set; } = 24; // Hours before event to send reminder
}

/// <summary>
/// Notification preferences response
/// </summary>
public class NotificationPreferencesResponse
{
    public bool Success { get; set; }
    public NotificationPreferencesRequest Preferences { get; set; } = new();
}
