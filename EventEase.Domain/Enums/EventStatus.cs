namespace EventEase.Domain.Enums;

/// <summary>
/// Status of an event
/// </summary>
public enum EventStatus
{
    /// <summary>
    /// Event is in draft mode
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Event is published and accepting registrations
    /// </summary>
    Published = 1,

    /// <summary>
    /// Event is full (max attendees reached)
    /// </summary>
    Full = 2,

    /// <summary>
    /// Event has been cancelled
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Event is completed
    /// </summary>
    Completed = 4,

    /// <summary>
    /// Event is postponed
    /// </summary>
    Postponed = 5
}
