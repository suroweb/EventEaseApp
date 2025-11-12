using EventEase.Application.Interfaces;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventEase.Infrastructure.Services.AI.Agents;

/// <summary>
/// Analytics Agent - Predictive analytics and insights (20-30 credits)
/// </summary>
public class AnalyticsAgentService : IAnalyticsAgentService
{
    private const decimal BASIC_ANALYTICS_COST = 20m;
    private const decimal ADVANCED_ANALYTICS_COST = 30m;

    private readonly ApplicationDbContext _context;
    private readonly IOpenAIService _openAI;
    private readonly IAnthropicService _anthropic;
    private readonly ICreditDeductionService _creditDeduction;
    private readonly ILogger<AnalyticsAgentService> _logger;

    public AnalyticsAgentService(
        ApplicationDbContext context,
        IOpenAIService openAI,
        IAnthropicService anthropic,
        ICreditDeductionService creditDeduction,
        ILogger<AnalyticsAgentService> logger)
    {
        _context = context;
        _openAI = openAI;
        _anthropic = anthropic;
        _creditDeduction = creditDeduction;
        _logger = logger;
    }

    public async Task<AnalyticsAgentResult> PredictEventAttendanceAsync(
        Guid tenantId,
        Guid eventId,
        AIProvider? preferredProvider = null)
    {
        var cost = BASIC_ANALYTICS_COST;

        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, cost))
        {
            return new AnalyticsAgentResult
            {
                Success = false,
                ErrorMessage = $"Insufficient credits. Required: {cost}",
                CreditsCost = cost
            };
        }

        try
        {
            var eventEntity = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventEntity == null)
            {
                return new AnalyticsAgentResult
                {
                    Success = false,
                    ErrorMessage = "Event not found",
                    CreditsCost = cost
                };
            }

            var registrations = eventEntity.Registrations.ToList();
            var confirmedCount = registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
            var waitlistedCount = registrations.Count(r => r.Status == RegistrationStatus.Waitlisted);
            var pendingCount = registrations.Count(r => r.Status == RegistrationStatus.Pending);

            // Get historical data for similar events
            var similarEvents = await _context.Events
                .Where(e => e.TenantId == tenantId &&
                            e.Category == eventEntity.Category &&
                            e.Status == EventStatus.Completed &&
                            e.StartDate < DateTime.UtcNow)
                .Include(e => e.Registrations)
                .Take(10)
                .Select(e => new
                {
                    e.Name,
                    RegisteredCount = e.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed),
                    AttendedCount = e.Registrations.Count(r => r.Status == RegistrationStatus.CheckedIn)
                })
                .ToListAsync();

            var provider = preferredProvider ?? AIProvider.OpenAI;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var historicalDataJson = JsonSerializer.Serialize(similarEvents);

            var systemPrompt = @"You are a predictive analytics expert specializing in event attendance forecasting. Predict actual attendance based on:
- Current registration counts
- Historical attendance rates for similar events
- Event type and timing
- Registration trends
- No-show patterns

Return JSON with: predictedAttendees, confidence, factors, recommendations.";

            var userPrompt = $@"Predict attendance for:
Event: {eventEntity.Name}
Category: {eventEntity.Category}
Date: {eventEntity.StartDate:yyyy-MM-dd}
Current Registrations:
- Confirmed: {confirmedCount}
- Pending: {pendingCount}
- Waitlisted: {waitlistedCount}
Max Capacity: {eventEntity.MaxAttendees}

Historical Similar Events:
{historicalDataJson}

