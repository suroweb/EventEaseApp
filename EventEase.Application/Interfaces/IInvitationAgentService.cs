using EventEase.Domain.Enums;

namespace EventEase.Application.Interfaces;

/// <summary>
/// Invitation Agent - ML-based guest selection and personalized invitations (10 + 0.5/guest credits)
/// </summary>
public interface IInvitationAgentService
{
    /// <summary>
    /// Select optimal guests for an event using ML
    /// </summary>
    Task<InvitationAgentResult> SelectGuestsForEventAsync(
        Guid tenantId,
        Guid eventId,
        int maxGuests,
        string? selectionCriteria = null,
        AIProvider? preferredProvider = null);

    /// <summary>
    /// Generate personalized invitation messages
    /// </summary>
    Task<InvitationAgentResult> GeneratePersonalizedInvitationsAsync(
        Guid tenantId,
        Guid eventId,
        List<Guid> guestIds,
        string? customMessage = null,
        AIProvider? preferredProvider = null);

    /// <summary>
    /// Determine optimal send time for invitations
    /// </summary>
    Task<InvitationAgentResult> DetermineOptimalSendTimeAsync(
        Guid tenantId,
        Guid eventId,
        List<Guid> guestIds,
        AIProvider? preferredProvider = null);

    /// <summary>
    /// Predict guest attendance likelihood
    /// </summary>
    Task<InvitationAgentResult> PredictGuestAttendanceAsync(
        Guid tenantId,
        Guid guestId,
        Guid eventId,
        AIProvider? preferredProvider = null);
}

/// <summary>
/// Invitation agent operation result
/// </summary>
public class InvitationAgentResult
{
    public bool Success { get; set; }
    public string Response { get; set; } = string.Empty;
    public decimal CreditsCost { get; set; }
    public AIProvider Provider { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public string? ErrorMessage { get; set; }
    public List<GuestSelection>? SelectedGuests { get; set; }
    public Dictionary<Guid, string>? PersonalizedMessages { get; set; }
    public Dictionary<Guid, DateTime>? OptimalSendTimes { get; set; }
    public Dictionary<Guid, double>? AttendancePredictions { get; set; }
}

/// <summary>
/// Guest selection with ML scoring
/// </summary>
public class GuestSelection
{
    public Guid GuestId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Reason { get; set; } = string.Empty;
    public double PredictedAttendanceRate { get; set; }
}
