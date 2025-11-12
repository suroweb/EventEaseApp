namespace EventEase.API.DTOs;

/// <summary>
/// Response model for tenant credit balance and summary
/// </summary>
public class CreditBalanceResponse
{
    public Guid TenantId { get; set; }
    public decimal AvailableCredits { get; set; }
    public decimal TotalCreditsEarned { get; set; }
    public decimal TotalCreditsSpent { get; set; }
    public decimal TotalCreditsPurchased { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
    public DateTime? NextExpiryDate { get; set; }
    public int CreditsExpiringIn30Days { get; set; }
    public bool IsTrialAccount { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public int? DaysLeftInTrial { get; set; }

    /// <summary>
    /// Recent credit transactions
    /// </summary>
    public List<CreditTransactionResponse> RecentTransactions { get; set; } = new();

    /// <summary>
    /// Active credit purchases with expiry information
    /// </summary>
    public List<CreditPurchaseResponse> ActivePurchases { get; set; } = new();
}
