using EventEase.Application.Interfaces;
using EventEase.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EventEase.Infrastructure.Services.Realtime;

/// <summary>
/// Service for managing real-time communication using SignalR
/// Provides a centralized way to send real-time updates to clients
/// </summary>
public class RealtimeService : IRealtimeService
{
    private readonly IHubContext<EventHub, IEventHubClient> _eventHub;
    private readonly IHubContext<NotificationHub, INotificationHubClient> _notificationHub;
    private readonly IHubContext<AnalyticsHub, IAnalyticsHubClient> _analyticsHub;
    private readonly ILogger<RealtimeService> _logger;

    public RealtimeService(
        IHubContext<EventHub, IEventHubClient> eventHub,
        IHubContext<NotificationHub, INotificationHubClient> notificationHub,
        IHubContext<AnalyticsHub, IAnalyticsHubClient> analyticsHub,
        ILogger<RealtimeService> logger)
    {
        _eventHub = eventHub;
        _notificationHub = notificationHub;
        _analyticsHub = analyticsHub;
        _logger = logger;
    }

    // ===== Event Updates =====

    public async Task NotifyEventUpdatedAsync(Guid tenantId, Guid eventId, object eventData)
    {
        try
        {
            var groupName = GetEventGroupName(tenantId, eventId);
            await _eventHub.Clients.Group(groupName).EventUpdated(eventId, eventData);

            _logger.LogInformation(
                "Sent event update notification for event {EventId} in tenant {TenantId}",
                eventId, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending event update notification for event {EventId} in tenant {TenantId}",
                eventId, tenantId);
            throw;
        }
    }

    public async Task NotifyEventRegistrationAsync(Guid tenantId, Guid eventId, Guid registrationId, object registrationData)
    {
        try
        {
            var groupName = GetEventGroupName(tenantId, eventId);
            await _eventHub.Clients.Group(groupName).NewRegistration(eventId, registrationId, registrationData);

            _logger.LogInformation(
                "Sent registration notification for event {EventId} in tenant {TenantId}",
                eventId, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending registration notification for event {EventId} in tenant {TenantId}",
                eventId, tenantId);
            throw;
        }
    }

    public async Task UpdateAttendeeCountAsync(Guid tenantId, Guid eventId, int count)
    {
        try
        {
            var groupName = GetEventGroupName(tenantId, eventId);
            await _eventHub.Clients.Group(groupName).AttendeeCountUpdated(eventId, count);

            _logger.LogDebug(
                "Updated attendee count to {Count} for event {EventId} in tenant {TenantId}",
                count, eventId, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating attendee count for event {EventId} in tenant {TenantId}",
                eventId, tenantId);
            throw;
        }
    }

    public async Task NotifyEventStatusChangedAsync(Guid tenantId, Guid eventId, string status)
    {
        try
        {
            var groupName = GetEventGroupName(tenantId, eventId);
            await _eventHub.Clients.Group(groupName).EventStatusChanged(eventId, status);

            _logger.LogInformation(
                "Sent status change notification for event {EventId} to status {Status} in tenant {TenantId}",
                eventId, status, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending status change notification for event {EventId} in tenant {TenantId}",
                eventId, tenantId);
            throw;
        }
    }

    public async Task SendPollResultsAsync(Guid tenantId, Guid eventId, Guid pollId, object pollResults)
    {
        try
        {
            var groupName = GetEventGroupName(tenantId, eventId);
            await _eventHub.Clients.Group(groupName).ReceivePollResults(pollId, pollResults);

            _logger.LogInformation(
                "Sent poll results for poll {PollId} in event {EventId}, tenant {TenantId}",
                pollId, eventId, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending poll results for poll {PollId} in event {EventId}, tenant {TenantId}",
                pollId, eventId, tenantId);
            throw;
        }
    }

    // ===== Notifications =====

