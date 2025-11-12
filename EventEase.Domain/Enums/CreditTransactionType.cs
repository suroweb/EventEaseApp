namespace EventEase.Domain.Enums;

/// <summary>
/// Types of credit transactions
/// </summary>
public enum CreditTransactionType
{
    /// <summary>
    /// Credits purchased by tenant
    /// </summary>
    Purchase = 0,

    /// <summary>
    /// Credits consumed for AI operations
    /// </summary>
    Deduction = 1,

    /// <summary>
    /// Credits refunded
    /// </summary>
    Refund = 2,

    /// <summary>
    /// Bonus credits awarded
    /// </summary>
    Bonus = 3,

    /// <summary>
    /// Credits expired
    /// </summary>
    Expiration = 4,

    /// <summary>
    /// Manual adjustment by system admin
    /// </summary>
    Adjustment = 5
}
