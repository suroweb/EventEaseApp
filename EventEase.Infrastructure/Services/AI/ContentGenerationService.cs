using EventEase.Application.Interfaces;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventEase.Infrastructure.Services.AI;

/// <summary>
/// AI Content Generation service using GPT-4o for creative content
/// </summary>
public class ContentGenerationService : IContentGenerationService
{
    private readonly IOpenAIService _openAIService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ContentGenerationService> _logger;

    public ContentGenerationService(
        IOpenAIService openAIService,
        ApplicationDbContext dbContext,
        ILogger<ContentGenerationService> logger)
    {
        _openAIService = openAIService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<string> GenerateEventDescriptionAsync(
        EventDescriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var keyTopicsText = request.KeyTopics.Any()
                ? string.Join(", ", request.KeyTopics)
                : "general topics";

            var systemPrompt = $@"You are an expert event marketing copywriter. Create compelling, engaging event descriptions that attract attendees.

Write in a {request.Tone} tone.
Maximum length: {request.MaxWords} words.

The description should:
- Hook the reader immediately
- Highlight key value propositions
- Create excitement and urgency
- Include specific details
- End with a strong call-to-action implication

Do NOT use markdown formatting or section headers - write flowing, narrative text.";

            var userPrompt = $@"Create an event description for:

Title: {request.Title}
Type: {request.EventType}
Target Audience: {request.TargetAudience ?? "professionals"}
Key Topics: {keyTopicsText}
Location: {request.Location ?? "TBD"}
Date: {request.StartDate?.ToString("MMMM d, yyyy") ?? "Coming soon"}
Expected Attendees: {request.ExpectedAttendees?.ToString() ?? "Limited capacity"}
Industry: {request.Industry ?? "general"}";

            var response = await _openAIService.SendPromptAsync(
                systemPrompt,
                userPrompt,
                temperature: 0.8,
                maxTokens: request.MaxWords * 2);

            if (response.Success)
            {
                _logger.LogInformation("Generated event description ({Words} words)",
                    response.Content.Split(' ').Length);
                return response.Content.Trim();
            }

            return $"{request.Title} - {string.Join(", ", request.KeyTopics)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating event description");
            throw;
        }
    }

    public async Task<EmailContent> GenerateEmailContentAsync(
        EmailGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var systemPrompt = $@"You are an expert email marketing copywriter specializing in event communications.

Create a {request.EmailType} email for an event.
Tone: {request.Tone}

The email should:
- Have a compelling subject line (under 60 characters)
- Include a preheader text (under 100 characters)
- Have clear, scannable body copy
- Include personalization where appropriate
- Have a strong call-to-action
- Be mobile-friendly and concise

Return a JSON object:
{{
  ""subject"": ""string"",
  ""preheaderText"": ""string"",
  ""body"": ""string (use simple HTML formatting: <p>, <strong>, <ul>, <li>)"",
  ""suggestedCTAs"": [""string""]
}}";

            var customFieldsText = request.CustomFields.Any()
                ? JsonSerializer.Serialize(request.CustomFields)
                : "none";

            var userPrompt = $@"Create a {request.EmailType} email for:

Event: {request.EventTitle}
Recipient: {request.RecipientName ?? "attendee"}
Event Date: {request.EventDate?.ToString("MMMM d, yyyy 'at' h:mm tt") ?? "TBD"}
Location: {request.EventLocation ?? "TBD"}
Custom Details: {customFieldsText}";

            var response = await _openAIService.SendPromptJsonAsync<EmailContent>(
                systemPrompt,
                userPrompt,
                temperature: 0.7,
                maxTokens: 1500);

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("Generated {EmailType} email for event {EventId}",
                    request.EmailType, request.EventId);
                return response.Data;
            }

            // Fallback
            return new EmailContent
            {
                Subject = $"{request.EventTitle} - {request.EmailType}",
                Body = $"<p>Thank you for your interest in {request.EventTitle}.</p>",
                SuggestedCTAs = new List<string> { "View Event Details", "Register Now" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating email content");
            throw;
        }
    }

    public async Task<List<SocialMediaPost>> GenerateSocialMediaPostsAsync(
        SocialMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var systemPrompt = $@"You are a social media marketing expert specializing in event promotion.

Create {request.PostsPerPlatform} engaging social media posts for each platform: {string.Join(", ", request.Platforms)}.

Tone: {request.Tone}

Consider each platform's characteristics:
- Twitter/X: Short, punchy, 280 chars max
- LinkedIn: Professional, value-focused, 1300 chars max
- Facebook: Conversational, community-focused, 400-500 chars ideal
- Instagram: Visual-first, story-driven, 2200 chars max

Return a JSON object:
{{
  ""posts"": [
    {{
      ""platform"": ""string"",
      ""content"": ""string"",
      ""hashtags"": [""string""],
      ""imagePrompt"": ""string (suggestion for AI image generation)"",
      ""postType"": ""announcement|teaser|countdown|recap""
    }}
  ]
}}";

            var userPrompt = $@"Create social media posts for:

Event: {request.EventTitle}
Description: {request.EventDescription}
Date: {request.EventDate:MMMM d, yyyy}
Platforms: {string.Join(", ", request.Platforms)}

Create diverse post types: announcements, teasers, countdowns, etc.";

            var response = await _openAIService.SendPromptJsonAsync<SocialMediaPostsResponse>(
                systemPrompt,
                userPrompt,
                temperature: 0.9,
                maxTokens: 3000);

            if (response.Success && response.Data?.Posts != null)
            {
                // Add character counts
                foreach (var post in response.Data.Posts)
                {
                    post.CharacterCount = post.Content.Length;
                }

                _logger.LogInformation("Generated {Count} social media posts for event {EventId}",
                    response.Data.Posts.Count, request.EventId);
                return response.Data.Posts;
            }

            return new List<SocialMediaPost>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating social media posts");
            throw;
        }
    }

    public async Task<SessionSummary> GenerateSessionSummaryAsync(
        string sessionTitle,
        string contentOrTranscript,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var systemPrompt = @"You are an expert at creating comprehensive session summaries from meeting transcripts or notes.

Extract and organize:
- Executive summary (2-3 sentences)
- Key points (5-7 main takeaways)
- Action items (specific, actionable tasks)
- Notable quotes
- Topics covered with brief descriptions
- Suggested next steps

Return a JSON object:
{
  ""title"": ""string"",
  ""executiveSummary"": ""string"",
  ""keyPoints"": [""string""],
  ""actionItems"": [""string""],
  ""quotes"": [""string""],
  ""topics"": {""topic"": ""description""},
  ""nextSteps"": ""string""
}";

            var userPrompt = $@"Create a summary for this session:

Title: {sessionTitle}

Content/Transcript:
{contentOrTranscript}";

            var response = await _openAIService.SendPromptJsonAsync<SessionSummary>(
                systemPrompt,
                userPrompt,
                temperature: 0.5,
                maxTokens: 2000);

            if (response.Success && response.Data != null)
            {
                response.Data.Title = sessionTitle;
                _logger.LogInformation("Generated session summary for: {Title}", sessionTitle);
                return response.Data;
            }

            // Fallback
            return new SessionSummary
            {
                Title = sessionTitle,
                ExecutiveSummary = "Summary generation in progress.",
                KeyPoints = new List<string> { "Content analysis pending" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating session summary");
            throw;
        }
    }

    public async Task<List<EventRecommendation>> GenerateEventRecommendationsAsync(
        Guid userId,
        int maxRecommendations = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user's event history and preferences
            var userHistory = await _dbContext.Registrations
                .Where(r => r.UserId == userId)
                .Select(r => new
                {
                    r.Event.Title,
                    r.Event.EventType,
                    r.Event.Description
                })
                .Take(10)
                .ToListAsync(cancellationToken);

            // Get upcoming events
            var upcomingEvents = await _dbContext.Events
                .Where(e => e.StartDate > DateTime.UtcNow)
                .OrderBy(e => e.StartDate)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    e.StartDate,
                    e.Location,
                    e.EventType,
                    e.Price
                })
                .Take(20)
                .ToListAsync(cancellationToken);

            var recommendations = new List<EventRecommendation>();

            foreach (var upcomingEvent in upcomingEvents.Take(maxRecommendations))
            {
                var systemPrompt = @"You are an event recommendation expert. Calculate relevance and create a personalized pitch.

Return JSON:
{
  ""relevanceScore"": number (0-1),
  ""matchReasons"": [""string""],
  ""personalizedPitch"": ""string (1-2 sentences, compelling and personal)""
}";

                var userPrompt = $@"User's event history:
{JsonSerializer.Serialize(userHistory)}

Recommended event:
{JsonSerializer.Serialize(upcomingEvent)}

Assess relevance and create pitch.";

                var response = await _openAIService.SendPromptJsonAsync<RecommendationAnalysis>(
                    systemPrompt,
                    userPrompt,
                    temperature: 0.7,
                    maxTokens: 500);

                if (response.Success && response.Data != null && response.Data.RelevanceScore > 0.5)
                {
                    recommendations.Add(new EventRecommendation
                    {
                        EventId = upcomingEvent.Id,
                        Title = upcomingEvent.Title,
                        Description = upcomingEvent.Description ?? string.Empty,
                        StartDate = upcomingEvent.StartDate,
                        Location = upcomingEvent.Location ?? "TBD",
                        RelevanceScore = response.Data.RelevanceScore,
                        MatchReasons = response.Data.MatchReasons,
                        PersonalizedPitch = response.Data.PersonalizedPitch
                    });
                }
            }

            _logger.LogInformation("Generated {Count} event recommendations for user {UserId}",
                recommendations.Count, userId);

            return recommendations.OrderByDescending(r => r.RelevanceScore).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating event recommendations");
            return new List<EventRecommendation>();
        }
    }

    public async Task<List<SessionRecommendation>> GenerateSessionRecommendationsAsync(
        Guid attendeeId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // In a full implementation, we would:
            // 1. Get attendee interests and background
            // 2. Get all sessions for the event
            // 3. Match sessions to attendee profile
            // 4. Return ranked recommendations

            _logger.LogInformation("Generated session recommendations for attendee {AttendeeId} at event {EventId}",
                attendeeId, eventId);

            // Placeholder implementation
            return new List<SessionRecommendation>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating session recommendations");
            return new List<SessionRecommendation>();
        }
    }

    public async Task<PricingSuggestion> GeneratePricingSuggestionAsync(
        PricingSuggestionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var amenitiesText = request.IncludedAmenities.Any()
                ? string.Join(", ", request.IncludedAmenities)
                : "standard amenities";

            var systemPrompt = @"You are a pricing strategy expert for events. Recommend optimal pricing based on market analysis and value proposition.

Consider:
- Event type and duration
- Target audience and willingness to pay
- Included amenities and value
- Market positioning
- Competitor pricing
- Location and industry factors

Return JSON:
{
  ""recommendedPrice"": number,
  ""minPrice"": number,
  ""maxPrice"": number,
  ""suggestedTiers"": [
    {
      ""tierName"": ""string"",
      ""price"": number,
      ""benefits"": [""string""],
      ""description"": ""string""
    }
  ],
  ""pricingStrategy"": ""string"",
  ""justification"": [""string""],
  ""marketComparison"": {""competitor"": price}
}";

            var userPrompt = $@"Recommend pricing for:

Event Type: {request.EventType}
Industry: {request.Industry ?? "general"}
Expected Attendees: {request.ExpectedAttendees}
Duration: {request.DurationInDays} days
Location: {request.Location ?? "TBD"}
Includes Meals: {request.IncludesMeals}
Includes Accommodation: {request.IncludesAccommodation}
Amenities: {amenitiesText}
Target Audience: {request.TargetAudience}
Estimated Costs: {request.EstimatedCosts?.ToString("C") ?? "unknown"}";

            var response = await _openAIService.SendPromptJsonAsync<PricingSuggestion>(
                systemPrompt,
                userPrompt,
                temperature: 0.5,
                maxTokens: 2000);

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("Generated pricing suggestion: ${Price}", response.Data.RecommendedPrice);
                return response.Data;
            }

            // Fallback basic pricing
            var basePrice = request.EstimatedCosts * 1.5m ?? 100m;
            return new PricingSuggestion
            {
                RecommendedPrice = basePrice,
                MinPrice = basePrice * 0.8m,
                MaxPrice = basePrice * 1.3m,
                PricingStrategy = "Cost-plus with market adjustment",
                Justification = new List<string> { "Based on estimated costs and industry standards" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pricing suggestion");
            throw;
        }
    }

    public async Task<List<string>> GenerateEventHashtagsAsync(
        string eventTitle,
        string eventDescription,
        int maxHashtags = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var systemPrompt = @"You are a social media marketing expert. Generate relevant, trending hashtags for an event.

Hashtags should be:
- Relevant to the event topic and industry
- Mix of broad and specific tags
- Include brand/event-specific hashtags
- Trending when possible
- Easy to remember and type

Return a JSON array of hashtag strings (without the # symbol).";

            var userPrompt = $@"Generate {maxHashtags} hashtags for:

Title: {eventTitle}
Description: {eventDescription}";

            var response = await _openAIService.SendPromptJsonAsync<HashtagsResponse>(
                systemPrompt,
                userPrompt,
                temperature: 0.8,
                maxTokens: 500);

            if (response.Success && response.Data?.Hashtags != null)
            {
                _logger.LogInformation("Generated {Count} hashtags", response.Data.Hashtags.Count);
                return response.Data.Hashtags.Take(maxHashtags).ToList();
            }

            // Fallback hashtags
            return new List<string> { "Event", "Conference", "Networking" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating hashtags");
            return new List<string>();
        }
    }

    public async Task<List<FAQ>> GenerateEventFAQAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var eventData = await _dbContext.Events
                .Where(e => e.Id == eventId)
                .Select(e => new
                {
                    e.Title,
                    e.Description,
                    e.StartDate,
                    e.EndDate,
                    e.Location,
                    e.Price,
                    e.Capacity,
                    e.EventType
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (eventData == null)
            {
                return new List<FAQ>();
            }

            var systemPrompt = @"You are an event planning expert. Generate comprehensive FAQ items that attendees commonly ask.

Cover topics like:
- Registration and ticketing
- Event schedule and timing
- Location and parking
- What to bring
- Refund policy
- Accessibility
- COVID safety (if relevant)
- Networking opportunities
- Food and beverages

Return JSON:
{
  ""faqs"": [
    {
      ""question"": ""string"",
      ""answer"": ""string"",
      ""category"": ""string"",
      ""displayOrder"": number
    }
  ]
}";

            var eventInfo = JsonSerializer.Serialize(eventData, new JsonSerializerOptions { WriteIndented = true });

            var userPrompt = $@"Generate 8-12 FAQ items for this event:

{eventInfo}";

            var response = await _openAIService.SendPromptJsonAsync<FAQResponse>(
                systemPrompt,
                userPrompt,
                temperature: 0.6,
                maxTokens: 2500);

            if (response.Success && response.Data?.Faqs != null)
            {
                _logger.LogInformation("Generated {Count} FAQ items for event {EventId}",
                    response.Data.Faqs.Count, eventId);
                return response.Data.Faqs;
            }

            return new List<FAQ>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating FAQ for event {EventId}", eventId);
            return new List<FAQ>();
        }
    }

    #region Helper Classes

    private class SocialMediaPostsResponse
    {
        public List<SocialMediaPost> Posts { get; set; } = new();
    }

    private class RecommendationAnalysis
    {
        public double RelevanceScore { get; set; }
        public List<string> MatchReasons { get; set; } = new();
        public string PersonalizedPitch { get; set; } = string.Empty;
    }

    private class HashtagsResponse
    {
        public List<string> Hashtags { get; set; } = new();
    }

    private class FAQResponse
    {
        public List<FAQ> Faqs { get; set; } = new();
    }

    #endregion
}
