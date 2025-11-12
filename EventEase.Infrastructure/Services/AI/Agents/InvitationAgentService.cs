using EventEase.Application.Interfaces;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventEase.Infrastructure.Services.AI.Agents;

/// <summary>
/// Invitation Agent - ML-based guest selection and personalized invitations (10 + 0.5/guest credits)
/// </summary>
public class InvitationAgentService : IInvitationAgentService
{
    private const decimal BASE_CREDIT_COST = 10m;
    private const decimal CREDIT_PER_GUEST = 0.5m;

    private readonly ApplicationDbContext _context;
    private readonly IOpenAIService _openAI;
    private readonly IAnthropicService _anthropic;
    private readonly ICreditDeductionService _creditDeduction;
    private readonly ILogger<InvitationAgentService> _logger;

    public InvitationAgentService(
        ApplicationDbContext context,
        IOpenAIService openAI,
        IAnthropicService anthropic,
        ICreditDeductionService creditDeduction,
        ILogger<InvitationAgentService> logger)
    {
        _context = context;
        _openAI = openAI;
        _anthropic = anthropic;
        _creditDeduction = creditDeduction;
        _logger = logger;
    }

    public async Task<InvitationAgentResult> SelectGuestsForEventAsync(
        Guid tenantId,
        Guid eventId,
        int maxGuests,
        string? selectionCriteria = null,
        AIProvider? preferredProvider = null)
    {
        var totalCost = BASE_CREDIT_COST + (maxGuests * CREDIT_PER_GUEST);

        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, totalCost))
        {
            return new InvitationAgentResult
            {
                Success = false,
                ErrorMessage = $"Insufficient credits. Required: {totalCost}",
                CreditsCost = totalCost
            };
        }

        try
        {
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity == null)
            {
                return new InvitationAgentResult
                {
                    Success = false,
                    ErrorMessage = "Event not found",
                    CreditsCost = totalCost
                };
            }

            // Get all active guests for tenant
            var guests = await _context.Guests
                .Where(g => g.TenantId == tenantId && g.IsActive)
                .ToListAsync();

            // Get historical invitation data
            var invitations = await _context.EventInvitations
                .Where(i => i.TenantId == tenantId)
                .ToListAsync();

            var provider = preferredProvider ?? AIProvider.OpenAI;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var guestData = guests.Select(g => new
            {
                g.Id,
                g.FirstName,
                g.LastName,
                g.Email,
                g.Company,
                g.JobTitle,
                g.Industry,
                EngagementScore = g.EngagementScore,
                TotalInvitations = invitations.Count(i => i.GuestId == g.Id),
                TotalAttended = invitations.Count(i => i.GuestId == g.Id && i.AttendedEvent)
            }).Take(100); // Limit to avoid token overflow

            var guestDataJson = JsonSerializer.Serialize(guestData);

            var systemPrompt = @"You are an ML-powered guest selection expert. Analyze the provided guest data and select the most suitable guests for the event based on:
- Engagement score and past attendance
- Industry and job title relevance
- Company alignment with event type
- Attendance patterns
- Custom selection criteria

Return a JSON array of selected guest IDs with scores and reasons.";

            var userPrompt = $@"Select the best {maxGuests} guests for this event:
Event: {eventEntity.Name}
Type: {eventEntity.Category}
Description: {eventEntity.Description}

Selection Criteria: {selectionCriteria ?? "Maximize attendance likelihood and engagement"}

Available Guests (limited sample):
{guestDataJson}

