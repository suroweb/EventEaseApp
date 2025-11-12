using EventEase.Domain.Enums;

namespace EventEase.API.DTOs;

/// <summary>
/// Response model for credit purchase details
/// </summary>
public class CreditPurchaseResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public int CreditsGranted { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string PaymentStatusDisplay => PaymentStatus.ToString();
    public string? StripePaymentIntentId { get; set; }
    public string? StripeInvoiceId { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public int DaysUntilExpiry => (ExpiresAt - DateTime.UtcNow).Days;
}
