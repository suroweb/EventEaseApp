using EventEase.Domain.Enums;

namespace EventEase.Application.Interfaces;

/// <summary>
/// Planning Agent - AI-powered event planning and creation (15 credits)
/// </summary>
public interface IPlanningAgentService
{
    /// <summary>
    /// Create event from natural language description
    /// </summary>
    Task<PlanningAgentResult> CreateEventFromDescriptionAsync(
        Guid tenantId,
        string description,
        AIProvider? preferredProvider = null);

    /// <summary>
    /// Get venue recommendations for an event
    /// </summary>
    Task<PlanningAgentResult> GetVenueRecommendationsAsync(
        Guid tenantId,
        string eventType,
        int expectedAttendees,
        string? location = null,
        decimal? budget = null,
        AIProvider? preferredProvider = null);

    /// <summary>
    /// Optimize event schedule/agenda
    /// </summary>
    Task<PlanningAgentResult> OptimizeEventScheduleAsync(
        Guid tenantId,
        Guid eventId,
        string requirements,
        AIProvider? preferredProvider = null);

    /// <summary>
    /// Generate event description and marketing copy
    /// </summary>
    Task<PlanningAgentResult> GenerateEventContentAsync(
        Guid tenantId,
        string eventName,
        string eventType,
        string targetAudience,
        AIProvider? preferredProvider = null);
}

/// <summary>
/// Planning agent operation result
/// </summary>
public class PlanningAgentResult
{
    public bool Success { get; set; }
    public string Response { get; set; } = string.Empty;
    public decimal CreditsCost { get; set; }
    public AIProvider Provider { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public string? ErrorMessage { get; set; }
    public object? StructuredData { get; set; }
}
