using EventEase.API.DTOs;
using EventEase.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventEase.API.Controllers;

/// <summary>
/// Analytics and insights endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IAnalyticsService analyticsService,
        ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard metrics with key performance indicators
    /// </summary>
    /// <param name="startDate">Start date for the period (optional, defaults to 30 days ago)</param>
    /// <param name="endDate">End date for the period (optional, defaults to now)</param>
    /// <response code="200">Returns dashboard metrics</response>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardMetricsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardMetricsResponse>> GetDashboardMetrics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            if (end <= start)
            {
                return BadRequest(new { Error = "End date must be after start date" });
            }

            var metrics = await _analyticsService.GetDashboardMetricsAsync(start, end);

            _logger.LogInformation("Dashboard metrics retrieved for period {StartDate} to {EndDate}",
                start, end);

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard metrics");
            return StatusCode(500, new { Error = "Failed to retrieve dashboard metrics" });
        }
    }

    /// <summary>
    /// Get comprehensive analytics for a specific event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <response code="200">Returns event analytics</response>
    /// <response code="404">Event not found</response>
    [HttpGet("events/{eventId}")]
    [ProducesResponseType(typeof(EventAnalyticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventAnalyticsResponse>> GetEventAnalytics(Guid eventId)
    {
        try
        {
            var analytics = await _analyticsService.GetEventAnalyticsAsync(eventId);

            _logger.LogInformation("Event analytics retrieved for event {EventId}", eventId);

            return Ok(analytics);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Error = $"Event {eventId} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event analytics for {EventId}", eventId);
            return StatusCode(500, new { Error = "Failed to retrieve event analytics" });
        }
    }

    /// <summary>
    /// Get analytics for multiple events (bulk)
    /// </summary>
    /// <param name="eventIds">List of event IDs</param>
    /// <response code="200">Returns list of event analytics</response>
    [HttpPost("events/bulk")]
    [ProducesResponseType(typeof(List<EventAnalyticsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EventAnalyticsResponse>>> GetBulkEventAnalytics(
        [FromBody] List<Guid> eventIds)
    {
        try
        {
            if (eventIds == null || !eventIds.Any())
            {
                return BadRequest(new { Error = "Event IDs list cannot be empty" });
            }

            if (eventIds.Count > 50)
            {
                return BadRequest(new { Error = "Cannot retrieve analytics for more than 50 events at once" });
            }

            var analytics = await _analyticsService.GetBulkEventAnalyticsAsync(eventIds);

            _logger.LogInformation("Bulk event analytics retrieved for {Count} events", eventIds.Count);

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bulk event analytics");
            return StatusCode(500, new { Error = "Failed to retrieve bulk event analytics" });
        }
    }

    /// <summary>
    /// Get tenant-wide analytics for a date range
    /// </summary>
    /// <param name="startDate">Start date (required)</param>
    /// <param name="endDate">End date (required)</param>
    /// <response code="200">Returns tenant analytics</response>
    [HttpGet("tenant")]
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [ProducesResponseType(typeof(TenantAnalyticsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TenantAnalyticsResponse>> GetTenantAnalytics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            if (endDate <= startDate)
            {
                return BadRequest(new { Error = "End date must be after start date" });
            }

            var analytics = await _analyticsService.GetTenantAnalyticsAsync(startDate, endDate);

            _logger.LogInformation("Tenant analytics retrieved for period {StartDate} to {EndDate}",
                startDate, endDate);

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant analytics");
            return StatusCode(500, new { Error = "Failed to retrieve tenant analytics" });
        }
    }

    /// <summary>
    /// Get registration funnel and conversion analytics
    /// </summary>
    /// <param name="eventId">Optional event ID to filter by specific event</param>
    /// <param name="startDate">Start date (required)</param>
    /// <param name="endDate">End date (required)</param>
    /// <response code="200">Returns registration analytics</response>
    [HttpGet("registrations")]
    [ProducesResponseType(typeof(RegistrationAnalyticsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RegistrationAnalyticsResponse>> GetRegistrationAnalytics(
        [FromQuery] Guid? eventId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            if (endDate <= startDate)
            {
                return BadRequest(new { Error = "End date must be after start date" });
            }

            var analytics = await _analyticsService.GetRegistrationAnalyticsAsync(
                eventId,
                startDate,
                endDate);

            _logger.LogInformation("Registration analytics retrieved for period {StartDate} to {EndDate}",
                startDate, endDate);

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving registration analytics");
            return StatusCode(500, new { Error = "Failed to retrieve registration analytics" });
        }
    }

    /// <summary>
    /// Get credit usage patterns and analytics
    /// </summary>
    /// <param name="startDate">Start date (required)</param>
    /// <param name="endDate">End date (required)</param>
    /// <response code="200">Returns credit analytics</response>
    [HttpGet("credits")]
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [ProducesResponseType(typeof(CreditAnalyticsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CreditAnalyticsResponse>> GetCreditAnalytics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            if (endDate <= startDate)
            {
                return BadRequest(new { Error = "End date must be after start date" });
            }

            var analytics = await _analyticsService.GetCreditAnalyticsAsync(startDate, endDate);

            _logger.LogInformation("Credit analytics retrieved for period {StartDate} to {EndDate}",
                startDate, endDate);

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credit analytics");
            return StatusCode(500, new { Error = "Failed to retrieve credit analytics" });
        }
    }

    /// <summary>
    /// Get period-over-period comparison analytics
    /// </summary>
    /// <param name="currentStart">Current period start date</param>
    /// <param name="currentEnd">Current period end date</param>
    /// <param name="comparisonType">Type of comparison (month-over-month, year-over-year, custom)</param>
    /// <response code="200">Returns comparison analytics</response>
    [HttpGet("comparison")]
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [ProducesResponseType(typeof(ComparisonAnalyticsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ComparisonAnalyticsResponse>> GetComparisonAnalytics(
        [FromQuery] DateTime currentStart,
        [FromQuery] DateTime currentEnd,
        [FromQuery] string comparisonType = "month-over-month")
    {
        try
        {
            if (currentEnd <= currentStart)
            {
                return BadRequest(new { Error = "End date must be after start date" });
            }

            var validTypes = new[] { "month-over-month", "year-over-year", "custom" };
            if (!validTypes.Contains(comparisonType.ToLower()))
            {
                return BadRequest(new
                {
                    Error = $"Invalid comparison type. Must be one of: {string.Join(", ", validTypes)}"
                });
            }

            var analytics = await _analyticsService.GetComparisonAnalyticsAsync(
                currentStart,
                currentEnd,
                comparisonType);

            _logger.LogInformation("Comparison analytics retrieved: {ComparisonType}", comparisonType);

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving comparison analytics");
            return StatusCode(500, new { Error = "Failed to retrieve comparison analytics" });
        }
    }

    /// <summary>
    /// Get predictive analytics and forecasting
    /// </summary>
    /// <param name="forecastDays">Number of days to forecast (default: 30, max: 90)</param>
    /// <response code="200">Returns predictive analytics</response>
    [HttpGet("predictive")]
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [ProducesResponseType(typeof(PredictiveAnalyticsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PredictiveAnalyticsResponse>> GetPredictiveAnalytics(
        [FromQuery] int forecastDays = 30)
    {
        try
        {
            if (forecastDays < 1 || forecastDays > 90)
            {
                return BadRequest(new { Error = "Forecast days must be between 1 and 90" });
            }

            var analytics = await _analyticsService.GetPredictiveAnalyticsAsync(forecastDays);

            _logger.LogInformation("Predictive analytics retrieved for {ForecastDays} days", forecastDays);

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving predictive analytics");
            return StatusCode(500, new { Error = "Failed to retrieve predictive analytics" });
        }
    }

    /// <summary>
    /// Calculate tenant health score
    /// </summary>
    /// <response code="200">Returns health score (0-100)</response>
    [HttpGet("health-score")]
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetTenantHealthScore()
    {
        try
        {
            var score = await _analyticsService.CalculateTenantHealthScoreAsync();

            _logger.LogInformation("Tenant health score calculated: {Score}", score);

            return Ok(new
            {
                HealthScore = score,
                MaxScore = 100,
                Rating = GetHealthRating(score),
                CalculatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating tenant health score");
            return StatusCode(500, new { Error = "Failed to calculate tenant health score" });
        }
    }

    /// <summary>
    /// Get real-time metrics (cached for performance)
    /// </summary>
    /// <response code="200">Returns real-time metrics</response>
    [HttpGet("realtime")]
    [ProducesResponseType(typeof(Dictionary<string, decimal>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, decimal>>> GetRealTimeMetrics()
    {
        try
        {
            var metrics = await _analyticsService.GetRealTimeMetricsAsync();

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving real-time metrics");
            return StatusCode(500, new { Error = "Failed to retrieve real-time metrics" });
        }
    }

    // Helper methods
    private string GetHealthRating(decimal score)
    {
        return score switch
        {
            >= 90 => "Excellent",
            >= 75 => "Good",
            >= 60 => "Fair",
            >= 40 => "Needs Improvement",
            _ => "Critical"
        };
    }
}
