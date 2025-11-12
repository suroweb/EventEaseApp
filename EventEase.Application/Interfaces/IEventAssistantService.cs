namespace EventEase.Application.Interfaces;

/// <summary>
/// AI Event Assistant service for intelligent event management
/// </summary>
public interface IEventAssistantService
{
    /// <summary>
    /// Create event from natural language description
    /// </summary>
    Task<EventCreationSuggestion> CreateEventFromNaturalLanguageAsync(
        string naturalLanguageInput,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggest optimal event schedule based on constraints
    /// </summary>
    Task<EventScheduleSuggestion> SuggestEventScheduleAsync(
        EventScheduleRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recommend venues based on event requirements
    /// </summary>
    Task<List<VenueRecommendation>> RecommendVenuesAsync(
        VenueRecommendationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggest speakers based on event topic and requirements
    /// </summary>
    Task<List<SpeakerSuggestion>> SuggestSpeakersAsync(
        SpeakerSuggestionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimize event agenda for maximum engagement
    /// </summary>
    Task<AgendaOptimization> OptimizeAgendaAsync(
        AgendaOptimizationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Answer natural language questions about an event
    /// </summary>
    Task<string> AnswerEventQuestionAsync(
        Guid eventId,
        string question,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Event creation suggestion from natural language
/// </summary>
public class EventCreationSuggestion
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? SuggestedStartDate { get; set; }
    public DateTime? SuggestedEndDate { get; set; }
    public string? Location { get; set; }
    public string? EventType { get; set; }
    public int? EstimatedCapacity { get; set; }
    public decimal? SuggestedPrice { get; set; }
    public List<string> ExtractedTags { get; set; } = new();
    public List<string> SuggestedCategories { get; set; } = new();
    public string? TargetAudience { get; set; }
    public double ConfidenceScore { get; set; }
}

/// <summary>
/// Event schedule request
/// </summary>
public class EventScheduleRequest
{
    public string EventTitle { get; set; } = string.Empty;
    public string EventDescription { get; set; } = string.Empty;
    public int DurationInDays { get; set; }
    public List<string> SessionTopics { get; set; } = new();
    public DateTime? PreferredStartDate { get; set; }
    public List<string> Constraints { get; set; } = new(); // e.g., "avoid weekends", "morning sessions only"
}

/// <summary>
/// Event schedule suggestion
/// </summary>
public class EventScheduleSuggestion
{
    public DateTime RecommendedStartDate { get; set; }
    public DateTime RecommendedEndDate { get; set; }
    public List<ScheduledSession> Sessions { get; set; } = new();
    public string Reasoning { get; set; } = string.Empty;
    public List<string> Considerations { get; set; } = new();
}

public class ScheduledSession
{
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SessionType { get; set; } = string.Empty; // keynote, workshop, panel, networking
}

/// <summary>
/// Venue recommendation request
/// </summary>
public class VenueRecommendationRequest
{
    public string EventType { get; set; } = string.Empty;
    public int ExpectedAttendees { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public decimal? MaxBudget { get; set; }
    public List<string> RequiredAmenities { get; set; } = new(); // e.g., "AV equipment", "catering", "parking"
    public DateTime? EventDate { get; set; }
}

/// <summary>
/// Venue recommendation
/// </summary>
public class VenueRecommendation
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal? EstimatedCost { get; set; }
    public List<string> Amenities { get; set; } = new();
    public double MatchScore { get; set; } // 0-1
    public string Reasoning { get; set; } = string.Empty;
    public string? ContactInfo { get; set; }
}

/// <summary>
/// Speaker suggestion request
/// </summary>
public class SpeakerSuggestionRequest
{
    public string EventTopic { get; set; } = string.Empty;
    public string EventDescription { get; set; } = string.Empty;
    public List<string> RequiredExpertise { get; set; } = new();
    public string? IndustryFocus { get; set; }
    public int NumberOfSpeakers { get; set; } = 1;
}

/// <summary>
/// Speaker suggestion
/// </summary>
public class SpeakerSuggestion
{
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Expertise { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public List<string> Topics { get; set; } = new();
    public double RelevanceScore { get; set; } // 0-1
    public string Reasoning { get; set; } = string.Empty;
}

/// <summary>
/// Agenda optimization request
/// </summary>
public class AgendaOptimizationRequest
{
    public Guid EventId { get; set; }
    public List<ProposedSession> ProposedSessions { get; set; } = new();
    public DateTime EventStartDate { get; set; }
    public DateTime EventEndDate { get; set; }
    public List<string> OptimizationGoals { get; set; } = new(); // e.g., "maximize networking", "balance topics"
}

public class ProposedSession
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string SessionType { get; set; } = string.Empty;
    public int Priority { get; set; } = 1; // 1-5
}

/// <summary>
/// Agenda optimization result
/// </summary>
public class AgendaOptimization
{
    public List<OptimizedSession> OptimizedSessions { get; set; } = new();
    public string OptimizationSummary { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = new();
    public double EngagementScore { get; set; } // Predicted engagement 0-1
}

public class OptimizedSession
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string SessionType { get; set; } = string.Empty;
    public string OptimizationReason { get; set; } = string.Empty;
}