Return JSON array with: guestId, score (0-1), reason, predictedAttendanceRate";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.5, 2000);

            if (!response.Success)
            {
                return new InvitationAgentResult
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage,
                    Provider = provider,
                    CreditsCost = totalCost
                };
            }

            await _creditDeduction.DeductCreditsAsync(
                tenantId,
                totalCost,
                AIAgentType.InvitationAgent,
                provider,
                $"Select {maxGuests} guests for event: {eventEntity.Name}",
                relatedEntityId: eventId,
                relatedEntityType: "Event",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: $"Select {maxGuests} guests for {eventEntity.Name}",
                agentResponse: response.Content
            );

            return new InvitationAgentResult
            {
                Success = true,
                Response = response.Content,
                CreditsCost = totalCost,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting guests for event {EventId}", eventId);

            return new InvitationAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = totalCost
            };
        }
    }

    public async Task<InvitationAgentResult> GeneratePersonalizedInvitationsAsync(
        Guid tenantId,
        Guid eventId,
        List<Guid> guestIds,
        string? customMessage = null,
        AIProvider? preferredProvider = null)
    {
        var totalCost = BASE_CREDIT_COST + (guestIds.Count * CREDIT_PER_GUEST);

        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, totalCost))
        {
            return new InvitationAgentResult
            {
                Success = false,
                ErrorMessage = $"Insufficient credits. Required: {totalCost}",
                CreditsCost = totalCost
            };
        }

        try
        {
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity == null)
            {
                return new InvitationAgentResult
                {
                    Success = false,
                    ErrorMessage = "Event not found",
                    CreditsCost = totalCost
                };
            }

            var guests = await _context.Guests
                .Where(g => guestIds.Contains(g.Id))
                .ToListAsync();

            var provider = preferredProvider ?? AIProvider.Anthropic;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var guestInfo = guests.Select(g => new
            {
                g.Id,
                g.FirstName,
                g.LastName,
                g.Company,
                g.JobTitle,
                g.Industry
            });

            var guestDataJson = JsonSerializer.Serialize(guestInfo);

            var systemPrompt = @"You are an expert at writing personalized event invitations. Create unique, engaging invitation messages for each guest that:
- Address them by name
- Reference their company/role/industry when relevant
- Highlight event aspects most relevant to them
- Use a warm, professional tone
- Include a clear call-to-action
- Are concise (2-3 paragraphs)

Return a JSON object mapping guest IDs to their personalized messages.";

            var userPrompt = $@"Generate personalized invitations for:
Event: {eventEntity.Name}
Description: {eventEntity.Description}
Date: {eventEntity.StartDate:MMMM d, yyyy}
{(eventEntity.IsVirtual ? "Format: Virtual Event" : $"Venue: {eventEntity.Venue}")}

{(string.IsNullOrEmpty(customMessage) ? "" : $"Custom message to incorporate: {customMessage}")}

Guests:
{guestDataJson}

Return JSON: {{""guestId"": ""personalized message""}}";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.7, 3000);

            if (!response.Success)
            {
                return new InvitationAgentResult
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage,
                    Provider = provider,
                    CreditsCost = totalCost
                };
            }

            await _creditDeduction.DeductCreditsAsync(
                tenantId,
                totalCost,
                AIAgentType.InvitationAgent,
                provider,
                $"Generate {guestIds.Count} personalized invitations for: {eventEntity.Name}",
                relatedEntityId: eventId,
                relatedEntityType: "Event",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: $"Personalize invitations for {guestIds.Count} guests",
                agentResponse: response.Content
            );

            return new InvitationAgentResult
            {
                Success = true,
                Response = response.Content,
                CreditsCost = totalCost,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating personalized invitations for event {EventId}", eventId);

            return new InvitationAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = totalCost
            };
        }
    }

    public async Task<InvitationAgentResult> DetermineOptimalSendTimeAsync(
        Guid tenantId,
        Guid eventId,
        List<Guid> guestIds,
        AIProvider? preferredProvider = null)
    {
        var totalCost = BASE_CREDIT_COST + (guestIds.Count * CREDIT_PER_GUEST);

        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, totalCost))
        {
            return new InvitationAgentResult
            {
                Success = false,
                ErrorMessage = $"Insufficient credits. Required: {totalCost}",
                CreditsCost = totalCost
            };
        }

        try
        {
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity == null)
            {
                return new InvitationAgentResult
                {
                    Success = false,
                    ErrorMessage = "Event not found",
                    CreditsCost = totalCost
                };
            }

            // Get historical invitation data for these guests
            var historicalData = await _context.EventInvitations
                .Where(i => guestIds.Contains(i.GuestId) && i.TenantId == tenantId)
                .Select(i => new
                {
                    i.GuestId,
                    i.InvitedAt,
                    i.RsvpStatus,
                    ResponseTime = i.RespondedAt.HasValue ? (i.RespondedAt.Value - i.InvitedAt).TotalHours : (double?)null
                })
                .ToListAsync();

            var provider = preferredProvider ?? AIProvider.OpenAI;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var historicalDataJson = JsonSerializer.Serialize(historicalData);

            var systemPrompt = @"You are a data scientist specializing in email engagement optimization. Analyze historical invitation patterns and determine the optimal send time for each guest based on:
