using EventEase.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EventEase.Infrastructure.Hubs;

/// <summary>
/// SignalR Hub for real-time event updates
/// Handles live attendee tracking, registration updates, polls, and event chat
/// </summary>
[Authorize]
public class EventHub : Hub<IEventHubClient>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ILogger<EventHub> _logger;

    public EventHub(
        ICurrentUserService currentUserService,
        ICurrentTenantService currentTenantService,
        ILogger<EventHub> logger)
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

        _logger.LogInformation(
            "User {UserId} ({UserName}) from tenant {TenantId} connected to EventHub with connection {ConnectionId}",
            userId, userName, tenantId, Context.ConnectionId);

        // Add user to their tenant group
        if (tenantId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetTenantGroupName(tenantId.Value));
            _logger.LogDebug("Added connection {ConnectionId} to tenant group {TenantId}",
                Context.ConnectionId, tenantId.Value);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _currentUserService.UserId;
        var tenantId = _currentTenantService.TenantId;

        if (exception != null)
        {
            _logger.LogWarning(exception,
                "User {UserId} from tenant {TenantId} disconnected from EventHub with error",
                userId, tenantId);
        }
        else
        {
            _logger.LogInformation(
                "User {UserId} from tenant {TenantId} disconnected from EventHub",
                userId, tenantId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to real-time updates for a specific event
    /// </summary>
    public async Task JoinEventAsync(Guid eventId)
    {
        var tenantId = _currentTenantService.TenantId;
        if (!tenantId.HasValue)
        {
            _logger.LogWarning("User attempted to join event {EventId} without tenant context", eventId);
            throw new HubException("Tenant context is required");
        }

        var groupName = GetEventGroupName(tenantId.Value, eventId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        var userId = _currentUserService.UserId;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";

        _logger.LogInformation(
            "User {UserId} joined event {EventId} (Group: {GroupName})",
            userId, eventId, groupName);

        // Notify other participants that user joined
        await Clients.OthersInGroup(groupName).UserJoinedEvent(userId, userName);
    }

    /// <summary>
    /// Unsubscribe from real-time updates for a specific event
    /// </summary>
    public async Task LeaveEventAsync(Guid eventId)
    {
        var tenantId = _currentTenantService.TenantId;
        if (!tenantId.HasValue)
        {
            return;
        }

        var groupName = GetEventGroupName(tenantId.Value, eventId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        var userId = _currentUserService.UserId;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";

        _logger.LogInformation(
            "User {UserId} left event {EventId} (Group: {GroupName})",
            userId, eventId, groupName);

        // Notify other participants that user left
        await Clients.Group(groupName).UserLeftEvent(userId, userName);
    }

    /// <summary>
    /// Send a chat message to all event participants
    /// </summary>
    public async Task SendChatMessageAsync(Guid eventId, string message)
    {
        var tenantId = _currentTenantService.TenantId;
        if (!tenantId.HasValue)
        {
            throw new HubException("Tenant context is required");
        }

        var userId = _currentUserService.UserId;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";

        if (string.IsNullOrWhiteSpace(message) || message.Length > 2000)
        {
            throw new HubException("Invalid message");
        }

        var groupName = GetEventGroupName(tenantId.Value, eventId);

        _logger.LogInformation(
            "User {UserId} sent chat message to event {EventId}",
            userId, eventId);

        // Broadcast message to all event participants
        await Clients.Group(groupName).ReceiveChatMessage(
            userId,
            userName,
            message,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Send typing indicator to event participants
    /// </summary>
    public async Task SendTypingIndicatorAsync(Guid eventId, bool isTyping)
    {
        var tenantId = _currentTenantService.TenantId;
        if (!tenantId.HasValue)
        {
            return;
        }

        var userId = _currentUserService.UserId;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";
        var groupName = GetEventGroupName(tenantId.Value, eventId);

        // Send typing indicator to others in the event (not to sender)
        await Clients.OthersInGroup(groupName).UserTyping(userId, userName, isTyping);
    }

    /// <summary>
    /// Submit a poll response
    /// </summary>
    public async Task SubmitPollResponseAsync(Guid eventId, Guid pollId, string response)
    {
        var tenantId = _currentTenantService.TenantId;
        if (!tenantId.HasValue)
        {
            throw new HubException("Tenant context is required");
        }

        var userId = _currentUserService.UserId;

        _logger.LogInformation(
            "User {UserId} submitted poll response for poll {PollId} in event {EventId}",
            userId, pollId, eventId);

        // In a real implementation, save the response to the database
        // For now, just acknowledge the response
        await Clients.Caller.PollResponseSubmitted(pollId, response);
    }

    /// <summary>
    /// Request current event statistics
    /// </summary>
    public async Task RequestEventStatsAsync(Guid eventId)
    {
        var tenantId = _currentTenantService.TenantId;
        if (!tenantId.HasValue)
        {
            throw new HubException("Tenant context is required");
        }

        // In a real implementation, fetch stats from the database
        // For now, return mock data
        var stats = new
        {
            EventId = eventId,
            TotalRegistrations = 0,
            CheckedInCount = 0,
            OnlineParticipants = 0,
            LastUpdated = DateTime.UtcNow
        };

        await Clients.Caller.ReceiveEventStats(stats);
    }

    // ===== Helper Methods =====

    private static string GetTenantGroupName(Guid tenantId)
        => $"tenant_{tenantId}";

    private static string GetEventGroupName(Guid tenantId, Guid eventId)
        => $"tenant_{tenantId}_event_{eventId}";
}

/// <summary>
/// Strongly-typed client interface for EventHub
/// Defines methods that can be called on connected clients
/// </summary>
public interface IEventHubClient
{
    // Event Updates
    Task EventUpdated(Guid eventId, object eventData);
    Task EventStatusChanged(Guid eventId, string status);
    Task ReceiveEventStats(object stats);

    // Registration Updates
    Task NewRegistration(Guid eventId, Guid registrationId, object registrationData);
    Task AttendeeCountUpdated(Guid eventId, int count);

    // Chat
    Task ReceiveChatMessage(Guid userId, string userName, string message, DateTime timestamp);
    Task UserTyping(Guid userId, string userName, bool isTyping);
    Task UserJoinedEvent(Guid userId, string userName);
    Task UserLeftEvent(Guid userId, string userName);

    // Polls
    Task ReceivePollResults(Guid pollId, object pollResults);
    Task PollResponseSubmitted(Guid pollId, string response);
    Task NewPollAvailable(Guid pollId, object pollData);
}
