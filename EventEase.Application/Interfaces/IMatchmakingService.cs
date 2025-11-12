namespace EventEase.Application.Interfaces;

/// <summary>
/// AI Matchmaking service for intelligent attendee networking
/// </summary>
public interface IMatchmakingService
{
    /// <summary>
    /// Calculate similarity score between two attendees
    /// </summary>
    Task<double> CalculateAttendeeSimilarityAsync(
        Guid attendeeId1,
        Guid attendeeId2,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get networking suggestions for an attendee
    /// </summary>
    Task<List<NetworkingSuggestion>> GetNetworkingSuggestionsAsync(
        Guid attendeeId,
        Guid eventId,
        int maxSuggestions = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate conversation starters between two attendees
    /// </summary>
    Task<List<string>> GenerateConversationStartersAsync(
        Guid attendeeId1,
        Guid attendeeId2,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get follow-up recommendations after an event
    /// </summary>
    Task<List<FollowUpRecommendation>> GetFollowUpRecommendationsAsync(
        Guid attendeeId,
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create or update attendee profile embeddings for similarity matching
    /// </summary>
    Task UpdateAttendeeEmbeddingsAsync(
        Guid attendeeId,
        AttendeeProfile profile,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find similar attendees based on interests and background
    /// </summary>
    Task<List<SimilarAttendee>> FindSimilarAttendeesAsync(
        Guid attendeeId,
        int maxResults = 20,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Networking suggestion for an attendee
/// </summary>
public class NetworkingSuggestion
{
    public Guid SuggestedAttendeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public double SimilarityScore { get; set; } // 0-1
    public List<string> CommonInterests { get; set; } = new();
    public List<string> ComplementarySkills { get; set; } = new();
    public string MatchReason { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string? LinkedInUrl { get; set; }
}

/// <summary>
/// Follow-up recommendation after an event
/// </summary>
public class FollowUpRecommendation
{
    public Guid AttendeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RecommendationType { get; set; } = string.Empty; // "connect", "collaborate", "schedule_meeting"
    public string Reason { get; set; } = string.Empty;
    public List<string> SuggestedTopics { get; set; } = new();
    public int Priority { get; set; } // 1-5
    public string? SuggestedMessage { get; set; }
}

/// <summary>
/// Attendee profile for similarity matching
/// </summary>
public class AttendeeProfile
{
    public Guid AttendeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public List<string> Interests { get; set; } = new();
    public List<string> Skills { get; set; } = new();
    public List<string> Goals { get; set; } = new();
    public string? Location { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? TwitterHandle { get; set; }
}

/// <summary>
/// Similar attendee result
/// </summary>
public class SimilarAttendee
{
    public Guid AttendeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
    public List<string> MatchingAttributes { get; set; } = new();
}
