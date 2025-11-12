using EventEase.API.DTOs;
using EventEase.Application.Interfaces;
using EventEase.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace EventEase.API.Controllers;

/// <summary>
/// AI Agents endpoints for intelligent automation
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "EventManagerOrAbove")]
[Produces("application/json")]
public class AIAgentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IPlanningAgentService _planningAgent;
    private readonly IInvitationAgentService _invitationAgent;
    private readonly IAnalyticsAgentService _analyticsAgent;
    private readonly IBudgetAgentService _budgetAgent;
    private readonly IIntegrationAgentService _integrationAgent;
    private readonly ILogger<AIAgentsController> _logger;

    public AIAgentsController(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICurrentTenantService currentTenantService,
        IPlanningAgentService planningAgent,
        IInvitationAgentService invitationAgent,
        IAnalyticsAgentService analyticsAgent,
        IBudgetAgentService budgetAgent,
        IIntegrationAgentService integrationAgent,
        ILogger<AIAgentsController> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _currentTenantService = currentTenantService;
        _planningAgent = planningAgent;
        _invitationAgent = invitationAgent;
        _analyticsAgent = analyticsAgent;
        _budgetAgent = budgetAgent;
        _integrationAgent = integrationAgent;
        _logger = logger;
    }

    #region Planning Agent Endpoints (15 credits)

    /// <summary>
    /// Create event from natural language description (Planning Agent - 15 credits)
    /// </summary>
    [HttpPost("planning/create-event")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> CreateEventFromDescription(
        [FromBody] PlanningAgentCreateEventRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _planningAgent.CreateEventFromDescriptionAsync(
            _currentTenantService.TenantId,
            request.Description,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            Data = result.StructuredData
        });
    }

    /// <summary>
    /// Get venue recommendations (Planning Agent - 15 credits)
    /// </summary>
    [HttpPost("planning/venue-recommendations")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> GetVenueRecommendations(
        [FromBody] PlanningAgentVenueRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _planningAgent.GetVenueRecommendationsAsync(
            _currentTenantService.TenantId,
            request.EventType,
            request.ExpectedAttendees,
            request.Location,
            request.Budget,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens
        });
    }

    /// <summary>
    /// Optimize event schedule (Planning Agent - 15 credits)
    /// </summary>
    [HttpPost("planning/optimize-schedule")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> OptimizeEventSchedule(
        [FromBody] PlanningAgentScheduleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _planningAgent.OptimizeEventScheduleAsync(
            _currentTenantService.TenantId,
            request.EventId,
            request.Requirements,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens
        });
    }

    /// <summary>
    /// Generate event marketing content (Planning Agent - 15 credits)
    /// </summary>
    [HttpPost("planning/generate-content")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> GenerateEventContent(
        [FromBody] PlanningAgentContentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _planningAgent.GenerateEventContentAsync(
            _currentTenantService.TenantId,
            request.EventName,
            request.EventType,
            request.TargetAudience,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens
        });
    }

    #endregion

    #region Invitation Agent Endpoints (10 + 0.5/guest credits)

    /// <summary>
    /// Select optimal guests for event using ML (Invitation Agent - 10 + 0.5/guest credits)
    /// </summary>
    [HttpPost("invitation/select-guests")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> SelectGuestsForEvent(
        [FromBody] InvitationAgentSelectGuestsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _invitationAgent.SelectGuestsForEventAsync(
            _currentTenantService.TenantId,
            request.EventId,
            request.MaxGuests,
            request.SelectionCriteria,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            Data = result.SelectedGuests
        });
    }

    /// <summary>
    /// Generate personalized invitation messages (Invitation Agent - 10 + 0.5/guest credits)
    /// </summary>
    [HttpPost("invitation/personalize")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> GeneratePersonalizedInvitations(
        [FromBody] InvitationAgentPersonalizeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _invitationAgent.GeneratePersonalizedInvitationsAsync(
            _currentTenantService.TenantId,
            request.EventId,
            request.GuestIds,
            request.CustomMessage,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            Data = result.PersonalizedMessages
        });
    }

    /// <summary>
    /// Determine optimal send times for invitations (Invitation Agent - 10 + 0.5/guest credits)
    /// </summary>
    [HttpPost("invitation/optimal-send-time")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> DetermineOptimalSendTime(
        [FromBody] InvitationAgentSendTimeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _invitationAgent.DetermineOptimalSendTimeAsync(
            _currentTenantService.TenantId,
            request.EventId,
            request.GuestIds,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            Data = result.OptimalSendTimes
        });
    }

    /// <summary>
    /// Predict guest attendance likelihood (Invitation Agent - 10.5 credits)
    /// </summary>
    [HttpPost("invitation/predict-attendance")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> PredictGuestAttendance(
        [FromBody] InvitationAgentPredictRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _invitationAgent.PredictGuestAttendanceAsync(
            _currentTenantService.TenantId,
            request.GuestId,
            request.EventId,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            Data = result.AttendancePredictions
        });
    }

    #endregion

    #region Analytics Agent Endpoints (20-30 credits)

    /// <summary>
    /// Predict event attendance (Analytics Agent - 20 credits)
    /// </summary>
    [HttpPost("analytics/predict-attendance/{eventId}")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> PredictEventAttendance(
        Guid eventId,
        [FromBody] AIAgentRequest? request = null)
    {
        var result = await _analyticsAgent.PredictEventAttendanceAsync(
            _currentTenantService.TenantId,
            eventId,
            request?.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            Data = result.AttendancePrediction
        });
    }

    /// <summary>
    /// Analyze event feedback sentiment (Analytics Agent - 20 credits)
    /// </summary>
    [HttpPost("analytics/sentiment")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> AnalyzeEventSentiment(
        [FromBody] AnalyticsAgentSentimentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _analyticsAgent.AnalyzeEventSentimentAsync(
            _currentTenantService.TenantId,
            request.EventId,
            request.FeedbackTexts,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            Data = result.SentimentSummary
        });
    }

    /// <summary>
    /// Calculate event ROI (Analytics Agent - 20 credits)
    /// </summary>
    [HttpPost("analytics/roi")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> CalculateEventROI(
        [FromBody] AnalyticsAgentROIRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _analyticsAgent.CalculateEventROIAsync(
            _currentTenantService.TenantId,
            request.EventId,
            request.TotalCost,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            Data = result.ROICalculation
        });
    }

    /// <summary>
    /// Generate comprehensive analytics report (Analytics Agent - 30 credits)
    /// </summary>
    [HttpPost("analytics/report/{eventId}")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> GenerateEventAnalyticsReport(
        Guid eventId,
        [FromBody] AIAgentRequest? request = null)
    {
        var result = await _analyticsAgent.GenerateEventAnalyticsReportAsync(
            _currentTenantService.TenantId,
            eventId,
            request?.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            Data = result.AnalyticsData
        });
    }

    /// <summary>
    /// Identify event trends (Analytics Agent - 30 credits)
    /// </summary>
    [HttpPost("analytics/trends")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> IdentifyEventTrends(
        [FromBody] AnalyticsAgentTrendsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _analyticsAgent.IdentifyEventTrendsAsync(
            _currentTenantService.TenantId,
            request.StartDate,
            request.EndDate,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens
        });
    }

    #endregion

    #region Budget Agent Endpoints (15-25 credits)

    /// <summary>
    /// Estimate event budget (Budget Agent - 15 credits)
    /// </summary>
    [HttpPost("budget/estimate")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> EstimateEventBudget(
        [FromBody] BudgetAgentEstimateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _budgetAgent.EstimateEventBudgetAsync(
            _currentTenantService.TenantId,
            request.EventType,
            request.ExpectedAttendees,
            request.Location,
            request.Requirements,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            Data = result.BudgetEstimate
        });
    }

    /// <summary>
    /// Compare vendor quotes (Budget Agent - 15 credits)
    /// </summary>
    [HttpPost("budget/compare-vendors")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> CompareVendors(
        [FromBody] BudgetAgentCompareVendorsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var quotes = request.Quotes.Select(q => new VendorQuote
        {
            VendorName = q.VendorName,
            Price = q.Price,
            Description = q.Description,
            Details = q.Details
        }).ToList();

        var result = await _budgetAgent.CompareVendorsAsync(
            _currentTenantService.TenantId,
            request.ServiceType,
            quotes,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            Data = result.VendorComparisons
        });
    }

    /// <summary>
    /// Analyze event expenses (Budget Agent - 15 credits)
    /// </summary>
    [HttpPost("budget/analyze-expenses")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> AnalyzeExpenses(
        [FromBody] BudgetAgentAnalyzeExpensesRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var expenses = request.Expenses.Select(e => new Expense
        {
            Category = e.Category,
            Amount = e.Amount,
            Description = e.Description,
            Date = e.Date
        }).ToList();

        var result = await _budgetAgent.AnalyzeExpensesAsync(
            _currentTenantService.TenantId,
            request.EventId,
            expenses,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            Data = result.ExpenseAnalysis
        });
    }

    /// <summary>
    /// Get budget optimization recommendations (Budget Agent - 25 credits)
    /// </summary>
    [HttpPost("budget/optimize")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> GetBudgetOptimization(
        [FromBody] BudgetAgentOptimizeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _budgetAgent.GetBudgetOptimizationAsync(
            _currentTenantService.TenantId,
            request.EventId,
            request.CurrentBudget,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens
        });
    }

    #endregion

    #region Integration Agent Endpoints (12 credits)

    /// <summary>
    /// Generate calendar invite (Integration Agent - 12 credits)
    /// </summary>
    [HttpPost("integration/calendar-invite")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> GenerateCalendarInvite(
        [FromBody] IntegrationAgentCalendarRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _integrationAgent.GenerateCalendarInviteAsync(
            _currentTenantService.TenantId,
            request.EventId,
            request.CalendarType,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            Data = new { FormattedContent = result.FormattedContent }
        });
    }

    /// <summary>
    /// Prepare CRM sync data (Integration Agent - 12 credits)
    /// </summary>
    [HttpPost("integration/crm-sync")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> PrepareCRMSync(
        [FromBody] IntegrationAgentCRMRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _integrationAgent.PrepareCRMSyncDataAsync(
            _currentTenantService.TenantId,
            request.EventId,
            request.CRMType,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            Data = result.IntegrationData
        });
    }

    /// <summary>
    /// Generate team notifications (Integration Agent - 12 credits)
    /// </summary>
    [HttpPost("integration/team-notification")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> GenerateTeamNotification(
        [FromBody] IntegrationAgentNotificationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _integrationAgent.GenerateTeamNotificationsAsync(
            _currentTenantService.TenantId,
            request.EventId,
            request.Platform,
            request.NotificationType,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            Data = new { FormattedContent = result.FormattedContent }
        });
    }

    /// <summary>
    /// Parse external event data (Integration Agent - 12 credits)
    /// </summary>
    [HttpPost("integration/parse-external")]
    [ProducesResponseType(typeof(AIAgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIAgentResponse>> ParseExternalEventData(
        [FromBody] IntegrationAgentParseRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _integrationAgent.ParseExternalEventDataAsync(
            _currentTenantService.TenantId,
            request.SourceData,
            request.SourceType,
            request.PreferredProvider);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorMessage, result.CreditsCost });
        }

        return Ok(new AIAgentResponse
        {
            Success = result.Success,
            Response = result.Response,
            CreditsCost = result.CreditsCost,
            Provider = result.Provider.ToString(),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            Data = result.IntegrationData
        });
    }

    #endregion

    #region AI Usage History

    /// <summary>
    /// Get AI agent usage history
    /// </summary>
    [HttpGet("usage")]
    [ProducesResponseType(typeof(PagedResult<AIAgentUsageResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AIAgentUsageResponse>>> GetAIUsageHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var tenantId = _currentTenantService.TenantId;

        var query = _context.AIAgentUsages
            .Where(u => u.TenantId == tenantId)
            .OrderByDescending(u => u.ExecutedAt);

        var totalCount = await query.CountAsync();

        var usages = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AIAgentUsageResponse
            {
                Id = u.Id,
                TenantId = u.TenantId,
                AgentType = u.AgentType.ToString(),
                Provider = u.Provider.ToString(),
                CreditsCost = u.CreditsCost,
                InputTokens = u.InputTokens,
                OutputTokens = u.OutputTokens,
                ExecutedAt = u.ExecutedAt,
                PromptSummary = u.PromptInput != null && u.PromptInput.Length > 100
                    ? u.PromptInput.Substring(0, 100) + "..."
                    : u.PromptInput
            })
            .ToListAsync();

        return Ok(new PagedResult<AIAgentUsageResponse>
        {
            Items = usages,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    #endregion
}