Provide: predicted attendees, confidence level, key factors, and recommendations.";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.5, 1000);

            if (!response.Success)
            {
                return new AnalyticsAgentResult
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage,
                    Provider = provider,
                    CreditsCost = cost
                };
            }

            await _creditDeduction.DeductCreditsAsync(
                tenantId,
                cost,
                AIAgentType.AnalyticsAgent,
                provider,
                $"Predict attendance for event: {eventEntity.Name}",
                relatedEntityId: eventId,
                relatedEntityType: "Event",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: userPrompt,
                agentResponse: response.Content
            );

            return new AnalyticsAgentResult
            {
                Success = true,
                Response = response.Content,
                CreditsCost = cost,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting attendance for event {EventId}", eventId);

            return new AnalyticsAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = cost
            };
        }
    }

    public async Task<AnalyticsAgentResult> AnalyzeEventSentimentAsync(
        Guid tenantId,
        Guid eventId,
        List<string> feedbackTexts,
        AIProvider? preferredProvider = null)
    {
        var cost = BASIC_ANALYTICS_COST;

        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, cost))
        {
            return new AnalyticsAgentResult
            {
                Success = false,
                ErrorMessage = $"Insufficient credits. Required: {cost}",
                CreditsCost = cost
            };
        }

        try
        {
            var eventEntity = await _context.Events.FindAsync(eventId);

            if (eventEntity == null)
            {
                return new AnalyticsAgentResult
                {
                    Success = false,
                    ErrorMessage = "Event not found",
                    CreditsCost = cost
                };
            }

            var provider = preferredProvider ?? AIProvider.OpenAI;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var feedbackJson = JsonSerializer.Serialize(feedbackTexts);

            var systemPrompt = @"You are a sentiment analysis expert. Analyze event feedback and provide:
- Overall sentiment (Positive/Neutral/Negative)
- Overall score (-1.0 to 1.0)
- Positive/Neutral/Negative counts
- Key themes and topics
- Highlights (best aspects)
- Concerns (areas for improvement)
- Actionable recommendations

Return structured JSON.";

            var userPrompt = $@"Analyze feedback for:
Event: {eventEntity.Name}

Feedback ({feedbackTexts.Count} responses):
{feedbackJson}

Provide comprehensive sentiment analysis with themes and recommendations.";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.3, 2000);

            if (!response.Success)
            {
                return new AnalyticsAgentResult
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage,
                    Provider = provider,
                    CreditsCost = cost
                };
            }

            await _creditDeduction.DeductCreditsAsync(
                tenantId,
                cost,
                AIAgentType.AnalyticsAgent,
                provider,
                $"Analyze sentiment for event: {eventEntity.Name}",
                relatedEntityId: eventId,
                relatedEntityType: "Event",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: $"Analyze {feedbackTexts.Count} feedback responses",
                agentResponse: response.Content
            );

            return new AnalyticsAgentResult
            {
                Success = true,
                Response = response.Content,
                CreditsCost = cost,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing sentiment for event {EventId}", eventId);

            return new AnalyticsAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = cost
            };
        }
    }

    public async Task<AnalyticsAgentResult> CalculateEventROIAsync(
        Guid tenantId,
        Guid eventId,
        decimal totalCost,
        AIProvider? preferredProvider = null)
    {
        var cost = BASIC_ANALYTICS_COST;

        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, cost))
        {
            return new AnalyticsAgentResult
            {
                Success = false,
                ErrorMessage = $"Insufficient credits. Required: {cost}",
                CreditsCost = cost
            };
        }

        try
        {
            var eventEntity = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventEntity == null)
            {
                return new AnalyticsAgentResult
                {
                    Success = false,
                    ErrorMessage = "Event not found",
                    CreditsCost = cost
                };
            }

            var totalRevenue = eventEntity.Registrations.Sum(r => r.TotalAmount);
            var attendeeCount = eventEntity.Registrations.Count(r => r.Status == RegistrationStatus.CheckedIn);

            var provider = preferredProvider ?? AIProvider.Anthropic;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var systemPrompt = @"You are an event ROI analyst. Calculate and explain event ROI considering:
- Direct revenue (ticket sales)
- Direct costs
- Intangible benefits (networking, brand awareness, leads generated)
- Cost per attendee
- Recommendations for improvement

Return JSON with: ROI percentage, net value, cost breakdown, intangible value, recommendations.";

            var userPrompt = $@"Calculate ROI for:
Event: {eventEntity.Name}
Total Cost: €{totalCost}
Total Revenue: €{totalRevenue}
Attendees: {attendeeCount}
Registrations: {eventEntity.Registrations.Count}

Consider both financial and intangible returns. Provide actionable recommendations.";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.5, 1500);

            if (!response.Success)
            {
                return new AnalyticsAgentResult
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage,
                    Provider = provider,
                    CreditsCost = cost
                };
            }

            await _creditDeduction.DeductCreditsAsync(
                tenantId,
                cost,
                AIAgentType.AnalyticsAgent,
                provider,
                $"Calculate ROI for event: {eventEntity.Name}",
                relatedEntityId: eventId,
                relatedEntityType: "Event",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: userPrompt,
                agentResponse: response.Content
            );

            return new AnalyticsAgentResult
            {
                Success = true,
                Response = response.Content,
                CreditsCost = cost,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating ROI for event {EventId}", eventId);

            return new AnalyticsAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = cost
            };
        }
    }

    public async Task<AnalyticsAgentResult> GenerateEventAnalyticsReportAsync(
        Guid tenantId,
        Guid eventId,
        AIProvider? preferredProvider = null)
    {
        var cost = ADVANCED_ANALYTICS_COST;

        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, cost))
        {
            return new AnalyticsAgentResult
            {
                Success = false,
                ErrorMessage = $"Insufficient credits. Required: {cost}",
                CreditsCost = cost
            };
        }

        try
        {
            var eventEntity = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventEntity == null)
            {
                return new AnalyticsAgentResult
                {
                    Success = false,
                    ErrorMessage = "Event not found",
                    CreditsCost = cost
                };
            }

            var registrations = eventEntity.Registrations.ToList();

            var analyticsData = new
            {
                eventEntity.Name,
                eventEntity.Category,
                eventEntity.StartDate,
                eventEntity.EndDate,
                TotalRegistrations = registrations.Count,
                Confirmed = registrations.Count(r => r.Status == RegistrationStatus.Confirmed),
                CheckedIn = registrations.Count(r => r.Status == RegistrationStatus.CheckedIn),
                Cancelled = registrations.Count(r => r.Status == RegistrationStatus.Cancelled),
                Waitlisted = registrations.Count(r => r.Status == RegistrationStatus.Waitlisted),
                TotalRevenue = registrations.Sum(r => r.TotalAmount),
                AverageTicketPrice = registrations.Any() ? registrations.Average(r => r.TotalAmount) : 0,
                AttendanceRate = registrations.Any() ? (double)registrations.Count(r => r.Status == RegistrationStatus.CheckedIn) / registrations.Count * 100 : 0,
                RegistrationsBySource = registrations.GroupBy(r => r.RegistrationSource).Select(g => new { Source = g.Key, Count = g.Count() }),
                RegistrationTrend = registrations.GroupBy(r => r.RegisteredAt.Date).OrderBy(g => g.Key).Select(g => new { Date = g.Key, Count = g.Count() })
            };

            var provider = preferredProvider ?? AIProvider.Anthropic;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var dataJson = JsonSerializer.Serialize(analyticsData);

            var systemPrompt = @"You are a senior event analytics consultant. Generate a comprehensive event analytics report including:
