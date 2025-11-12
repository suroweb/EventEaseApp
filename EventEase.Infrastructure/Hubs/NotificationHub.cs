using EventEase.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EventEase.Infrastructure.Hubs;

/// <summary>
/// SignalR Hub for real-time notifications
/// Handles instant notifications to users, tenants, and roles
/// </summary>
[Authorize]
public class NotificationHub : Hub<INotificationHubClient>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ILogger<NotificationHub> _logger;

    // In-memory connection tracking (in production, use Redis or a distributed cache)
    private static readonly Dictionary<Guid, HashSet<string>> _userConnections = new();
    private static readonly object _lock = new();

    public NotificationHub(
        ICurrentUserService currentUserService,
        ICurrentTenantService currentTenantService,
        ILogger<NotificationHub> logger)
    {
        _currentUserService = currentUserService;
        _currentTenantService = currentTenantService;
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = _currentUserService.UserId;
        var tenantId = _currentTenantService.TenantId;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";
        var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "User";

        _logger.LogInformation(
            "User {UserId} ({UserName}) from tenant {TenantId} connected to NotificationHub with connection {ConnectionId}",
            userId, userName, tenantId, Context.ConnectionId);

        // Track user connections
        lock (_lock)
        {
            if (!_userConnections.ContainsKey(userId))
            {
                _userConnections[userId] = new HashSet<string>();
            }
            _userConnections[userId].Add(Context.ConnectionId);
        }

        // Add to user-specific group
        await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));

        // Add to tenant group
        if (tenantId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetTenantGroupName(tenantId.Value));
            await Groups.AddToGroupAsync(Context.ConnectionId, GetTenantRoleGroupName(tenantId.Value, userRole));
        }

        // Notify user they're connected
        await Clients.Caller.Connected(new
        {
            UserId = userId,
            TenantId = tenantId,
            ConnectedAt = DateTime.UtcNow
        });

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _currentUserService.UserId;
        var tenantId = _currentTenantService.TenantId;

        // Remove from connection tracking
        lock (_lock)
        {
            if (_userConnections.ContainsKey(userId))
            {
                _userConnections[userId].Remove(Context.ConnectionId);
                if (_userConnections[userId].Count == 0)
                {
                    _userConnections.Remove(userId);
                }
            }
        }

        if (exception != null)
        {
            _logger.LogWarning(exception,
                "User {UserId} from tenant {TenantId} disconnected from NotificationHub with error",
                userId, tenantId);
        }
        else
        {
            _logger.LogInformation(
                "User {UserId} from tenant {TenantId} disconnected from NotificationHub",
                userId, tenantId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    public async Task MarkNotificationAsReadAsync(Guid notificationId)
    {
        var userId = _currentUserService.UserId;

        _logger.LogInformation(
            "User {UserId} marked notification {NotificationId} as read",
            userId, notificationId);

        // In a real implementation, update the database
        // For now, just acknowledge
        await Clients.Caller.NotificationMarkedAsRead(notificationId);
    }

    /// <summary>
    /// Mark multiple notifications as read
    /// </summary>
    public async Task MarkAllNotificationsAsReadAsync()
    {
        var userId = _currentUserService.UserId;

        _logger.LogInformation(
            "User {UserId} marked all notifications as read",
            userId);

        // In a real implementation, update the database
        await Clients.Caller.AllNotificationsMarkedAsRead();
    }

    /// <summary>
    /// Delete a notification
    /// </summary>
    public async Task DeleteNotificationAsync(Guid notificationId)
    {
        var userId = _currentUserService.UserId;

        _logger.LogInformation(
            "User {UserId} deleted notification {NotificationId}",
            userId, notificationId);

        // In a real implementation, soft delete in the database
        await Clients.Caller.NotificationDeleted(notificationId);
    }

    /// <summary>
    /// Get unread notification count
    /// </summary>
    public async Task GetUnreadCountAsync()
    {
        var userId = _currentUserService.UserId;

        // In a real implementation, query the database
        var unreadCount = 0;

        await Clients.Caller.UnreadCountUpdated(unreadCount);
    }

    /// <summary>
    /// Subscribe to notifications for a specific event
    /// </summary>
    public async Task SubscribeToEventNotificationsAsync(Guid eventId)
    {
        var tenantId = _currentTenantService.TenantId;
        if (!tenantId.HasValue)
        {
            throw new HubException("Tenant context is required");
        }

        var groupName = GetEventNotificationGroupName(tenantId.Value, eventId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "User {UserId} subscribed to notifications for event {EventId}",
            _currentUserService.UserId, eventId);
    }

    /// <summary>
    /// Unsubscribe from notifications for a specific event
    /// </summary>
    public async Task UnsubscribeFromEventNotificationsAsync(Guid eventId)
    {
        var tenantId = _currentTenantService.TenantId;
        if (!tenantId.HasValue)
        {
            return;
        }

        var groupName = GetEventNotificationGroupName(tenantId.Value, eventId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "User {UserId} unsubscribed from notifications for event {EventId}",
            _currentUserService.UserId, eventId);
    }

    /// <summary>
    /// Get notification preferences
    /// </summary>
    public async Task GetNotificationPreferencesAsync()
    {
        var userId = _currentUserService.UserId;

        // In a real implementation, fetch from database
        var preferences = new
        {
            UserId = userId,
            EmailNotifications = true,
            PushNotifications = true,
            EventUpdates = true,
            RegistrationUpdates = true,
            PaymentUpdates = true,
            MarketingEmails = false
        };

        await Clients.Caller.ReceiveNotificationPreferences(preferences);
    }

    /// <summary>
    /// Update notification preferences
    /// </summary>
    public async Task UpdateNotificationPreferencesAsync(object preferences)
    {
        var userId = _currentUserService.UserId;

        _logger.LogInformation(
            "User {UserId} updated notification preferences",
            userId);

        // In a real implementation, update the database
        await Clients.Caller.NotificationPreferencesUpdated(preferences);
    }

    // ===== Helper Methods =====

    private static string GetUserGroupName(Guid userId)
        => $"user_{userId}";

    private static string GetTenantGroupName(Guid tenantId)
        => $"tenant_{tenantId}";

    private static string GetTenantRoleGroupName(Guid tenantId, string role)
        => $"tenant_{tenantId}_role_{role}";

    private static string GetEventNotificationGroupName(Guid tenantId, Guid eventId)
        => $"tenant_{tenantId}_event_{eventId}_notifications";

    /// <summary>
    /// Get all connection IDs for a specific user (useful for multi-device support)
    /// </summary>
    public static List<string> GetUserConnections(Guid userId)
    {
        lock (_lock)
        {
            return _userConnections.ContainsKey(userId)
                ? _userConnections[userId].ToList()
                : new List<string>();
        }
    }

    /// <summary>
    /// Check if a user is currently connected
    /// </summary>
    public static bool IsUserOnline(Guid userId)
    {
        lock (_lock)
        {
            return _userConnections.ContainsKey(userId) && _userConnections[userId].Count > 0;
        }
    }
}

/// <summary>
/// Strongly-typed client interface for NotificationHub
/// Defines methods that can be called on connected clients
/// </summary>
public interface INotificationHubClient
{
    // Connection
    Task Connected(object connectionInfo);
    Task Disconnected();

    // Notifications
    Task ReceiveNotification(object notification);
    Task NotificationMarkedAsRead(Guid notificationId);
    Task AllNotificationsMarkedAsRead();
    Task NotificationDeleted(Guid notificationId);
    Task UnreadCountUpdated(int count);

    // Preferences
    Task ReceiveNotificationPreferences(object preferences);
    Task NotificationPreferencesUpdated(object preferences);

    // Event-specific notifications
    Task EventNotification(Guid eventId, string type, object data);

    // System notifications
    Task SystemNotification(string type, string message, string severity);

    // Credit updates
    Task CreditBalanceUpdated(int remainingCredits, int usedCredits);

    // Payment notifications
    Task PaymentReceived(object paymentDetails);
    Task PaymentFailed(object paymentDetails);
}
