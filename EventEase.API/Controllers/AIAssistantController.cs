using EventEase.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventEase.API.Controllers;

/// <summary>
/// AI Assistant controller for next-gen AI-powered features
/// Phase 2.0 - AI-Powered Features (Next-Gen)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIAssistantController : ControllerBase
{
    private readonly IEventAssistantService _eventAssistantService;
    private readonly IMatchmakingService _matchmakingService;
    private readonly IPredictiveAnalyticsService _predictiveAnalyticsService;
    private readonly IContentGenerationService _contentGenerationService;
    private readonly ILogger<AIAssistantController> _logger;

    public AIAssistantController(
        IEventAssistantService eventAssistantService,
        IMatchmakingService matchmakingService,
        IPredictiveAnalyticsService predictiveAnalyticsService,
        IContentGenerationService contentGenerationService,
        ILogger<AIAssistantController> logger)
    {
        _eventAssistantService = eventAssistantService;
        _matchmakingService = matchmakingService;
        _predictiveAnalyticsService = predictiveAnalyticsService;
        _contentGenerationService = contentGenerationService;
        _logger = logger;
    }

    #region Event Assistant Endpoints

    /// <summary>
    /// Create event from natural language description
    /// </summary>
    [HttpPost("event-assistant/create-from-language")]
    [ProducesResponseType(typeof(EventCreationSuggestion), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEventFromNaturalLanguage(
        [FromBody] CreateFromNaturalLanguageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = GetTenantId();
            var suggestion = await _eventAssistantService.CreateEventFromNaturalLanguageAsync(
                request.Description,
                tenantId,
                cancellationToken);

            return Ok(suggestion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event from natural language");
            return BadRequest(new { error = "Failed to process natural language input" });
        }
    }

    /// <summary>
    /// Get optimal event schedule suggestions
    /// </summary>
    [HttpPost("event-assistant/suggest-schedule")]
    [ProducesResponseType(typeof(EventScheduleSuggestion), StatusCodes.Status200OK)]
    public async Task<IActionResult> SuggestEventSchedule(
        [FromBody] EventScheduleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var suggestion = await _eventAssistantService.SuggestEventScheduleAsync(request, cancellationToken);
            return Ok(suggestion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting event schedule");
            return BadRequest(new { error = "Failed to generate schedule suggestion" });
        }
    }

    /// <summary>
    /// Get venue recommendations
    /// </summary>
    [HttpPost("event-assistant/recommend-venues")]
    [ProducesResponseType(typeof(List<VenueRecommendation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecommendVenues(
        [FromBody] VenueRecommendationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var recommendations = await _eventAssistantService.RecommendVenuesAsync(request, cancellationToken);
            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recommending venues");
            return BadRequest(new { error = "Failed to generate venue recommendations" });
        }
    }

    /// <summary>
    /// Get speaker suggestions
    /// </summary>
    [HttpPost("event-assistant/suggest-speakers")]
    [ProducesResponseType(typeof(List<SpeakerSuggestion>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SuggestSpeakers(
        [FromBody] SpeakerSuggestionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var suggestions = await _eventAssistantService.SuggestSpeakersAsync(request, cancellationToken);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting speakers");
            return BadRequest(new { error = "Failed to generate speaker suggestions" });
        }
    }

    /// <summary>
    /// Optimize event agenda
    /// </summary>
    [HttpPost("event-assistant/optimize-agenda")]
    [ProducesResponseType(typeof(AgendaOptimization), StatusCodes.Status200OK)]
    public async Task<IActionResult> OptimizeAgenda(
        [FromBody] AgendaOptimizationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var optimization = await _eventAssistantService.OptimizeAgendaAsync(request, cancellationToken);
            return Ok(optimization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing agenda");
            return BadRequest(new { error = "Failed to optimize agenda" });
        }
    }

    /// <summary>
    /// Ask a question about an event
    /// </summary>
    [HttpPost("event-assistant/ask/{eventId}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> AskEventQuestion(
        Guid eventId,
        [FromBody] AskQuestionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var answer = await _eventAssistantService.AnswerEventQuestionAsync(
                eventId,
                request.Question,
                cancellationToken);
            return Ok(new { answer });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error answering event question");
            return BadRequest(new { error = "Failed to process question" });
        }
    }

    #endregion

    #region Matchmaking & Networking Endpoints

    /// <summary>
    /// Get networking suggestions for an attendee
    /// </summary>
    [HttpGet("matchmaking/networking-suggestions/{eventId}")]
    [ProducesResponseType(typeof(List<NetworkingSuggestion>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNetworkingSuggestions(
        Guid eventId,
        [FromQuery] int maxSuggestions = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();
            var suggestions = await _matchmakingService.GetNetworkingSuggestionsAsync(
                userId,
                eventId,
                maxSuggestions,
                cancellationToken);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting networking suggestions");
            return BadRequest(new { error = "Failed to generate networking suggestions" });
        }
    }

    /// <summary>
    /// Generate conversation starters between two attendees
    /// </summary>
    [HttpPost("matchmaking/conversation-starters")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateConversationStarters(
        [FromBody] ConversationStartersRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var starters = await _matchmakingService.GenerateConversationStartersAsync(
                request.AttendeeId1,
                request.AttendeeId2,
                cancellationToken);
            return Ok(new { conversationStarters = starters });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating conversation starters");
            return BadRequest(new { error = "Failed to generate conversation starters" });
        }
    }

    /// <summary>
    /// Get follow-up recommendations after an event
    /// </summary>
    [HttpGet("matchmaking/follow-up-recommendations/{eventId}")]
    [ProducesResponseType(typeof(List<FollowUpRecommendation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFollowUpRecommendations(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var recommendations = await _matchmakingService.GetFollowUpRecommendationsAsync(
                userId,
                eventId,
                cancellationToken);
            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting follow-up recommendations");
            return BadRequest(new { error = "Failed to generate follow-up recommendations" });
        }
    }

    /// <summary>
    /// Find similar attendees
    /// </summary>
    [HttpGet("matchmaking/similar-attendees")]
    [ProducesResponseType(typeof(List<SimilarAttendee>), StatusCodes.Status200OK)]
    public async Task<IActionResult> FindSimilarAttendees(
        [FromQuery] int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();
            var similarAttendees = await _matchmakingService.FindSimilarAttendeesAsync(
                userId,
                maxResults,
                cancellationToken);
            return Ok(similarAttendees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar attendees");
            return BadRequest(new { error = "Failed to find similar attendees" });
        }
    }

    #endregion

    #region Predictive Analytics Endpoints

    /// <summary>
    /// Forecast attendance for an event
    /// </summary>
    [HttpGet("analytics/forecast-attendance/{eventId}")]
    [ProducesResponseType(typeof(AttendanceForecast), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForecastAttendance(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        try
        {
            var forecast = await _predictiveAnalyticsService.ForecastAttendanceAsync(eventId, cancellationToken);
            return Ok(forecast);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forecasting attendance for event {EventId}", eventId);
            return BadRequest(new { error = "Failed to forecast attendance" });
        }
    }

    /// <summary>
    /// Predict no-shows for an event
    /// </summary>
    [HttpGet("analytics/predict-no-shows/{eventId}")]
    [ProducesResponseType(typeof(List<NoShowPrediction>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PredictNoShows(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        try
        {
            var predictions = await _predictiveAnalyticsService.PredictNoShowsAsync(eventId, cancellationToken);
            return Ok(predictions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting no-shows for event {EventId}", eventId);
            return BadRequest(new { error = "Failed to predict no-shows" });
        }
    }

    /// <summary>
    /// Optimize revenue for an event
    /// </summary>
    [HttpPost("analytics/optimize-revenue/{eventId}")]
    [ProducesResponseType(typeof(RevenueOptimization), StatusCodes.Status200OK)]
    public async Task<IActionResult> OptimizeRevenue(
        Guid eventId,
        [FromBody] RevenueOptimizationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var optimization = await _predictiveAnalyticsService.OptimizeRevenueAsync(
                eventId,
                request,
                cancellationToken);
            return Ok(optimization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing revenue for event {EventId}", eventId);
            return BadRequest(new { error = "Failed to optimize revenue" });
        }
    }

    /// <summary>
    /// Predict tenant churn risk
    /// </summary>
    [HttpGet("analytics/predict-churn")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ChurnPrediction), StatusCodes.Status200OK)]
    public async Task<IActionResult> PredictTenantChurn(CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = GetTenantId();
            var prediction = await _predictiveAnalyticsService.PredictTenantChurnAsync(tenantId, cancellationToken);
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting tenant churn");
            return BadRequest(new { error = "Failed to predict churn" });
        }
    }

    /// <summary>
    /// Predict event success
    /// </summary>
    [HttpGet("analytics/predict-success/{eventId}")]
    [ProducesResponseType(typeof(EventSuccessPrediction), StatusCodes.Status200OK)]
    public async Task<IActionResult> PredictEventSuccess(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        try
        {
            var prediction = await _predictiveAnalyticsService.PredictEventSuccessAsync(eventId, cancellationToken);
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting event success for event {EventId}", eventId);
            return BadRequest(new { error = "Failed to predict event success" });
        }
    }

    /// <summary>
    /// Get ML model performance metrics
    /// </summary>
    [HttpGet("analytics/model-metrics/{modelName}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ModelPerformanceMetrics), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModelMetrics(
        string modelName,
        CancellationToken cancellationToken)
    {
        try
        {
            var metrics = await _predictiveAnalyticsService.GetModelMetricsAsync(modelName, cancellationToken);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model metrics for {ModelName}", modelName);
            return BadRequest(new { error = "Failed to retrieve model metrics" });
        }
    }

    #endregion

    #region Content Generation Endpoints

    /// <summary>
    /// Generate event description
    /// </summary>
    [HttpPost("content/generate-description")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateEventDescription(
        [FromBody] EventDescriptionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var description = await _contentGenerationService.GenerateEventDescriptionAsync(request, cancellationToken);
            return Ok(new { description });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating event description");
            return BadRequest(new { error = "Failed to generate event description" });
        }
    }

    /// <summary>
    /// Generate email content
    /// </summary>
    [HttpPost("content/generate-email")]
    [ProducesResponseType(typeof(EmailContent), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateEmailContent(
        [FromBody] EmailGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var email = await _contentGenerationService.GenerateEmailContentAsync(request, cancellationToken);
            return Ok(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating email content");
            return BadRequest(new { error = "Failed to generate email content" });
        }
    }

    /// <summary>
    /// Generate social media posts
    /// </summary>
    [HttpPost("content/generate-social-media")]
    [ProducesResponseType(typeof(List<SocialMediaPost>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateSocialMediaPosts(
        [FromBody] SocialMediaRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var posts = await _contentGenerationService.GenerateSocialMediaPostsAsync(request, cancellationToken);
            return Ok(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating social media posts");
            return BadRequest(new { error = "Failed to generate social media posts" });
        }
    }

    /// <summary>
    /// Generate session summary
    /// </summary>
    [HttpPost("content/generate-session-summary")]
    [ProducesResponseType(typeof(SessionSummary), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateSessionSummary(
        [FromBody] SessionSummaryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _contentGenerationService.GenerateSessionSummaryAsync(
                request.SessionTitle,
                request.Content,
                cancellationToken);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating session summary");
            return BadRequest(new { error = "Failed to generate session summary" });
        }
    }

    /// <summary>
    /// Get personalized event recommendations
    /// </summary>
    [HttpGet("content/event-recommendations")]
    [ProducesResponseType(typeof(List<EventRecommendation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventRecommendations(
        [FromQuery] int maxRecommendations = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();
            var recommendations = await _contentGenerationService.GenerateEventRecommendationsAsync(
                userId,
                maxRecommendations,
                cancellationToken);
            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event recommendations");
            return BadRequest(new { error = "Failed to generate event recommendations" });
        }
    }

    /// <summary>
    /// Generate pricing suggestions
    /// </summary>
    [HttpPost("content/generate-pricing")]
    [ProducesResponseType(typeof(PricingSuggestion), StatusCodes.Status200OK)]
    public async Task<IActionResult> GeneratePricingSuggestion(
        [FromBody] PricingSuggestionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var suggestion = await _contentGenerationService.GeneratePricingSuggestionAsync(request, cancellationToken);
            return Ok(suggestion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pricing suggestion");
            return BadRequest(new { error = "Failed to generate pricing suggestion" });
        }
    }

    /// <summary>
    /// Generate event hashtags
    /// </summary>
    [HttpPost("content/generate-hashtags")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateHashtags(
        [FromBody] HashtagRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var hashtags = await _contentGenerationService.GenerateEventHashtagsAsync(
                request.EventTitle,
                request.EventDescription,
                request.MaxHashtags,
                cancellationToken);
            return Ok(new { hashtags });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating hashtags");
            return BadRequest(new { error = "Failed to generate hashtags" });
        }
    }

    /// <summary>
    /// Generate event FAQ
    /// </summary>
    [HttpGet("content/generate-faq/{eventId}")]
    [ProducesResponseType(typeof(List<FAQ>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateEventFAQ(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        try
        {
            var faqs = await _contentGenerationService.GenerateEventFAQAsync(eventId, cancellationToken);
            return Ok(faqs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating FAQ for event {EventId}", eventId);
            return BadRequest(new { error = "Failed to generate FAQ" });
        }
    }

    #endregion

    #region Helper Methods

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    private Guid GetTenantId()
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            throw new UnauthorizedAccessException("Tenant ID not found in token");
        }
        return tenantId;
    }

    #endregion
}

#region Request DTOs

public class CreateFromNaturalLanguageRequest
{
    public string Description { get; set; } = string.Empty;
}

public class AskQuestionRequest
{
    public string Question { get; set; } = string.Empty;
}

public class ConversationStartersRequest
{
    public Guid AttendeeId1 { get; set; }
    public Guid AttendeeId2 { get; set; }
}

public class SessionSummaryRequest
{
    public string SessionTitle { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class HashtagRequest
{
    public string EventTitle { get; set; } = string.Empty;
    public string EventDescription { get; set; } = string.Empty;
    public int MaxHashtags { get; set; } = 10;
}

#endregion
