using EventEase.Application.Interfaces;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventEase.Infrastructure.Services.AI.Agents;

/// <summary>
/// Integration Agent - Third-party integrations and automation (variable cost)
/// </summary>
public class IntegrationAgentService : IIntegrationAgentService
{
    private const decimal INTEGRATION_COST = 12m;

    private readonly ApplicationDbContext _context;
    private readonly IOpenAIService _openAI;
    private readonly IAnthropicService _anthropic;
    private readonly ICreditDeductionService _creditDeduction;
    private readonly ILogger<IntegrationAgentService> _logger;

    public IntegrationAgentService(
        ApplicationDbContext context,
        IOpenAIService openAI,
        IAnthropicService anthropic,
        ICreditDeductionService creditDeduction,
        ILogger<IntegrationAgentService> logger)
    {
        _context = context;
        _openAI = openAI;
        _anthropic = anthropic;
        _creditDeduction = creditDeduction;
        _logger = logger;
    }

    public async Task<IntegrationAgentResult> GenerateCalendarInviteAsync(
        Guid tenantId,
        Guid eventId,
        string calendarType,
        AIProvider? preferredProvider = null)
    {
        var cost = INTEGRATION_COST;

        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, cost))
        {
            return new IntegrationAgentResult
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
                return new IntegrationAgentResult
                {
                    Success = false,
                    ErrorMessage = "Event not found",
                    CreditsCost = cost
                };
            }

            var provider = preferredProvider ?? AIProvider.OpenAI;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var systemPrompt = $@"You are a calendar integration expert. Generate properly formatted calendar invite content for {calendarType} including:
- Event title
- Date and time (with timezone)
- Location (physical or virtual)
- Description
- RSVP instructions
- Contact information
- Calendar-specific formatting

Format the output appropriately for {calendarType} (.ics format details).";

            var userPrompt = $@"Generate {calendarType} calendar invite for:
Event: {eventEntity.Name}
Description: {eventEntity.Description}
Start: {eventEntity.StartDate:yyyy-MM-dd HH:mm} ({eventEntity.TimeZone ?? "UTC"})
End: {eventEntity.EndDate:yyyy-MM-dd HH:mm}
Location: {(eventEntity.IsVirtual ? $"Virtual: {eventEntity.VirtualMeetingUrl}" : $"{eventEntity.Venue}, {eventEntity.VenueAddress}")}

Provide formatted calendar invite content ready for {calendarType}.";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.3, 1000);

            if (!response.Success)
            {
                return new IntegrationAgentResult
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
                AIAgentType.IntegrationAgent,
                provider,
                $"Generate {calendarType} invite for event: {eventEntity.Name}",
                relatedEntityId: eventId,
                relatedEntityType: "Event",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: userPrompt,
                agentResponse: response.Content
            );

            return new IntegrationAgentResult
            {
                Success = true,
                Response = response.Content,
                FormattedContent = response.Content,
                CreditsCost = cost,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating calendar invite for event {EventId}", eventId);

            return new IntegrationAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = cost
            };
        }
    }

    public async Task<IntegrationAgentResult> PrepareCRMSyncDataAsync(
        Guid tenantId,
        Guid eventId,
        string crmType,
        AIProvider? preferredProvider = null)
    {
        var cost = INTEGRATION_COST;

        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, cost))
        {
            return new IntegrationAgentResult
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
                return new IntegrationAgentResult
                {
                    Success = false,
                    ErrorMessage = "Event not found",
                    CreditsCost = cost
                };
            }

            var provider = preferredProvider ?? AIProvider.Anthropic;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var registrationData = eventEntity.Registrations.Take(10).Select(r => new
            {
                r.FirstName,
                r.LastName,
                r.Email,
                r.Company,
                r.JobTitle,
                r.Status
            });

            var dataJson = JsonSerializer.Serialize(registrationData);

            var systemPrompt = $@"You are a CRM integration specialist. Prepare event data for {crmType} sync including:
- Field mapping recommendations
- Data transformation rules
- Contact/lead creation strategy
- Activity logging format
- Custom field suggestions
- Deduplication strategy
- Sync frequency recommendations

