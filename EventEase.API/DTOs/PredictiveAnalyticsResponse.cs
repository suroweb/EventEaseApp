namespace EventEase.API.DTOs;

/// <summary>
/// Predictive analytics and forecasting
/// </summary>
public class PredictiveAnalyticsResponse
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string ForecastPeriod { get; set; } = string.Empty; // "Next 30 days", "Next Quarter", etc.

    // Event Forecasts
    public ForecastMetric ExpectedEvents { get; set; } = new();
    public ForecastMetric ExpectedAttendees { get; set; } = new();
    public ForecastMetric ExpectedRevenue { get; set; } = new();
    public ForecastMetric ExpectedCreditUsage { get; set; } = new();

    // Trends
    public List<ForecastDataPoint> EventsForecast { get; set; } = new();
    public List<ForecastDataPoint> AttendeesForecast { get; set; } = new();
    public List<ForecastDataPoint> RevenueForecast { get; set; } = new();

    // Recommendations
    public List<Recommendation> Recommendations { get; set; } = new();

    // Risk Indicators
    public List<RiskIndicator> RiskIndicators { get; set; } = new();

    // Confidence Level
    public decimal ConfidenceScore { get; set; } // 0-100
    public string ModelAccuracy { get; set; } = string.Empty;
}

/// <summary>
/// Forecast metric with confidence interval
/// </summary>
public class ForecastMetric
{
    public decimal PredictedValue { get; set; }
    public decimal LowerBound { get; set; }
    public decimal UpperBound { get; set; }
    public decimal ConfidenceLevel { get; set; } // 0-100
    public string Unit { get; set; } = string.Empty;
}

/// <summary>
/// Forecast data point for time series
/// </summary>
public class ForecastDataPoint
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal PredictedValue { get; set; }
    public decimal? ActualValue { get; set; } // Null for future dates
    public decimal LowerBound { get; set; }
    public decimal UpperBound { get; set; }
}

/// <summary>
/// AI-generated recommendation
/// </summary>
public class Recommendation
{
    public string Category { get; set; } = string.Empty; // "events", "marketing", "pricing", "capacity"
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium"; // "high", "medium", "low"
    public decimal ImpactScore { get; set; } // 0-100
    public List<string> Actions { get; set; } = new();
}

/// <summary>
/// Risk indicator
/// </summary>
public class RiskIndicator
{
    public string Type { get; set; } = string.Empty; // "capacity", "budget", "credits", "engagement"
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "medium"; // "high", "medium", "low"
    public decimal Probability { get; set; } // 0-100
    public DateTime? EstimatedOccurrence { get; set; }
    public List<string> MitigationStrategies { get; set; } = new();
}
