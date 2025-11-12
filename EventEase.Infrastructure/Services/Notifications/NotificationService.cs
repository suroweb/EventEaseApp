using EventEase.Application.Interfaces;
using EventEase.Domain.Entities;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventEase.Infrastructure.Services.Notifications;

/// <summary>
/// Unified notification service supporting email, SMS, push, and in-app notifications
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IRealtimeService _realtimeService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext context,
        IEmailService emailService,
        IRealtimeService realtimeService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public async Task<NotificationResult> SendNotificationAsync(
        Guid userId,
        string title,
        string message,
        NotificationType type,
        Dictionary<string, string>? data = null)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new NotificationResult
                {
                    Success = false,
                    ErrorMessage = "User not found",
                    SentAt = DateTime.UtcNow
                };
            }

            // Get user preferences
            var preferences = await GetUserPreferencesAsync(userId);

            // Create notification record
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = user.TenantId,
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                DataJson = data != null ? JsonSerializer.Serialize(data) : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var result = new NotificationResult
            {
                Success = true,
                NotificationId = notification.Id,
                SentAt = DateTime.UtcNow
            };

            // Send through enabled channels
            if (preferences.PushEnabled)
            {
                var pushResult = await SendPushNotificationAsync(userId, title, message, data);
                if (pushResult.Success)
                {
                    result.DeliveredChannels.Add(DeliveryChannel.Push);
                    notification.PushSent = true;
                    notification.PushSentAt = DateTime.UtcNow;
                }
                else
                {
                    result.FailedChannels.Add(DeliveryChannel.Push);
                }
            }

            // Send via real-time (in-app)
            try
            {
                await _realtimeService.SendNotificationToUserAsync(userId, new
                {
                    Id = notification.Id,
                    Title = title,
                    Message = message,
                    Type = type.ToString(),
                    Data = data,
                    Timestamp = notification.CreatedAt
                });
                result.DeliveredChannels.Add(DeliveryChannel.InApp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send in-app notification");
                result.FailedChannels.Add(DeliveryChannel.InApp);
            }

            // Send email if enabled and appropriate for notification type
            if (preferences.EmailEnabled && ShouldSendEmail(type, preferences))
            {
                var emailResult = await _emailService.SendEmailAsync(
                    user.Email,
                    user.FullName ?? user.Email,
                    title,
                    $"<html><body><h2>{title}</h2><p>{message}</p></body></html>",
                    message);

                if (emailResult.Success)
                {
                    result.DeliveredChannels.Add(DeliveryChannel.Email);
                    notification.EmailSent = true;
                    notification.EmailSentAt = DateTime.UtcNow;
                }
                else
                {
                    result.FailedChannels.Add(DeliveryChannel.Email);
                }
            }

            // Send SMS if enabled (placeholder for Twilio integration)
            if (preferences.SmsEnabled && !string.IsNullOrEmpty(user.PhoneNumber) && ShouldSendSms(type))
            {
                var smsResult = await SendSmsAsync(user.PhoneNumber, message);
                if (smsResult.Success)
                {
                    result.DeliveredChannels.Add(DeliveryChannel.Sms);
                    notification.SmsSent = true;
                    notification.SmsSentAt = DateTime.UtcNow;
                }
                else
                {
                    result.FailedChannels.Add(DeliveryChannel.Sms);
                }
            }

            await _context.SaveChangesAsync();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
            return new NotificationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }

    public async Task<List<NotificationResult>> SendBulkNotificationAsync(
        List<Guid> userIds,
        string title,
        string message,
        NotificationType type)
    {
        var results = new List<NotificationResult>();

        foreach (var userId in userIds)
        {
            var result = await SendNotificationAsync(userId, title, message, type);
            results.Add(result);
        }

        return results;
    }

    public async Task<NotificationResult> SendPushNotificationAsync(
        Guid userId,
        string title,
        string body,
        Dictionary<string, string>? data = null)
    {
        try
        {
            _logger.LogInformation("Sending push notification to user {UserId}", userId);

            /*
             * Production Implementation with Firebase Cloud Messaging (FCM) or OneSignal:
             *
             * using FirebaseAdmin.Messaging;
             *
             * var message = new Message
             * {
             *     Token = userDeviceToken,
             *     Notification = new Notification
             *     {
             *         Title = title,
             *         Body = body
             *     },
             *     Data = data
             * };
             *
             * var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
             *
             * return new NotificationResult
             * {
             *     Success = true,
             *     NotificationId = Guid.Parse(response),
             *     SentAt = DateTime.UtcNow
             * };
             */

            // Simulated implementation
            _logger.LogInformation("Push notification sent successfully");

            return new NotificationResult
            {
                Success = true,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification");
            return new NotificationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }

    public async Task<NotificationResult> SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            _logger.LogInformation("Sending SMS to {PhoneNumber}", phoneNumber);

            /*
             * Production Implementation with Twilio:
             *
             * using Twilio;
             * using Twilio.Rest.Api.V2010.Account;
             * using Twilio.Types;
             *
             * TwilioClient.Init(accountSid, authToken);
             *
             * var messageResource = await MessageResource.CreateAsync(
             *     body: message,
             *     from: new PhoneNumber(twilioPhoneNumber),
             *     to: new PhoneNumber(phoneNumber)
             * );
             *
             * return new NotificationResult
             * {
             *     Success = true,
             *     NotificationId = Guid.Parse(messageResource.Sid),
             *     SentAt = DateTime.UtcNow
             * };
             */

            // Simulated implementation
            _logger.LogInformation("SMS sent successfully");

            return new NotificationResult
            {
                Success = true,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS");
            return new NotificationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }

    public async Task<NotificationPreferences> GetUserPreferencesAsync(Guid userId)
    {
        // In production, store preferences in database
        // For now, return default preferences

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return new NotificationPreferences();
        }

        // TODO: Load from database UserNotificationPreferences table
        return new NotificationPreferences
        {
            EmailEnabled = true,
            SmsEnabled = false,
            PushEnabled = true,
            EventReminders = true,
            EventUpdates = true,
            EventCancellations = true,
            RegistrationConfirmations = true,
            RegistrationUpdates = true,
            PaymentReceipts = true,
            PaymentFailures = true,
            SystemUpdates = false,
            MarketingEmails = false,
            UsageReports = true,
            Reminder24Hours = true,
            Reminder1Hour = true,
            Reminder15Minutes = false
        };
    }

    public async Task<bool> UpdateUserPreferencesAsync(
        Guid userId,
        NotificationPreferences preferences)
    {
        try
        {
            // TODO: Save to database UserNotificationPreferences table
            _logger.LogInformation("Updated notification preferences for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences");
            return false;
        }
    }

    public async Task<List<NotificationHistory>> GetNotificationHistoryAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationHistory
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                Channels = GetDeliveredChannels(n),
                IsRead = n.IsRead,
                SentAt = n.CreatedAt,
                ReadAt = n.ReadAt,
                Data = n.DataJson != null
                    ? JsonSerializer.Deserialize<Dictionary<string, string>>(n.DataJson)
                    : null
            })
            .ToListAsync();

        return notifications;
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId)
    {
        try
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
            {
                return false;
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Notify real-time clients
            await _realtimeService.SendNotificationToUserAsync(notification.UserId, new
            {
                Type = "NotificationRead",
                NotificationId = notificationId
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return false;
        }
    }

    public async Task<bool> MarkAllAsReadAsync(Guid userId)
    {
        try
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Notify real-time clients
            await _realtimeService.SendNotificationToUserAsync(userId, new
            {
                Type = "AllNotificationsRead"
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return false;
        }
    }

    public async Task<bool> DeleteNotificationAsync(Guid notificationId)
    {
        try
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
            {
                return false;
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification");
            return false;
        }
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();
    }

    private bool ShouldSendEmail(NotificationType type, NotificationPreferences preferences)
    {
        return type switch
        {
            NotificationType.EventReminder => preferences.EventReminders,
            NotificationType.EventUpdate => preferences.EventUpdates,
            NotificationType.EventCancellation => preferences.EventCancellations,
            NotificationType.RegistrationConfirmation => preferences.RegistrationConfirmations,
            NotificationType.PaymentReceipt => preferences.PaymentReceipts,
            NotificationType.PaymentFailed => preferences.PaymentFailures,
            NotificationType.SystemAlert => preferences.SystemUpdates,
            _ => true
        };
    }

    private bool ShouldSendSms(NotificationType type)
    {
        // Only send SMS for critical notifications
        return type == NotificationType.EventReminder ||
               type == NotificationType.EventCancellation ||
               type == NotificationType.PaymentFailed ||
               type == NotificationType.SystemAlert;
    }

    private List<DeliveryChannel> GetDeliveredChannels(Notification notification)
    {
        var channels = new List<DeliveryChannel>();

        if (notification.EmailSent) channels.Add(DeliveryChannel.Email);
        if (notification.SmsSent) channels.Add(DeliveryChannel.Sms);
        if (notification.PushSent) channels.Add(DeliveryChannel.Push);

        return channels;
    }
}
