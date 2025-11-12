using EventEase.Application.Interfaces;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventEase.Infrastructure.Services.AI;

/// <summary>
/// AI Event Assistant service implementation using Claude 3.5 Sonnet for complex reasoning
/// </summary>
public class EventAssistantService : IEventAssistantService
{
    private readonly IAnthropicService _anthropicService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<EventAssistantService> _logger;

    public EventAssistantService(
        IAnthropicService anthropicService,
        ApplicationDbContext dbContext,
        ILogger<EventAssistantService> logger)
    {
        _anthropicService = anthropicService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<EventCreationSuggestion> CreateEventFromNaturalLanguageAsync(
        string naturalLanguageInput,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var systemPrompt = @"You are an expert event planner AI assistant. Parse natural language event descriptions and extract structured event information.

Extract and infer the following information:
- Event title
- Detailed description
- Start and end dates (if mentioned, or suggest optimal dates)
- Location (physical or virtual)
- Event type (conference, workshop, webinar, networking, etc.)
- Estimated capacity
- Suggested ticket price (based on event type and scope)
- Relevant tags and categories
- Target audience

Return your response as a JSON object with this exact structure:
{
  ""title"": ""string"",
  ""description"": ""string (detailed, professional)"",
  ""suggestedStartDate"": ""ISO 8601 date or null"",
  ""suggestedEndDate"": ""ISO 8601 date or null"",
  ""location"": ""string or null"",
  ""eventType"": ""string"",
  ""estimatedCapacity"": number or null,
  ""suggestedPrice"": number or null,
  ""extractedTags"": [""string""],
  ""suggestedCategories"": [""string""],
  ""targetAudience"": ""string"",
  ""confidenceScore"": number (0-1)
}";

            var userPrompt = $"Parse this event request:\n\n{naturalLanguageInput}";

            var response = await _anthropicService.SendPromptJsonAsync<EventCreationSuggestion>(
                systemPrompt,
                userPrompt,
                temperature: 0.7,
                maxTokens: 2000);

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation(
                    "Successfully created event suggestion from natural language for tenant {TenantId}",
                    tenantId);
                return response.Data;
            }

            _logger.LogWarning("Failed to parse natural language event creation: {Error}", response.ErrorMessage);
            return new EventCreationSuggestion
            {
                Title = "Event",
                Description = naturalLanguageInput,
                ConfidenceScore = 0.3
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event from natural language");
            throw;
        }
    }

    public async Task<EventScheduleSuggestion> SuggestEventScheduleAsync(
        EventScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var constraintsText = request.Constraints.Count > 0
                ? string.Join(", ", request.Constraints)
                : "none specified";

            var systemPrompt = @"You are an expert event scheduling AI. Create optimal event schedules considering:
- Attendee engagement patterns (morning energy, post-lunch dips, etc.)
- Session types and their ideal timing
- Break requirements
- Networking opportunities
- Travel and logistics

Return a JSON object with this structure:
{
  ""recommendedStartDate"": ""ISO 8601 datetime"",
  ""recommendedEndDate"": ""ISO 8601 datetime"",
  ""sessions"": [
    {
      ""title"": ""string"",
      ""startTime"": ""ISO 8601 datetime"",
      ""endTime"": ""ISO 8601 datetime"",
      ""description"": ""string"",
      ""sessionType"": ""keynote|workshop|panel|networking|break""
    }
  ],
  ""reasoning"": ""string (explain your scheduling decisions)"",
  ""considerations"": [""string""]
}";

            var userPrompt = $@"Create an optimal schedule for this event:

Title: {request.EventTitle}
Description: {request.EventDescription}
Duration: {request.DurationInDays} days
Preferred Start: {request.PreferredStartDate?.ToString("yyyy-MM-dd") ?? "flexible"}
Session Topics: {string.Join(", ", request.SessionTopics)}
Constraints: {constraintsText}

Consider typical work week patterns, engagement optimization, and natural breaks.";

            var response = await _anthropicService.SendPromptJsonAsync<EventScheduleSuggestion>(
                systemPrompt,
                userPrompt,
                temperature: 0.8,
                maxTokens: 3000);

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("Successfully generated event schedule suggestion");
                return response.Data;
            }