- Past response times
- Day of week patterns
- Time of day patterns
- Industry norms (e.g., healthcare vs tech)
- Event timing

Return JSON mapping guest IDs to optimal send times (ISO 8601 format).";

            var userPrompt = $@"Determine optimal invitation send times:
Event Date: {eventEntity.StartDate:yyyy-MM-dd}
Current Date: {DateTime.UtcNow:yyyy-MM-dd}

Historical Guest Data:
{historicalDataJson}

Consider:
- Send 2-4 weeks before event
- Best days: Tuesday-Thursday
- Best times: 10am-2pm or 7-9pm local time
- Industry-specific patterns

Return JSON: {{""guestId"": ""2025-XX-XX HH:mm:ss""}}";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.5, 2000);

            if (!response.Success)
            {
                return new InvitationAgentResult
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage,
                    Provider = provider,
                    CreditsCost = totalCost
                };
            }

            await _creditDeduction.DeductCreditsAsync(
                tenantId,
                totalCost,
                AIAgentType.InvitationAgent,
                provider,
                $"Determine optimal send times for {guestIds.Count} guests",
                relatedEntityId: eventId,
                relatedEntityType: "Event",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: $"Optimal send times for {guestIds.Count} guests",
                agentResponse: response.Content
            );

            return new InvitationAgentResult
            {
                Success = true,
                Response = response.Content,
                CreditsCost = totalCost,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining optimal send times for event {EventId}", eventId);

            return new InvitationAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = totalCost
            };
        }
    }

    public async Task<InvitationAgentResult> PredictGuestAttendanceAsync(
        Guid tenantId,
        Guid guestId,
        Guid eventId,
        AIProvider? preferredProvider = null)
    {
        var totalCost = BASE_CREDIT_COST + CREDIT_PER_GUEST;

        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, totalCost))
        {
            return new InvitationAgentResult
            {
                Success = false,
                ErrorMessage = $"Insufficient credits. Required: {totalCost}",
                CreditsCost = totalCost
            };
        }

        try
        {
            var guest = await _context.Guests.FindAsync(guestId);
            var eventEntity = await _context.Events.FindAsync(eventId);

            if (guest == null || eventEntity == null)
            {
                return new InvitationAgentResult
                {
                    Success = false,
                    ErrorMessage = "Guest or event not found",
                    CreditsCost = totalCost
                };
            }

            var guestHistory = await _context.EventInvitations
                .Where(i => i.GuestId == guestId)
                .Select(i => new
                {
                    i.RsvpStatus,
                    i.AttendedEvent,
                    EventCategory = i.Event!.Category
                })
                .ToListAsync();

            var provider = preferredProvider ?? AIProvider.OpenAI;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var historyJson = JsonSerializer.Serialize(guestHistory);

            var systemPrompt = @"You are an ML expert in attendance prediction. Predict the likelihood of a guest attending an event based on:
- Historical attendance rate
- Event category match with past preferences
- Engagement score
- Response patterns

Return a prediction score (0-1) with reasoning.";

            var userPrompt = $@"Predict attendance likelihood:
Guest: {guest.FirstName} {guest.LastName}
Engagement Score: {guest.EngagementScore}
Event: {eventEntity.Name}
Category: {eventEntity.Category}

Guest History:
{historyJson}

Return JSON: {{""attendanceProbability"": 0.XX, ""confidence"": ""high/medium/low"", ""factors"": [""reasons""]}}";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.3, 500);

            if (!response.Success)
            {
                return new InvitationAgentResult
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage,
                    Provider = provider,
                    CreditsCost = totalCost
                };
            }

            await _creditDeduction.DeductCreditsAsync(
                tenantId,
                totalCost,
                AIAgentType.InvitationAgent,
                provider,
                $"Predict attendance for guest {guest.Email} at {eventEntity.Name}",
                relatedEntityId: eventId,
                relatedEntityType: "Event",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: $"Predict attendance for {guest.Email}",
                agentResponse: response.Content
            );

            return new InvitationAgentResult
            {
                Success = true,
                Response = response.Content,
                CreditsCost = totalCost,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting attendance for guest {GuestId}", guestId);

            return new InvitationAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = totalCost
            };
        }
    }
}
