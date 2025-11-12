namespace EventEase.Domain.Enums;

/// <summary>
/// Status of an event registration
/// </summary>
public enum RegistrationStatus
{
    /// <summary>
    /// Registration is pending confirmation
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Registration is confirmed
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Registration is cancelled by attendee
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// Attendee checked in at the event
    /// </summary>
    CheckedIn = 3,

    /// <summary>
    /// Attendee was a no-show
    /// </summary>
    NoShow = 4,

    /// <summary>
    /// Registration is on waitlist (event full)
    /// </summary>
    Waitlisted = 5
}