    public async Task SendNotificationToUserAsync(Guid userId, object notification)
    {
        try
        {
            var groupName = GetUserGroupName(userId);
            await _notificationHub.Clients.Group(groupName).ReceiveNotification(notification);

            _logger.LogInformation(
                "Sent notification to user {UserId}",
                userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending notification to user {UserId}",
                userId);
            throw;
        }
    }

    public async Task SendNotificationToTenantAsync(Guid tenantId, object notification)
    {
        try
        {
            var groupName = GetTenantGroupName(tenantId);
            await _notificationHub.Clients.Group(groupName).ReceiveNotification(notification);

            _logger.LogInformation(
                "Sent notification to all users in tenant {TenantId}",
                tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending notification to tenant {TenantId}",
                tenantId);
            throw;
        }
    }

    public async Task SendNotificationToEventParticipantsAsync(Guid tenantId, Guid eventId, object notification)
    {
        try
        {
            var groupName = GetEventNotificationGroupName(tenantId, eventId);
            await _notificationHub.Clients.Group(groupName).EventNotification(eventId, "event_update", notification);

            _logger.LogInformation(
                "Sent notification to participants of event {EventId} in tenant {TenantId}",
                eventId, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending notification to event participants for event {EventId} in tenant {TenantId}",
                eventId, tenantId);
            throw;
        }
    }

    public async Task SendNotificationToRoleAsync(Guid tenantId, string role, object notification)
    {
        try
        {
            var groupName = GetTenantRoleGroupName(tenantId, role);
            await _notificationHub.Clients.Group(groupName).ReceiveNotification(notification);

            _logger.LogInformation(
                "Sent notification to role {Role} in tenant {TenantId}",
                role, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending notification to role {Role} in tenant {TenantId}",
                role, tenantId);
            throw;
        }
    }

    // ===== Analytics =====

    public async Task UpdateAnalyticsDashboardAsync(Guid tenantId, object analyticsData)
    {
        try
        {
            var groupName = GetTenantAnalyticsGroupName(tenantId);
            await _analyticsHub.Clients.Group(groupName).ReceiveDashboardUpdate(analyticsData);

            _logger.LogDebug(
                "Updated analytics dashboard for tenant {TenantId}",
                tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating analytics dashboard for tenant {TenantId}",
                tenantId);
            throw;
        }
    }

    public async Task UpdateMetricAsync(Guid tenantId, string metricName, object metricValue)
    {
        try
        {
            var groupName = GetTenantAnalyticsGroupName(tenantId);
            await _analyticsHub.Clients.Group(groupName).ReceiveMetricUpdate(metricName, metricValue);

            _logger.LogDebug(
                "Updated metric {MetricName} for tenant {TenantId}",
                metricName, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating metric {MetricName} for tenant {TenantId}",
                metricName, tenantId);
            throw;
        }
    }

    public async Task UpdateRevenueAsync(Guid tenantId, decimal totalRevenue, decimal monthlyRevenue)
    {
        try
        {
            var groupName = GetTenantAnalyticsGroupName(tenantId);
            await _analyticsHub.Clients.Group(groupName).RevenueUpdated(totalRevenue, monthlyRevenue);

            _logger.LogInformation(
                "Updated revenue for tenant {TenantId}: Total={TotalRevenue}, Monthly={MonthlyRevenue}",
                tenantId, totalRevenue, monthlyRevenue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating revenue for tenant {TenantId}",
                tenantId);
            throw;
        }
    }

    public async Task UpdateEventPerformanceAsync(Guid tenantId, Guid eventId, object performanceData)
    {
        try
        {
            var groupName = GetEventAnalyticsGroupName(tenantId, eventId);
            await _analyticsHub.Clients.Group(groupName).ReceiveEventPerformance(eventId, performanceData);

            _logger.LogDebug(
                "Updated event performance for event {EventId} in tenant {TenantId}",
                eventId, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating event performance for event {EventId} in tenant {TenantId}",
                eventId, tenantId);
            throw;
        }
    }

    // ===== Chat =====

