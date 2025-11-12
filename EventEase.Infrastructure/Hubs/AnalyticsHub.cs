using EventEase.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EventEase.Infrastructure.Hubs;

/// <summary>
/// SignalR Hub for real-time analytics and dashboard updates
/// Handles live metrics, revenue tracking, and performance data
/// </summary>
[Authorize(Roles = "SystemAdmin,TenantOwner,TenantAdmin")]
public class AnalyticsHub : Hub<IAnalyticsHubClient>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ILogger<AnalyticsHub> _logger;

    // Track active dashboard viewers per tenant
    private static readonly Dictionary<Guid, HashSet<string>> _activeDashboardViewers = new();
    private static readonly object _lock = new();

    public AnalyticsHub(
        ICurrentUserService currentUserService,
        ICurrentTenantService currentTenantService,
        ILogger<AnalyticsHub> logger)
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
        var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        if (!tenantId.HasValue)
        {
            _logger.LogWarning("User {UserId} attempted to connect to AnalyticsHub without tenant context", userId);
            throw new HubException("Tenant context is required for analytics");
        }

        _logger.LogInformation(
            "User {UserId} ({UserName}, {Role}) from tenant {TenantId} connected to AnalyticsHub with connection {ConnectionId}",
            userId, userName, userRole, tenantId, Context.ConnectionId);

        // Track dashboard viewers
        lock (_lock)
        {
            if (!_activeDashboardViewers.ContainsKey(tenantId.Value))
            {
                _activeDashboardViewers[tenantId.Value] = new HashSet<string>();
            }
            _activeDashboardViewers[tenantId.Value].Add(Context.ConnectionId);
        }

        // Add to tenant analytics group
        await Groups.AddToGroupAsync(Context.ConnectionId, GetTenantAnalyticsGroupName(tenantId.Value));

        // Send initial dashboard snapshot
        await SendInitialDashboardDataAsync();

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _currentUserService.UserId;
        var tenantId = _currentTenantService.TenantId;

        // Remove from dashboard viewers
        if (tenantId.HasValue)
        {
            lock (_lock)
            {
                if (_activeDashboardViewers.ContainsKey(tenantId.Value))
                {
                    _activeDashboardViewers[tenantId.Value].Remove(Context.ConnectionId);
                    if (_activeDashboardViewers[tenantId.Value].Count == 0)
                    {
                        _activeDashboardViewers.Remove(tenantId.Value);
                    }
                }
            }
        }

        if (exception != null)
        {
            _logger.LogWarning(exception,
                "User {UserId} from tenant {TenantId} disconnected from AnalyticsHub with error",
                userId, tenantId);
        }
        else
        {
            _logger.LogInformation(
                "User {UserId} from tenant {TenantId} disconnected from AnalyticsHub",
                userId, tenantId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to real-time analytics for a specific event
    /// </summary>
    public async Task SubscribeToEventAnalyticsAsync(Guid eventId)
    {
        var tenantId = _currentTenantService.TenantId;
        if (!tenantId.HasValue)
        {
            throw new HubException("Tenant context is required");
        }

        var groupName = GetEventAnalyticsGroupName(tenantId.Value, eventId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "User {UserId} subscribed to analytics for event {EventId}",
            _currentUserService.UserId, eventId);

        // Send initial event analytics
        await SendInitialEventAnalyticsAsync(eventId);
    }

    /// <summary>
    /// Unsubscribe from real-time analytics for a specific event
    /// </summary>
    public async Task UnsubscribeFromEventAnalyticsAsync(Guid eventId)
    {
        var tenantId = _currentTenantService.TenantId;
        if (!tenantId.HasValue)
        {
            return;
        }

        var groupName = GetEventAnalyticsGroupName(tenantId.Value, eventId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "User {UserId} unsubscribed from analytics for event {EventId}",
            _currentUserService.UserId, eventId);
    }

    /// <summary>
    /// Request a specific metric update
    /// </summary>
    public async Task RequestMetricUpdateAsync(string metricName)
    {
        var tenantId = _currentTenantService.TenantId;
        if (!tenantId.HasValue)
        {
            throw new HubException("Tenant context is required");
        }

        _logger.LogInformation(
            "User {UserId} requested metric update for {MetricName}",
            _currentUserService.UserId, metricName);

        // In a real implementation, fetch the metric from the database
        var metricValue = await GetMetricValueAsync(tenantId.Value, metricName);

        await Clients.Caller.ReceiveMetricUpdate(metricName, metricValue);
    }

    /// <summary>
    /// Request dashboard refresh
    /// </summary>
    public async Task RequestDashboardRefreshAsync()
    {
        var tenantId = _currentTenantService.TenantId;
        if (!tenantId.HasValue)
        {
            throw new HubException("Tenant context is required");
        }

        _logger.LogInformation(
            "User {UserId} requested dashboard refresh",
            _currentUserService.UserId);

        await SendInitialDashboardDataAsync();
    }

    /// <summary>
    /// Request event performance report
    /// </summary>
    public async Task RequestEventPerformanceAsync(Guid eventId)
    {
        var tenantId = _currentTenantService.TenantId;
        if (!tenantId.HasValue)
        {
            throw new HubException("Tenant context is required");
        }

        _logger.LogInformation(
            "User {UserId} requested performance data for event {EventId}",
            _currentUserService.UserId, eventId);

        // In a real implementation, fetch performance data from the database
        var performanceData = new
        {
            EventId = eventId,
            TotalRegistrations = 0,
            CheckInRate = 0.0,
            RevenueGenerated = 0.0m,
            AverageRating = 0.0,
            EngagementScore = 0.0,
            LastUpdated = DateTime.UtcNow
        };

        await Clients.Caller.ReceiveEventPerformance(eventId, performanceData);
    }

    /// <summary>
    /// Set custom date range for analytics
    /// </summary>
    public async Task SetDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var tenantId = _currentTenantService.TenantId;
        if (!tenantId.HasValue)
        {
            throw new HubException("Tenant context is required");
        }

        _logger.LogInformation(
            "User {UserId} set date range for analytics: {StartDate} to {EndDate}",
            _currentUserService.UserId, startDate, endDate);

        // In a real implementation, fetch analytics for the date range
        var analyticsData = new
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalEvents = 0,
            TotalRegistrations = 0,
            TotalRevenue = 0.0m
        };

        await Clients.Caller.ReceiveDateRangeAnalytics(analyticsData);
    }

    // ===== Private Helper Methods =====

    private async Task SendInitialDashboardDataAsync()
    {
        var tenantId = _currentTenantService.TenantId;
        if (!tenantId.HasValue) return;

        // In a real implementation, fetch from database
        var dashboardData = new
        {
            TenantId = tenantId.Value,
            Summary = new
            {
                TotalEvents = 0,
                ActiveEvents = 0,
                TotalRegistrations = 0,
                TotalRevenue = 0.0m,
                CreditsRemaining = 0,
                CreditsUsed = 0
            },
            RecentActivity = Array.Empty<object>(),
            TopEvents = Array.Empty<object>(),
            RevenueByMonth = Array.Empty<object>(),
            RegistrationTrend = Array.Empty<object>(),
            LastUpdated = DateTime.UtcNow
        };

        await Clients.Caller.ReceiveDashboardUpdate(dashboardData);
    }

    private async Task SendInitialEventAnalyticsAsync(Guid eventId)
    {
        var tenantId = _currentTenantService.TenantId;
        if (!tenantId.HasValue) return;

        // In a real implementation, fetch from database
        var eventAnalytics = new
        {
            EventId = eventId,
            TotalRegistrations = 0,
            CheckedIn = 0,
            Cancelled = 0,
            Revenue = 0.0m,
            AverageRating = 0.0,
            PollResponses = 0,
            ChatMessages = 0,
            OnlineParticipants = 0,
            LastUpdated = DateTime.UtcNow
        };

        await Clients.Caller.ReceiveEventAnalytics(eventId, eventAnalytics);
    }

    private async Task<object> GetMetricValueAsync(Guid tenantId, string metricName)
    {
        // In a real implementation, fetch from database
        await Task.CompletedTask;

        return metricName switch
        {
            "totalEvents" => 0,
            "activeEvents" => 0,
            "totalRegistrations" => 0,
            "totalRevenue" => 0.0m,
            "creditsRemaining" => 0,
            _ => 0
        };
    }

    private static string GetTenantAnalyticsGroupName(Guid tenantId)
        => $"tenant_{tenantId}_analytics";

    private static string GetEventAnalyticsGroupName(Guid tenantId, Guid eventId)
        => $"tenant_{tenantId}_event_{eventId}_analytics";

    /// <summary>
    /// Get count of active dashboard viewers for a tenant
    /// </summary>
    public static int GetActiveDashboardViewerCount(Guid tenantId)
    {
        lock (_lock)
        {
            return _activeDashboardViewers.ContainsKey(tenantId)
                ? _activeDashboardViewers[tenantId].Count
                : 0;
        }
    }
}