- Executive summary
- Key performance metrics
- Registration analysis
- Attendance patterns
- Revenue analysis
- Demographic insights
- Success factors
- Areas for improvement
- Actionable recommendations
- Comparison to industry benchmarks

Make it professional, data-driven, and actionable.";

            var userPrompt = $@"Generate comprehensive analytics report for:

Event Data:
{dataJson}

Provide a detailed, executive-level report with insights and recommendations.";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.7, 3000);

            if (!response.Success)
            {
                return new AnalyticsAgentResult
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage,
                    Provider = provider,
                    CreditsCost = cost
                };
            }

            await _creditDeduction.DeductCreditsAsync(
                tenantId,
                cost,
                AIAgentType.AnalyticsAgent,
                provider,
                $"Generate comprehensive analytics report for: {eventEntity.Name}",
                relatedEntityId: eventId,
                relatedEntityType: "Event",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: "Generate comprehensive analytics report",
                agentResponse: response.Content
            );

            return new AnalyticsAgentResult
            {
                Success = true,
                Response = response.Content,
                CreditsCost = cost,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating analytics report for event {EventId}", eventId);

            return new AnalyticsAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = cost
            };
        }
    }

    public async Task<AnalyticsAgentResult> IdentifyEventTrendsAsync(
        Guid tenantId,
        DateTime startDate,
        DateTime endDate,
        AIProvider? preferredProvider = null)
    {
        var cost = ADVANCED_ANALYTICS_COST;

        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, cost))
        {
            return new AnalyticsAgentResult
            {
                Success = false,
                ErrorMessage = $"Insufficient credits. Required: {cost}",
                CreditsCost = cost
            };
        }

        try
        {
            var events = await _context.Events
                .Where(e => e.TenantId == tenantId && e.StartDate >= startDate && e.StartDate <= endDate)
                .Include(e => e.Registrations)
                .ToListAsync();

            var trendsData = events.Select(e => new
            {
                e.Name,
                e.Category,
                e.StartDate,
                e.MaxAttendees,
                RegistrationCount = e.Registrations.Count,
                AttendanceCount = e.Registrations.Count(r => r.Status == RegistrationStatus.CheckedIn),
                Revenue = e.Registrations.Sum(r => r.TotalAmount),
                e.IsFree,
                e.IsVirtual
            }).ToList();

            var provider = preferredProvider ?? AIProvider.OpenAI;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var dataJson = JsonSerializer.Serialize(trendsData);

            var systemPrompt = @"You are a market trends analyst specializing in events. Identify patterns and trends including:
- Popular event categories
- Attendance patterns over time
- Virtual vs in-person preferences
- Pricing trends
- Seasonal patterns
- Growth opportunities
- Emerging formats

Provide actionable insights and recommendations.";

            var userPrompt = $@"Identify trends from {events.Count} events between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}:

Event Data:
{dataJson}

Provide comprehensive trend analysis with insights and recommendations for future events.";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.7, 2500);

            if (!response.Success)
            {
                return new AnalyticsAgentResult
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage,
                    Provider = provider,
                    CreditsCost = cost
                };
            }

            await _creditDeduction.DeductCreditsAsync(
                tenantId,
                cost,
                AIAgentType.AnalyticsAgent,
                provider,
                $"Identify event trends for period {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: $"Analyze trends for {events.Count} events",
                agentResponse: response.Content
            );

            return new AnalyticsAgentResult
            {
                Success = true,
                Response = response.Content,
                CreditsCost = cost,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error identifying event trends for tenant {TenantId}", tenantId);

            return new AnalyticsAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = cost
            };
        }
    }
}
