using EventEase.Application.Interfaces;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventEase.Infrastructure.Services.AI.Agents;

/// <summary>
/// Planning Agent - AI-powered event planning and creation (15 credits)
/// </summary>
public class PlanningAgentService : IPlanningAgentService
{
    private const decimal CREDIT_COST = 15m;

    private readonly ApplicationDbContext _context;
    private readonly IOpenAIService _openAI;
    private readonly IAnthropicService _anthropic;
    private readonly ICreditDeductionService _creditDeduction;
    private readonly ILogger<PlanningAgentService> _logger;

    public PlanningAgentService(
        ApplicationDbContext context,
        IOpenAIService openAI,
        IAnthropicService anthropic,
        ICreditDeductionService creditDeduction,
        ILogger<PlanningAgentService> logger)
    {
        _context = context;
        _openAI = openAI;
        _anthropic = anthropic;
        _creditDeduction = creditDeduction;
        _logger = logger;
    }

    public async Task<PlanningAgentResult> CreateEventFromDescriptionAsync(
        Guid tenantId,
        string description,
        AIProvider? preferredProvider = null)
    {
        // Check credits
        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, CREDIT_COST))
        {
            return new PlanningAgentResult
            {
                Success = false,
                ErrorMessage = $"Insufficient credits. Required: {CREDIT_COST}",
                CreditsCost = CREDIT_COST
            };
        }

        try
        {
            var provider = preferredProvider ?? AIProvider.OpenAI;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var systemPrompt = @"You are an expert event planner. Given a natural language description of an event, extract and structure the event details into a JSON format with the following fields:
- name: event name
- description: detailed description
- category: event category (Conference, Workshop, Seminar, Networking, etc.)
- startDate: ISO 8601 date-time
- endDate: ISO 8601 date-time
- location: location name
- venue: venue name
- isVirtual: boolean
- virtualMeetingUrl: if virtual
- maxAttendees: estimated number
- isFree: boolean
- price: if not free
- currency: default EUR
- tags: array of relevant tags
- requiresApproval: boolean (based on event type)
- allowWaitlist: boolean

Provide realistic suggestions and fill in reasonable defaults where information is missing.";

            var userPrompt = $"Create a structured event from this description:\n\n{description}";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.7, 1500);

            if (!response.Success)
            {
                return new PlanningAgentResult
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage,
                    Provider = provider,
                    CreditsCost = CREDIT_COST
                };
            }

            // Deduct credits
            await _creditDeduction.DeductCreditsAsync(
                tenantId,
                CREDIT_COST,
                AIAgentType.PlanningAgent,
                provider,
                "Create event from natural language description",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: description,
                agentResponse: response.Content
            );

            _logger.LogInformation("Planning Agent: Created event structure for tenant {TenantId} using {Provider}",
                tenantId, provider);

            return new PlanningAgentResult
            {
                Success = true,
                Response = response.Content,
                CreditsCost = CREDIT_COST,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Planning Agent for tenant {TenantId}", tenantId);

            return new PlanningAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = CREDIT_COST
            };
        }
    }

    public async Task<PlanningAgentResult> GetVenueRecommendationsAsync(
        Guid tenantId,
        string eventType,
        int expectedAttendees,
        string? location = null,
        decimal? budget = null,
        AIProvider? preferredProvider = null)
    {
        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, CREDIT_COST))
        {
            return new PlanningAgentResult
            {
                Success = false,
                ErrorMessage = $"Insufficient credits. Required: {CREDIT_COST}",
                CreditsCost = CREDIT_COST
            };
        }

        try
        {
            var provider = preferredProvider ?? AIProvider.Anthropic;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var systemPrompt = @"You are an expert venue consultant. Provide venue recommendations based on event requirements. Consider:
- Capacity and space requirements
- Event type and atmosphere
- Location accessibility
- Budget constraints
- Amenities needed (AV equipment, catering, parking, etc.)
- Backup options

Provide 3-5 venue recommendations with rationale for each.";

            var userPrompt = $@"Recommend venues for:
Event Type: {eventType}
Expected Attendees: {expectedAttendees}
Location: {location ?? "flexible"}
Budget: {(budget.HasValue ? $"€{budget.Value}" : "not specified")}

Provide specific venue types, features to look for, and considerations.";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.7, 1500);

            if (!response.Success)
            {
                return new PlanningAgentResult
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage,
                    Provider = provider,
                    CreditsCost = CREDIT_COST
                };
            }

            await _creditDeduction.DeductCreditsAsync(
                tenantId,
                CREDIT_COST,
                AIAgentType.PlanningAgent,
                provider,
                $"Get venue recommendations for {eventType}",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: userPrompt,
                agentResponse: response.Content
            );

            return new PlanningAgentResult
            {
                Success = true,
                Response = response.Content,
                CreditsCost = CREDIT_COST,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting venue recommendations for tenant {TenantId}", tenantId);

            return new PlanningAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = CREDIT_COST
            };
        }
    }

    public async Task<PlanningAgentResult> OptimizeEventScheduleAsync(
        Guid tenantId,
        Guid eventId,
        string requirements,
        AIProvider? preferredProvider = null)
    {
        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, CREDIT_COST))
        {
            return new PlanningAgentResult
            {
                Success = false,
                ErrorMessage = $"Insufficient credits. Required: {CREDIT_COST}",
                CreditsCost = CREDIT_COST
            };
        }

        try
        {
            var eventEntity = await _context.Events.FindAsync(eventId);

            if (eventEntity == null)
            {
                return new PlanningAgentResult
                {
                    Success = false,
                    ErrorMessage = "Event not found",
                    CreditsCost = CREDIT_COST
                };
            }

            var provider = preferredProvider ?? AIProvider.OpenAI;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var systemPrompt = @"You are an expert event scheduler. Optimize the event schedule/agenda considering:
- Optimal session timing and flow
- Attendee energy levels throughout the day
- Break scheduling
- Networking opportunities
- Travel time between sessions (if multi-track)
- Speaker availability
- Engagement peaks and valleys

Provide a detailed, optimized schedule with rationale.";

            var userPrompt = $@"Optimize the schedule for:
Event: {eventEntity.Name}
Type: {eventEntity.Category}
Duration: {eventEntity.StartDate:yyyy-MM-dd HH:mm} to {eventEntity.EndDate:yyyy-MM-dd HH:mm}
Expected Attendees: {eventEntity.MaxAttendees ?? 100}

Requirements:
{requirements}

Provide an optimized agenda with time blocks and recommendations.";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.7, 2000);

            if (!response.Success)
            {
                return new PlanningAgentResult
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage,
                    Provider = provider,
                    CreditsCost = CREDIT_COST
                };
            }

            await _creditDeduction.DeductCreditsAsync(
                tenantId,
                CREDIT_COST,
                AIAgentType.PlanningAgent,
                provider,
                $"Optimize schedule for event: {eventEntity.Name}",
                relatedEntityId: eventId,
                relatedEntityType: "Event",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: userPrompt,
                agentResponse: response.Content
            );

            return new PlanningAgentResult
            {
                Success = true,
                Response = response.Content,
                CreditsCost = CREDIT_COST,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing schedule for event {EventId}", eventId);

            return new PlanningAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = CREDIT_COST
            };
        }
    }

    public async Task<PlanningAgentResult> GenerateEventContentAsync(
        Guid tenantId,
        string eventName,
        string eventType,
        string targetAudience,
        AIProvider? preferredProvider = null)
    {
        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, CREDIT_COST))
        {
            return new PlanningAgentResult
            {
                Success = false,
                ErrorMessage = $"Insufficient credits. Required: {CREDIT_COST}",
                CreditsCost = CREDIT_COST
            };
        }

        try
        {
            var provider = preferredProvider ?? AIProvider.Anthropic;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var systemPrompt = @"You are an expert marketing copywriter specializing in event promotion. Create compelling event content including:
- Engaging event description
- Key highlights and benefits
- Call-to-action
- Social media posts (short versions)
- Email subject lines
- SEO-friendly keywords

Make it professional, exciting, and tailored to the target audience.";

            var userPrompt = $@"Generate marketing content for:
Event Name: {eventName}
Event Type: {eventType}
Target Audience: {targetAudience}

Provide:
1. Long-form description (150-200 words)
2. Short description (50 words)
3. 3 social media posts
4. 3 email subject line options
5. Key selling points
6. Suggested hashtags";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.8, 1500);

            if (!response.Success)
            {
                return new PlanningAgentResult
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage,
                    Provider = provider,
                    CreditsCost = CREDIT_COST
                };
            }

            await _creditDeduction.DeductCreditsAsync(
                tenantId,
                CREDIT_COST,
                AIAgentType.PlanningAgent,
                provider,
                $"Generate marketing content for: {eventName}",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: userPrompt,
                agentResponse: response.Content
            );

            return new PlanningAgentResult
            {
                Success = true,
                Response = response.Content,
                CreditsCost = CREDIT_COST,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating event content for tenant {TenantId}", tenantId);

            return new PlanningAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = CREDIT_COST
            };
        }
    }
}