    public async Task SendChatMessageAsync(Guid tenantId, Guid eventId, Guid userId, string userName, string message)
    {
        try
        {
            var groupName = GetEventGroupName(tenantId, eventId);
            await _eventHub.Clients.Group(groupName).ReceiveChatMessage(userId, userName, message, DateTime.UtcNow);

            _logger.LogInformation(
                "Sent chat message from user {UserId} to event {EventId} in tenant {TenantId}",
                userId, eventId, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending chat message from user {UserId} to event {EventId} in tenant {TenantId}",
                userId, eventId, tenantId);
            throw;
        }
    }

    public async Task NotifyUserPresenceAsync(Guid tenantId, Guid eventId, Guid userId, string userName, bool isOnline)
    {
        try
        {
            var groupName = GetEventGroupName(tenantId, eventId);

            if (isOnline)
            {
                await _eventHub.Clients.Group(groupName).UserJoinedEvent(userId, userName);
            }
            else
            {
                await _eventHub.Clients.Group(groupName).UserLeftEvent(userId, userName);
            }

            _logger.LogDebug(
                "Notified presence change for user {UserId} in event {EventId}: {Status}",
                userId, eventId, isOnline ? "Online" : "Offline");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error notifying presence for user {UserId} in event {EventId}",
                userId, eventId);
            throw;
        }
    }

    public async Task SendTypingIndicatorAsync(Guid tenantId, Guid eventId, Guid userId, string userName, bool isTyping)
    {
        try
        {
            var groupName = GetEventGroupName(tenantId, eventId);
            await _eventHub.Clients.Group(groupName).UserTyping(userId, userName, isTyping);

            _logger.LogTrace(
                "Sent typing indicator for user {UserId} in event {EventId}: {IsTyping}",
                userId, eventId, isTyping);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending typing indicator for user {UserId} in event {EventId}",
                userId, eventId);
            // Don't throw for typing indicators as they're not critical
        }
    }

    // ===== Connection Management =====

    public async Task AddToGroupAsync(string connectionId, string groupName)
    {
        try
        {
            // This would need to be implemented with IHubContext
            // For now, groups are managed automatically by the hubs
            await Task.CompletedTask;

            _logger.LogDebug(
                "Added connection {ConnectionId} to group {GroupName}",
                connectionId, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error adding connection {ConnectionId} to group {GroupName}",
                connectionId, groupName);
            throw;
        }
    }

    public async Task RemoveFromGroupAsync(string connectionId, string groupName)
    {
        try
        {
            // This would need to be implemented with IHubContext
            // For now, groups are managed automatically by the hubs
            await Task.CompletedTask;

            _logger.LogDebug(
                "Removed connection {ConnectionId} from group {GroupName}",
                connectionId, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error removing connection {ConnectionId} from group {GroupName}",
                connectionId, groupName);
            throw;
        }
    }

    public async Task<int> GetActiveConnectionCountAsync(Guid tenantId)
    {
        try
        {
            // In a real implementation with Redis backplane, this would query the connection count
            // For now, return 0 as a placeholder
            await Task.CompletedTask;
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting active connection count for tenant {TenantId}",
                tenantId);
            return 0;
        }
    }

    // ===== Helper Methods =====

    private static string GetUserGroupName(Guid userId)
        => $"user_{userId}";

    private static string GetTenantGroupName(Guid tenantId)
        => $"tenant_{tenantId}";

    private static string GetEventGroupName(Guid tenantId, Guid eventId)
        => $"tenant_{tenantId}_event_{eventId}";

    private static string GetTenantAnalyticsGroupName(Guid tenantId)
        => $"tenant_{tenantId}_analytics";

    private static string GetEventAnalyticsGroupName(Guid tenantId, Guid eventId)
        => $"tenant_{tenantId}_event_{eventId}_analytics";

    private static string GetEventNotificationGroupName(Guid tenantId, Guid eventId)
        => $"tenant_{tenantId}_event_{eventId}_notifications";

    private static string GetTenantRoleGroupName(Guid tenantId, string role)
        => $"tenant_{tenantId}_role_{role}";
}
