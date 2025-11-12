namespace EventEase.Application.Interfaces;

/// <summary>
/// AI Content Generation service using GPT-4o
/// </summary>
public interface IContentGenerationService
{
    /// <summary>
    /// Generate event description from basic details
    /// </summary>
    Task<string> GenerateEventDescriptionAsync(
        EventDescriptionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate email content for various event communications
    /// </summary>
    Task<EmailContent> GenerateEmailContentAsync(
        EmailGenerationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate social media posts for event promotion
    /// </summary>
    Task<List<SocialMediaPost>> GenerateSocialMediaPostsAsync(
        SocialMediaRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate session summary from notes or transcript
    /// </summary>
    Task<SessionSummary> GenerateSessionSummaryAsync(
        string sessionTitle,
        string contentOrTranscript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate personalized event recommendations for an attendee
    /// </summary>
    Task<List<EventRecommendation>> GenerateEventRecommendationsAsync(
        Guid userId,
        int maxRecommendations = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate session recommendations for an attendee at an event
    /// </summary>
    Task<List<SessionRecommendation>> GenerateSessionRecommendationsAsync(
        Guid attendeeId,
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate optimal pricing suggestions for an event
    /// </summary>
    Task<PricingSuggestion> GeneratePricingSuggestionAsync(
        PricingSuggestionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate event hashtags
    /// </summary>
    Task<List<string>> GenerateEventHashtagsAsync(
        string eventTitle,
        string eventDescription,
        int maxHashtags = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate FAQ for an event
    /// </summary>
    Task<List<FAQ>> GenerateEventFAQAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Event description generation request
/// </summary>
public class EventDescriptionRequest
{
    public string Title { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? TargetAudience { get; set; }
    public List<string> KeyTopics { get; set; } = new();
    public string? Location { get; set; }
    public DateTime? StartDate { get; set; }
    public int? ExpectedAttendees { get; set; }
    public string Tone { get; set; } = "professional"; // professional, casual, exciting, formal
    public int MaxWords { get; set; } = 200;
}

/// <summary>
/// Email generation request
/// </summary>
public class EmailGenerationRequest
{
    public string EmailType { get; set; } = string.Empty; // invitation, reminder, confirmation, thank-you, follow-up
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
    public DateTime? EventDate { get; set; }
    public string? EventLocation { get; set; }
    public Dictionary<string, string> CustomFields { get; set; } = new(); // Additional context
    public string Tone { get; set; } = "professional";
}

/// <summary>
/// Generated email content
/// </summary>
public class EmailContent
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? PreheaderText { get; set; }
    public List<string> SuggestedCTAs { get; set; } = new(); // Call-to-action buttons
}

/// <summary>
/// Social media generation request
/// </summary>
public class SocialMediaRequest
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string EventDescription { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public List<string> Platforms { get; set; } = new(); // twitter, linkedin, facebook, instagram
    public string Tone { get; set; } = "exciting";
    public int PostsPerPlatform { get; set; } = 3;
}

/// <summary>
/// Social media post
/// </summary>
public class SocialMediaPost
{
    public string Platform { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Hashtags { get; set; } = new();
    public string? ImagePrompt { get; set; } // Suggestion for image generation
    public int CharacterCount { get; set; }
    public string PostType { get; set; } = string.Empty; // announcement, teaser, countdown, recap
}

/// <summary>
/// Session summary
/// </summary>
public class SessionSummary
{
    public string Title { get; set; } = string.Empty;
    public string ExecutiveSummary { get; set; } = string.Empty;
    public List<string> KeyPoints { get; set; } = new();
    public List<string> ActionItems { get; set; } = new();
    public List<string> Quotes { get; set; } = new();
    public Dictionary<string, string> Topics { get; set; } = new(); // Topic -> Description
    public string? NextSteps { get; set; }
}

/// <summary>
/// Event recommendation for a user
/// </summary>
public class EventRecommendation
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public double RelevanceScore { get; set; } // 0-1
    public List<string> MatchReasons { get; set; } = new();
    public string PersonalizedPitch { get; set; } = string.Empty; // Why this user should attend
}

/// <summary>
/// Session recommendation for an attendee
/// </summary>
public class SessionRecommendation
{
    public Guid SessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public double RelevanceScore { get; set; } // 0-1
    public List<string> MatchReasons { get; set; } = new();
    public List<string> RelatedInterests { get; set; } = new();
}

/// <summary>
/// Pricing suggestion request
/// </summary>
public class PricingSuggestionRequest
{
    public string EventType { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public int ExpectedAttendees { get; set; }
    public int DurationInDays { get; set; }
    public string? Location { get; set; }
    public bool IncludesMeals { get; set; }
    public bool IncludesAccommodation { get; set; }
    public List<string> IncludedAmenities { get; set; } = new();
    public string TargetAudience { get; set; } = string.Empty;
    public decimal? EstimatedCosts { get; set; }
}

/// <summary>
/// Pricing suggestion
/// </summary>
public class PricingSuggestion
{
    public decimal RecommendedPrice { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public List<PricingTier> SuggestedTiers { get; set; } = new();
    public string PricingStrategy { get; set; } = string.Empty;
    public List<string> Justification { get; set; } = new();
    public Dictionary<string, decimal> MarketComparison { get; set; } = new(); // Competitor pricing
}

public class PricingTier
{
    public string TierName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public List<string> Benefits { get; set; } = new();
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// FAQ item
/// </summary>
public class FAQ
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
