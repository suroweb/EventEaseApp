using EventEase.Domain.Enums;

namespace EventEase.Application.Interfaces;

/// <summary>
/// Unified notification service supporting email, SMS, and push notifications
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send notification through all enabled channels for a user
    /// </summary>
    Task<NotificationResult> SendNotificationAsync(
        Guid userId,
        string title,
        string message,
        NotificationType type,
        Dictionary<string, string>? data = null);

    /// <summary>
    /// Send notification to multiple users
    /// </summary>
    Task<List<NotificationResult>> SendBulkNotificationAsync(
        List<Guid> userIds,
        string title,
        string message,
        NotificationType type);

    /// <summary>
    /// Send push notification
    /// </summary>
    Task<NotificationResult> SendPushNotificationAsync(
        Guid userId,
        string title,
        string body,
        Dictionary<string, string>? data = null);

    /// <summary>
    /// Send SMS notification
    /// </summary>
    Task<NotificationResult> SendSmsAsync(
        string phoneNumber,
        string message);

    /// <summary>
    /// Get user notification preferences
    /// </summary>
    Task<NotificationPreferences> GetUserPreferencesAsync(Guid userId);

    /// <summary>
    /// Update user notification preferences
    /// </summary>
    Task<bool> UpdateUserPreferencesAsync(
        Guid userId,
        NotificationPreferences preferences);

    /// <summary>
    /// Get notification history for a user
    /// </summary>
    Task<List<NotificationHistory>> GetNotificationHistoryAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Mark notification as read
    /// </summary>
    Task<bool> MarkAsReadAsync(Guid notificationId);

    /// <summary>
    /// Mark all notifications as read for a user
    /// </summary>
    Task<bool> MarkAllAsReadAsync(Guid userId);

    /// <summary>
    /// Delete notification
    /// </summary>
    Task<bool> DeleteNotificationAsync(Guid notificationId);

    /// <summary>
    /// Get unread notification count
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId);
}

public class NotificationResult
{
    public bool Success { get; set; }
    public Guid? NotificationId { get; set; }
    public List<DeliveryChannel> DeliveredChannels { get; set; } = new();
    public List<DeliveryChannel> FailedChannels { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; }
}

public class NotificationPreferences
{
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    public bool PushEnabled { get; set; } = true;

    // Event notifications
    public bool EventReminders { get; set; } = true;
    public bool EventUpdates { get; set; } = true;
    public bool EventCancellations { get; set; } = true;

    // Registration notifications
    public bool RegistrationConfirmations { get; set; } = true;
    public bool RegistrationUpdates { get; set; } = true;

    // Payment notifications
    public bool PaymentReceipts { get; set; } = true;
    public bool PaymentFailures { get; set; } = true;

    // System notifications
    public bool SystemUpdates { get; set; } = false;
    public bool MarketingEmails { get; set; } = false;
    public bool UsageReports { get; set; } = true;

    // Reminder timing
    public bool Reminder24Hours { get; set; } = true;
    public bool Reminder1Hour { get; set; } = true;
    public bool Reminder15Minutes { get; set; } = false;
}

public class NotificationHistory
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public NotificationType Type { get; set; }
    public List<DeliveryChannel> Channels { get; set; } = new();
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public Dictionary<string, string>? Data { get; set; }
}

public enum DeliveryChannel
{
    Email,
    Sms,
    Push,
    InApp
}
