namespace EventEase.Domain.Enums;

/// <summary>
/// Status of an invitation
/// </summary>
public enum InvitationStatus
{
    /// <summary>
    /// Invitation is drafted but not sent
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Invitation is queued for sending
    /// </summary>
    Queued = 1,

    /// <summary>
    /// Invitation has been sent
    /// </summary>
    Sent = 2,

    /// <summary>
    /// Invitation was opened by recipient
    /// </summary>
    Opened = 3,

    /// <summary>
    /// Recipient clicked link in invitation
    /// </summary>
    Clicked = 4,

    /// <summary>
    /// Invitation was accepted (registered)
    /// </summary>
    Accepted = 5,

    /// <summary>
    /// Invitation was declined
    /// </summary>
    Declined = 6,

    /// <summary>
    /// Invitation bounced (invalid email)
    /// </summary>
    Bounced = 7
}
