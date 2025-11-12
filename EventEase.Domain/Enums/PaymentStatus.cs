namespace EventEase.Domain.Enums;

/// <summary>
/// Payment transaction status from Stripe
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment is pending
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment succeeded
    /// </summary>
    Succeeded = 1,

    /// <summary>
    /// Payment failed
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Payment was refunded
    /// </summary>
    Refunded = 3,

    /// <summary>
    /// Payment is disputed/chargeback
    /// </summary>
    Disputed = 4,

    /// <summary>
    /// Payment was cancelled
    /// </summary>
    Cancelled = 5
}
