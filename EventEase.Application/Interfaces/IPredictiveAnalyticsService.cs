namespace EventEase.Application.Interfaces;

/// <summary>
/// AI Predictive Analytics service using ML.NET
/// </summary>
public interface IPredictiveAnalyticsService
{
    /// <summary>
    /// Forecast attendance for an upcoming event
    /// </summary>
    Task<AttendanceForecast> ForecastAttendanceAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Predict no-show probability for registrants
    /// </summary>
    Task<List<NoShowPrediction>> PredictNoShowsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimize revenue with dynamic pricing suggestions
    /// </summary>
    Task<RevenueOptimization> OptimizeRevenueAsync(
        Guid eventId,
        RevenueOptimizationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Predict tenant churn risk
    /// </summary>
    Task<ChurnPrediction> PredictTenantChurnAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get event success prediction before event happens
    /// </summary>
    Task<EventSuccessPrediction> PredictEventSuccessAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Train ML models with historical data
    /// </summary>
    Task TrainModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get model performance metrics
    /// </summary>
    Task<ModelPerformanceMetrics> GetModelMetricsAsync(
        string modelName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Attendance forecast for an event
/// </summary>
public class AttendanceForecast
{
    public Guid EventId { get; set; }
    public int CurrentRegistrations { get; set; }
    public int PredictedFinalAttendance { get; set; }
    public int PredictedActualAttendees { get; set; } // Accounting for no-shows
    public double ConfidenceInterval { get; set; } // e.g., 0.95 for 95% confidence
    public int LowerBound { get; set; }
    public int UpperBound { get; set; }
    public List<DailyForecast> DailyForecasts { get; set; } = new();
    public string ModelVersion { get; set; } = string.Empty;
    public DateTime PredictionDate { get; set; }
}

public class DailyForecast
{
    public DateTime Date { get; set; }
    public int PredictedRegistrations { get; set; }
    public int ActualRegistrations { get; set; }
}

/// <summary>
/// No-show prediction for a registrant
/// </summary>
public class NoShowPrediction
{
    public Guid RegistrationId { get; set; }
    public Guid AttendeeId { get; set; }
    public string AttendeeName { get; set; } = string.Empty;
    public string AttendeeEmail { get; set; } = string.Empty;
    public double NoShowProbability { get; set; } // 0-1
    public string RiskLevel { get; set; } = string.Empty; // Low, Medium, High
    public List<string> RiskFactors { get; set; } = new();
    public string? RecommendedAction { get; set; }
}

/// <summary>
/// Revenue optimization request
/// </summary>
public class RevenueOptimizationRequest
{
    public decimal CurrentPrice { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int TargetAttendance { get; set; }
    public DateTime EventDate { get; set; }
    public int DaysUntilEvent { get; set; }
}

/// <summary>
/// Revenue optimization result
/// </summary>
public class RevenueOptimization
{
    public Guid EventId { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal RecommendedPrice { get; set; }
    public decimal PredictedRevenue { get; set; }
    public int PredictedAttendance { get; set; }
    public List<PricingScenario> AlternativeScenarios { get; set; } = new();
    public string OptimizationStrategy { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = new();
}

public class PricingScenario
{
    public decimal Price { get; set; }
    public int PredictedAttendance { get; set; }
    public decimal PredictedRevenue { get; set; }
    public double ConfidenceScore { get; set; }
}

/// <summary>
/// Tenant churn prediction
/// </summary>
public class ChurnPrediction
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public double ChurnProbability { get; set; } // 0-1
    public string RiskLevel { get; set; } = string.Empty; // Low, Medium, High
    public List<string> ChurnIndicators { get; set; } = new();
    public List<string> RetentionRecommendations { get; set; } = new();
    public DateTime PredictionDate { get; set; }
    public int DaysToLikelyChurn { get; set; }
}

/// <summary>
/// Event success prediction
/// </summary>
public class EventSuccessPrediction
{
    public Guid EventId { get; set; }
    public double SuccessProbability { get; set; } // 0-1
    public string PredictedOutcome { get; set; } = string.Empty; // Excellent, Good, Average, Poor
    public List<SuccessFactor> KeySuccessFactors { get; set; } = new();
    public List<string> RiskFactors { get; set; } = new();
    public List<string> ImprovementSuggestions { get; set; } = new();
    public double PredictedSatisfactionScore { get; set; } // 0-5
}

public class SuccessFactor
{
    public string Factor { get; set; } = string.Empty;
    public double Impact { get; set; } // -1 to 1
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// ML Model performance metrics
/// </summary>
public class ModelPerformanceMetrics
{
    public string ModelName { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public DateTime TrainingDate { get; set; }
    public int TrainingSamples { get; set; }
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public double MeanAbsoluteError { get; set; }
    public double RootMeanSquaredError { get; set; }
    public Dictionary<string, double> AdditionalMetrics { get; set; } = new();
}
