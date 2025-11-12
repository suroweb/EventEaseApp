namespace EventEase.Application.Interfaces;

/// <summary>
/// Service for managing real-time communication using SignalR
/// </summary>
public interface IRealtimeService
{
    // ===== Event Updates =====

    /// <summary>
    /// Notify all clients in a tenant about an event update
    /// </summary>
    Task NotifyEventUpdatedAsync(Guid tenantId, Guid eventId, object eventData);

    /// <summary>
    /// Notify all clients about a new registration for an event
    /// </summary>
    Task NotifyEventRegistrationAsync(Guid tenantId, Guid eventId, Guid registrationId, object registrationData);

    /// <summary>
    /// Update live attendee count for an event
    /// </summary>
    Task UpdateAttendeeCountAsync(Guid tenantId, Guid eventId, int count);

    /// <summary>
    /// Notify about event status change (e.g., Draft -> Published -> Ongoing -> Completed)
    /// </summary>
    Task NotifyEventStatusChangedAsync(Guid tenantId, Guid eventId, string status);

    /// <summary>
    /// Send live poll results to event participants
    /// </summary>
    Task SendPollResultsAsync(Guid tenantId, Guid eventId, Guid pollId, object pollResults);

    // ===== Notifications =====

    /// <summary>
    /// Send notification to a specific user
    /// </summary>
    Task SendNotificationToUserAsync(Guid userId, object notification);

    /// <summary>
    /// Send notification to all users in a tenant
    /// </summary>
    Task SendNotificationToTenantAsync(Guid tenantId, object notification);

    /// <summary>
    /// Send notification to all event participants
    /// </summary>
    Task SendNotificationToEventParticipantsAsync(Guid tenantId, Guid eventId, object notification);

    /// <summary>
    /// Send notification to a specific role within a tenant
    /// </summary>
    Task SendNotificationToRoleAsync(Guid tenantId, string role, object notification);

    // ===== Analytics =====

    /// <summary>
    /// Update live analytics dashboard for a tenant
    /// </summary>
    Task UpdateAnalyticsDashboardAsync(Guid tenantId, object analyticsData);

    /// <summary>
    /// Send real-time metric update
    /// </summary>
    Task UpdateMetricAsync(Guid tenantId, string metricName, object metricValue);

    /// <summary>
    /// Broadcast revenue update
    /// </summary>
    Task UpdateRevenueAsync(Guid tenantId, decimal totalRevenue, decimal monthlyRevenue);

    /// <summary>
    /// Update event performance metrics in real-time
    /// </summary>
    Task UpdateEventPerformanceAsync(Guid tenantId, Guid eventId, object performanceData);

    // ===== Chat =====

    /// <summary>
    /// Send chat message to event participants
    /// </summary>
    Task SendChatMessageAsync(Guid tenantId, Guid eventId, Guid userId, string userName, string message);

    /// <summary>
    /// Notify about user joining/leaving event chat
    /// </summary>
    Task NotifyUserPresenceAsync(Guid tenantId, Guid eventId, Guid userId, string userName, bool isOnline);

    /// <summary>
    /// Send typing indicator
    /// </summary>
    Task SendTypingIndicatorAsync(Guid tenantId, Guid eventId, Guid userId, string userName, bool isTyping);

    // ===== Connection Management =====

    /// <summary>
    /// Add user to a SignalR group
    /// </summary>
    Task AddToGroupAsync(string connectionId, string groupName);

    /// <summary>
    /// Remove user from a SignalR group
    /// </summary>
    Task RemoveFromGroupAsync(string connectionId, string groupName);

    /// <summary>
    /// Get active connection count for a tenant
    /// </summary>
    Task<int> GetActiveConnectionCountAsync(Guid tenantId);
}
