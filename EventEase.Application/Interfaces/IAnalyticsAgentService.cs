using EventEase.Domain.Enums;

namespace EventEase.Application.Interfaces;

/// <summary>
/// Analytics Agent - Predictive analytics and insights (20-30 credits)
/// </summary>
public interface IAnalyticsAgentService
{
    /// <summary>
    /// Predict event attendance
    /// </summary>
    Task<AnalyticsAgentResult> PredictEventAttendanceAsync(
        Guid tenantId,
        Guid eventId,
        AIProvider? preferredProvider = null);

    /// <summary>
    /// Analyze sentiment from event feedback
    /// </summary>
    Task<AnalyticsAgentResult> AnalyzeEventSentimentAsync(
        Guid tenantId,
        Guid eventId,
        List<string> feedbackTexts,
        AIProvider? preferredProvider = null);

    /// <summary>
    /// Calculate ROI and recommendations
    /// </summary>
    Task<AnalyticsAgentResult> CalculateEventROIAsync(
        Guid tenantId,
        Guid eventId,
        decimal totalCost,
        AIProvider? preferredProvider = null);

    /// <summary>
    /// Generate comprehensive event analytics report
    /// </summary>
    Task<AnalyticsAgentResult> GenerateEventAnalyticsReportAsync(
        Guid tenantId,
        Guid eventId,
        AIProvider? preferredProvider = null);

    /// <summary>
    /// Identify trends across events
    /// </summary>
    Task<AnalyticsAgentResult> IdentifyEventTrendsAsync(
        Guid tenantId,
        DateTime startDate,
        DateTime endDate,
        AIProvider? preferredProvider = null);
}

/// <summary>
/// Analytics agent operation result
/// </summary>
public class AnalyticsAgentResult
{
    public bool Success { get; set; }
    public string Response { get; set; } = string.Empty;
    public decimal CreditsCost { get; set; }
    public AIProvider Provider { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public string? ErrorMessage { get; set; }
    public AttendancePrediction? AttendancePrediction { get; set; }
    public SentimentAnalysisSummary? SentimentSummary { get; set; }
    public ROICalculation? ROICalculation { get; set; }
    public object? AnalyticsData { get; set; }
}

/// <summary>
/// Attendance prediction details
/// </summary>
public class AttendancePrediction
{
    public int PredictedAttendees { get; set; }
    public int RegisteredCount { get; set; }
    public double PredictedAttendanceRate { get; set; }
    public string Confidence { get; set; } = "Medium";
    public List<string> Factors { get; set; } = new();
}

/// <summary>
/// Sentiment analysis summary
/// </summary>
public class SentimentAnalysisSummary
{
    public double OverallScore { get; set; }
    public string OverallSentiment { get; set; } = "Neutral";
    public int PositiveCount { get; set; }
    public int NeutralCount { get; set; }
    public int NegativeCount { get; set; }
    public List<string> KeyThemes { get; set; } = new();
    public List<string> Highlights { get; set; } = new();
    public List<string> Concerns { get; set; } = new();
}

/// <summary>
/// ROI calculation details
/// </summary>
public class ROICalculation
{
    public decimal TotalCost { get; set; }
    public decimal EstimatedRevenue { get; set; }
    public decimal ROIPercentage { get; set; }
    public decimal NetValue { get; set; }
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, decimal> CostBreakdown { get; set; } = new();
}