            throw new InvalidOperationException($"Failed to generate schedule: {response.ErrorMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting event schedule");
            throw;
        }
    }

    public async Task<List<VenueRecommendation>> RecommendVenuesAsync(
        VenueRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var amenitiesText = request.RequiredAmenities.Count > 0
                ? string.Join(", ", request.RequiredAmenities)
                : "none specified";

            var systemPrompt = @"You are a venue selection expert. Recommend suitable venues based on event requirements.

Consider:
- Capacity matching
- Location accessibility
- Amenities and facilities
- Budget constraints
- Event type appropriateness
- Typical venue pricing in the area

Return a JSON array of venue recommendations with this structure:
[
  {
    ""name"": ""string"",
    ""address"": ""string"",
    ""capacity"": number,
    ""estimatedCost"": number or null,
    ""amenities"": [""string""],
    ""matchScore"": number (0-1),
    ""reasoning"": ""string (why this venue is a good match)"",
    ""contactInfo"": ""string or null""
  }
]

Provide 3-5 diverse recommendations ranging from budget to premium options.";

            var userPrompt = $@"Recommend venues for this event:

Event Type: {request.EventType}
Expected Attendees: {request.ExpectedAttendees}
Location: {request.City}, {request.Country}
Max Budget: {request.MaxBudget?.ToString("C") ?? "flexible"}
Required Amenities: {amenitiesText}
Event Date: {request.EventDate?.ToString("yyyy-MM-dd") ?? "flexible"}";

            var response = await _anthropicService.SendPromptJsonAsync<VenueRecommendationsResponse>(
                systemPrompt,
                userPrompt,
                temperature: 0.7,
                maxTokens: 2500);

            if (response.Success && response.Data?.Venues != null)
            {
                _logger.LogInformation("Successfully generated {Count} venue recommendations", response.Data.Venues.Count);
                return response.Data.Venues;
            }

            return new List<VenueRecommendation>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recommending venues");
            throw;
        }
    }

    public async Task<List<SpeakerSuggestion>> SuggestSpeakersAsync(
        SpeakerSuggestionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var expertiseText = request.RequiredExpertise.Count > 0
                ? string.Join(", ", request.RequiredExpertise)
                : "general expertise";

            var systemPrompt = @"You are an expert at identifying and suggesting speakers for events.

Consider:
- Topic expertise and credibility
- Speaking experience
- Audience appeal
- Industry relevance
- Diversity of perspectives

Return a JSON array of speaker suggestions:
[
  {
    ""name"": ""string (can be archetypal/example names)"",
    ""title"": ""string"",
    ""expertise"": ""string"",
    ""bio"": ""string (brief professional bio)"",
    ""topics"": [""string""],
    ""relevanceScore"": number (0-1),
    ""reasoning"": ""string (why they're a good fit)""
  }
]

Suggest {0} speakers with diverse backgrounds and expertise levels.";

            systemPrompt = string.Format(systemPrompt, request.NumberOfSpeakers);

            var userPrompt = $@"Suggest speakers for this event:

Topic: {request.EventTopic}
Description: {request.EventDescription}
Required Expertise: {expertiseText}
Industry Focus: {request.IndustryFocus ?? "any"}
Number of Speakers: {request.NumberOfSpeakers}";

            var response = await _anthropicService.SendPromptJsonAsync<SpeakerSuggestionsResponse>(
                systemPrompt,
                userPrompt,
                temperature: 0.8,
                maxTokens: 3000);

            if (response.Success && response.Data?.Speakers != null)
            {
                _logger.LogInformation("Successfully generated {Count} speaker suggestions", response.Data.Speakers.Count);
                return response.Data.Speakers;
            }

            return new List<SpeakerSuggestion>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting speakers");
            throw;
        }
    }

    public async Task<AgendaOptimization> OptimizeAgendaAsync(
        AgendaOptimizationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var goalsText = request.OptimizationGoals.Count > 0
                ? string.Join(", ", request.OptimizationGoals)
                : "maximize engagement";

            var sessionsJson = JsonSerializer.Serialize(request.ProposedSessions);

            var systemPrompt = @"You are an expert event agenda optimizer. Analyze proposed sessions and create an optimized schedule that:

- Maximizes attendee engagement
- Balances different session types
- Provides strategic breaks
- Creates networking opportunities
- Considers energy levels throughout the day
- Sequences content logically
- Avoids topic fatigue

Return a JSON object:
{
  ""optimizedSessions"": [
    {
      ""title"": ""string"",
      ""description"": ""string"",
      ""startTime"": ""ISO 8601 datetime"",
      ""endTime"": ""ISO 8601 datetime"",
      ""sessionType"": ""string"",
      ""optimizationReason"": ""string (why scheduled at this time)""
    }
  ],
  ""optimizationSummary"": ""string"",
  ""recommendations"": [""string""],
  ""engagementScore"": number (0-1, predicted engagement level)
}";

            var userPrompt = $@"Optimize the agenda for this event:

Event ID: {request.EventId}
Start Date: {request.EventStartDate:yyyy-MM-dd HH:mm}
End Date: {request.EventEndDate:yyyy-MM-dd HH:mm}
Optimization Goals: {goalsText}

Proposed Sessions:
{sessionsJson}

Create an optimized schedule that achieves the stated goals while maintaining high engagement.";

            var response = await _anthropicService.SendPromptJsonAsync<AgendaOptimization>(
                systemPrompt,
                userPrompt,
                temperature: 0.7,
                maxTokens: 4000);

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("Successfully optimized agenda for event {EventId}", request.EventId);
                return response.Data;
            }

            throw new InvalidOperationException($"Failed to optimize agenda: {response.ErrorMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing agenda for event {EventId}", request.EventId);
            throw;
        }
    }

    public async Task<string> AnswerEventQuestionAsync(
        Guid eventId,
        string question,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Fetch event details from database
            var eventDetails = await _dbContext.Events
                .Where(e => e.Id == eventId)
                .Select(e => new
                {
                    e.Title,
                    e.Description,
                    e.StartDate,
                    e.EndDate,
                    e.Location,
                    e.Capacity,
                    e.Price,
                    e.EventType,
                    RegistrationCount = e.Registrations.Count
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (eventDetails == null)
            {
                return "I couldn't find information about that event.";
            }

            var eventContext = JsonSerializer.Serialize(eventDetails, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var systemPrompt = @"You are a helpful event assistant. Answer questions about events based on the provided event information.

Be:
- Concise and helpful
- Professional but friendly
- Accurate based on the data provided
- Honest if information is not available

If the question cannot be answered with the provided data, politely say so and suggest contacting the event organizer.";

            var userPrompt = $@"Event Information:
{eventContext}

Question: {question}

Please provide a helpful answer.";

            var response = await _anthropicService.SendPromptAsync(
                systemPrompt,
                userPrompt,
                temperature: 0.7,
                maxTokens: 500);

            if (response.Success)
            {
                return response.Content;
            }

            return "I'm sorry, I couldn't process your question at this time.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error answering event question for event {EventId}", eventId);
            throw;
        }
    }

    #region Helper Classes

    private class VenueRecommendationsResponse
    {
        public List<VenueRecommendation> Venues { get; set; } = new();
    }

    private class SpeakerSuggestionsResponse
    {
        public List<SpeakerSuggestion> Speakers { get; set; } = new();
    }

    #endregion
}
