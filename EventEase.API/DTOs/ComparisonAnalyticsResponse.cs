namespace EventEase.API.DTOs;

/// <summary>
/// Period-over-period comparison analytics
/// </summary>
public class ComparisonAnalyticsResponse
{
    public string ComparisonType { get; set; } = string.Empty; // "month-over-month", "year-over-year", "custom"

    // Current Period
    public PeriodMetrics CurrentPeriod { get; set; } = new();

    // Previous Period
    public PeriodMetrics PreviousPeriod { get; set; } = new();

    // Comparison Metrics
    public ComparisonMetrics Comparison { get; set; } = new();
}

/// <summary>
/// Metrics for a specific period
/// </summary>
public class PeriodMetrics
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Label { get; set; } = string.Empty; // "January 2025", "Q4 2024", etc.

    // Core Metrics
    public int EventsCreated { get; set; }
    public int EventsPublished { get; set; }
    public int TotalRegistrations { get; set; }
    public int ConfirmedAttendees { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal CreditUsage { get; set; }
    public int AIRequests { get; set; }
    public decimal PaymentVolume { get; set; }

    // Engagement Metrics
    public decimal AverageAttendeesPerEvent { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal AttendanceRate { get; set; }
}

/// <summary>
/// Comparison calculations
/// </summary>
public class ComparisonMetrics
{
    public ChangeMetric EventsCreated { get; set; } = new();
    public ChangeMetric EventsPublished { get; set; } = new();
    public ChangeMetric TotalRegistrations { get; set; } = new();
    public ChangeMetric ConfirmedAttendees { get; set; } = new();
    public ChangeMetric TotalRevenue { get; set; } = new();
    public ChangeMetric CreditUsage { get; set; } = new();
    public ChangeMetric AIRequests { get; set; } = new();
    public ChangeMetric PaymentVolume { get; set; } = new();
    public ChangeMetric AverageAttendeesPerEvent { get; set; } = new();
    public ChangeMetric ConversionRate { get; set; } = new();
    public ChangeMetric AttendanceRate { get; set; } = new();

    // Overall Health
    public string OverallTrend { get; set; } = "stable"; // "improving", "declining", "stable"
    public decimal OverallChangePercentage { get; set; }
}

/// <summary>
/// Individual metric change
/// </summary>
public class ChangeMetric
{
    public decimal AbsoluteChange { get; set; }
    public decimal PercentageChange { get; set; }
    public string Direction { get; set; } = "stable"; // "up", "down", "stable"
    public bool IsImprovement { get; set; } // Context-aware (e.g., revenue up = improvement, cancellations up = not improvement)
}