Provide {crmType}-specific guidance.";

            var userPrompt = $@"Prepare CRM sync for {crmType}:
Event: {eventEntity.Name}
Registrations: {eventEntity.Registrations.Count}

Sample Registration Data:
{dataJson}

Provide:
- {crmType} field mapping
- Data transformation rules
- API integration points
- Sync strategy
- Best practices";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.5, 1500);

            if (!response.Success)
            {
                return new IntegrationAgentResult
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
                AIAgentType.IntegrationAgent,
                provider,
                $"Prepare {crmType} sync for event: {eventEntity.Name}",
                relatedEntityId: eventId,
                relatedEntityType: "Event",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: $"Prepare {crmType} sync data",
                agentResponse: response.Content
            );

            return new IntegrationAgentResult
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
            _logger.LogError(ex, "Error preparing CRM sync for event {EventId}", eventId);

            return new IntegrationAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = cost
            };
        }
    }

    public async Task<IntegrationAgentResult> GenerateTeamNotificationsAsync(
        Guid tenantId,
        Guid eventId,
        string platform,
        string notificationType,
        AIProvider? preferredProvider = null)
    {
        var cost = INTEGRATION_COST;

        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, cost))
        {
            return new IntegrationAgentResult
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
                return new IntegrationAgentResult
                {
                    Success = false,
                    ErrorMessage = "Event not found",
                    CreditsCost = cost
                };
            }

            var provider = preferredProvider ?? AIProvider.Anthropic;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var systemPrompt = $@"You are a team collaboration expert. Generate {platform} notification messages that are:
- Clear and concise
- Action-oriented when needed
- Properly formatted for {platform}
- Include relevant emojis (appropriate for professional context)
- Use {platform}-specific features (mentions, threads, etc.)

Adapt tone and content based on notification type: {notificationType}";

            var userPrompt = $@"Generate {platform} notification for:
Type: {notificationType}
Event: {eventEntity.Name}
Date: {eventEntity.StartDate:MMMM d, yyyy at h:mm tt}
Registrations: {eventEntity.Registrations.Count}/{eventEntity.MaxAttendees}
Status: {eventEntity.Status}

Create engaging {platform} message formatted correctly with relevant details.";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.7, 500);

            if (!response.Success)
            {
                return new IntegrationAgentResult
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
                AIAgentType.IntegrationAgent,
                provider,
                $"Generate {platform} {notificationType} notification for: {eventEntity.Name}",
                relatedEntityId: eventId,
                relatedEntityType: "Event",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: userPrompt,
                agentResponse: response.Content
            );

            return new IntegrationAgentResult
            {
                Success = true,
                Response = response.Content,
                FormattedContent = response.Content,
                CreditsCost = cost,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating team notification for event {EventId}", eventId);

            return new IntegrationAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = cost
            };
        }
    }

    public async Task<IntegrationAgentResult> ParseExternalEventDataAsync(
        Guid tenantId,
        string sourceData,
        string sourceType,
        AIProvider? preferredProvider = null)
    {
        var cost = INTEGRATION_COST;

        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, cost))
        {
            return new IntegrationAgentResult
            {
                Success = false,
                ErrorMessage = $"Insufficient credits. Required: {cost}",
                CreditsCost = cost
            };
        }

        try
        {
            var provider = preferredProvider ?? AIProvider.OpenAI;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var systemPrompt = @"You are a data extraction expert. Parse and structure event information from unstructured sources. Extract:
- Event name
- Date and time
- Location/venue
- Description
- Organizer
- Registration details
- Contact information
- Any other relevant details

Return structured JSON that can be used to create an event.";

            var userPrompt = $@"Parse event data from {sourceType}:

Source Data:
{sourceData}

Extract and structure all event information into JSON format compatible with event creation.";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.3, 1500);

            if (!response.Success)
            {
                return new IntegrationAgentResult
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
                AIAgentType.IntegrationAgent,
                provider,
                $"Parse event data from {sourceType}",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput = $"Parse {sourceType} data",
                agentResponse: response.Content
            );

            return new IntegrationAgentResult
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
            _logger.LogError(ex, "Error parsing external event data for tenant {TenantId}", tenantId);

            return new IntegrationAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = cost
            };
        }
    }
}