/// <summary>
/// Strongly-typed client interface for AnalyticsHub
/// Defines methods that can be called on connected clients
/// </summary>
public interface IAnalyticsHubClient
{
    // Dashboard Updates
    Task ReceiveDashboardUpdate(object dashboardData);
    Task ReceiveDateRangeAnalytics(object analyticsData);

    // Metric Updates
    Task ReceiveMetricUpdate(string metricName, object metricValue);
    Task RevenueUpdated(decimal totalRevenue, decimal monthlyRevenue);
    Task RegistrationCountUpdated(int totalCount, int todayCount);
    Task EventCountUpdated(int totalEvents, int activeEvents);
    Task CreditUsageUpdated(int remainingCredits, int usedCredits, int totalCredits);

    // Event Analytics
    Task ReceiveEventAnalytics(Guid eventId, object analyticsData);
    Task ReceiveEventPerformance(Guid eventId, object performanceData);
    Task EventRevenueUpdated(Guid eventId, decimal revenue);
    Task EventRegistrationUpdated(Guid eventId, int registrationCount);

    // Real-time Activity Feed
    Task NewActivityReceived(object activity);

    // Reports
    Task ReportGenerated(string reportType, object reportData);
    Task ReportGenerationFailed(string reportType, string error);

    // Trends
    Task TrendDataUpdated(string trendType, object trendData);

    // Alerts
    Task MetricThresholdReached(string metricName, object thresholdData);
}
