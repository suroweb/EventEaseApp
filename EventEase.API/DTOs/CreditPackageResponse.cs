namespace EventEase.API.DTOs;

/// <summary>
/// Response model for credit package details
/// </summary>
public class CreditPackageResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public int BaseCredits { get; set; }
    public int? BonusCredits { get; set; }
    public int TotalCredits => BaseCredits + (BonusCredits ?? 0);
    public int ValidityDays { get; set; }
    public decimal PricePerCredit => TotalCredits > 0 ? Price / TotalCredits : 0;
    public bool IsActive { get; set; }
    public bool IsPopular { get; set; }
    public int DisplayOrder { get; set; }
}
