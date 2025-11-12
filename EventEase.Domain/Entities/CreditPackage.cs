using EventEase.Domain.Common;
using EventEase.Domain.Enums;

namespace EventEase.Domain.Entities;

/// <summary>
/// Represents a credit package available for purchase
/// </summary>
public class CreditPackage : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CreditPackageType Type { get; set; }

    // Pricing
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";

    // Credits
    public int BaseCredits { get; set; }
    public int BonusCredits { get; set; } = 0;
    public int TotalCredits => BaseCredits + BonusCredits;

    // Validity
    public int ValidityDays { get; set; } // Days until expiration

    // Stripe Integration
    public string? StripePriceId { get; set; }
    public string? StripeProductId { get; set; }

    // Availability
    public bool IsActive { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;

    // Features
    public string? Features { get; set; } // JSON array of features
    public bool IsFeatured { get; set; } = false;
    public string? BadgeText { get; set; } // e.g., "Most Popular", "Best Value"
}
