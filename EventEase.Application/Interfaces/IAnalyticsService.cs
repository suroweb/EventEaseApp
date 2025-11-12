using EventEase.API.DTOs;

namespace EventEase.Application.Interfaces;

/// <summary>
/// Service for generating analytics and insights
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Get comprehensive analytics for a specific event
    /// </summary>
    Task<EventAnalyticsResponse> GetEventAnalyticsAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tenant-wide analytics for a date range
    /// </summary>
    Task<TenantAnalyticsResponse> GetTenantAnalyticsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get registration funnel and conversion analytics
    /// </summary>
    Task<RegistrationAnalyticsResponse> GetRegistrationAnalyticsAsync(
        Guid? eventId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get credit usage patterns and analytics
    /// </summary>
    Task<CreditAnalyticsResponse> GetCreditAnalyticsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get dashboard metrics with key performance indicators
    /// </summary>
    Task<DashboardMetricsResponse> GetDashboardMetricsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get period-over-period comparison analytics
    /// </summary>
    Task<ComparisonAnalyticsResponse> GetComparisonAnalyticsAsync(
        DateTime currentStart,
        DateTime currentEnd,
        string comparisonType = "month-over-month",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get predictive analytics and forecasting
    /// </summary>
    Task<PredictiveAnalyticsResponse> GetPredictiveAnalyticsAsync(
        int forecastDays = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get analytics for multiple events (bulk)
    /// </summary>
    Task<List<EventAnalyticsResponse>> GetBulkEventAnalyticsAsync(
        List<Guid> eventIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate tenant health score based on various metrics
    /// </summary>
    Task<decimal> CalculateTenantHealthScoreAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get real-time metrics (cached for performance)
    /// </summary>
    Task<Dictionary<string, decimal>> GetRealTimeMetricsAsync(CancellationToken cancellationToken = default);
}
