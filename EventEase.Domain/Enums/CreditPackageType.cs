namespace EventEase.Domain.Enums;

/// <summary>
/// Types of credit packages available
/// </summary>
public enum CreditPackageType
{
    /// <summary>
    /// Starter package: €99 = 1,000 credits (6 months validity)
    /// </summary>
    Starter = 0,

    /// <summary>
    /// Pro package: €399 = 6,000 credits (5,000 + 1,000 bonus) (12 months validity)
    /// </summary>
    Pro = 1,

    /// <summary>
    /// Enterprise package: Custom pricing, 20,000+ credits
    /// </summary>
    Enterprise = 2,

    /// <summary>
    /// Pay-as-you-go: €0.15 per credit
    /// </summary>
    PayAsYouGo = 3
}
