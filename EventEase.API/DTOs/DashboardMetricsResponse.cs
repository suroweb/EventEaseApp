namespace EventEase.API.DTOs;

/// <summary>
/// Dashboard key metrics and summary data
/// </summary>
public class DashboardMetricsResponse
{
    public Guid TenantId { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    // Key Metrics
    public KeyMetric TotalEvents { get; set; } = new();
    public KeyMetric TotalAttendees { get; set; } = new();
    public KeyMetric TotalRevenue { get; set; } = new();
    public KeyMetric ActiveUsers { get; set; } = new();

    // Quick Stats
    public int UpcomingEvents { get; set; }
    public int OngoingEvents { get; set; }
    public int CompletedEventsThisMonth { get; set; }
    public decimal AvailableCredits { get; set; }
    public decimal AverageEventCapacityUtilization { get; set; }

    // Recent Activity
    public List<RecentActivity> RecentActivities { get; set; } = new();

    // Trend Charts (last 12 months)
    public List<TrendDataPoint> EventsTrend { get; set; } = new();
    public List<TrendDataPoint> AttendeesTrend { get; set; } = new();
    public List<TrendDataPoint> RevenueTrend { get; set; } = new();

    // Top Performers
    public List<TopEvent> TopEventsByAttendance { get; set; } = new();
    public List<TopEvent> TopEventsByRevenue { get; set; } = new();
}

/// <summary>
/// Key metric with comparison data
/// </summary>
public class KeyMetric
{
    public decimal Value { get; set; }
    public decimal PreviousPeriodValue { get; set; }
    public decimal ChangePercentage { get; set; }
    public string Trend { get; set; } = "stable"; // "up", "down", "stable"
    public string Label { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty; // "", "EUR", "%", etc.
}

/// <summary>
/// Trend data point for charts
/// </summary>
public class TrendDataPoint
{
    public string Label { get; set; } = string.Empty; // Month name or date
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
}

/// <summary>
/// Recent activity item
/// </summary>
public class RecentActivity
{
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = string.Empty; // "event_created", "registration", "payment", etc.
    public string Description { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? EntityName { get; set; }
}

/// <summary>
/// Top performing event
/// </summary>
public class TopEvent
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public int AttendeeCount { get; set; }
    public decimal Revenue { get; set; }
    public string Currency { get; set; } = "EUR";
}
