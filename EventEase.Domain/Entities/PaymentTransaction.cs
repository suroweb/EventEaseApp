using EventEase.Domain.Common;
using EventEase.Domain.Enums;

namespace EventEase.Domain.Entities;

/// <summary>
/// Represents a payment transaction from Stripe webhooks
/// </summary>
public class PaymentTransaction : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    // Stripe Details
    public string StripePaymentIntentId { get; set; } = string.Empty;
    public string? StripeChargeId { get; set; }
    public string? StripeInvoiceId { get; set; }
    public string? StripeCustomerId { get; set; }

    // Amount
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal? RefundedAmount { get; set; } = 0;
    public decimal NetAmount => Amount - (RefundedAmount ?? 0);

    // Status
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? FailureReason { get; set; }

    // Related Entities
    public Guid? CreditPackageId { get; set; }
    public Guid? CreditTransactionId { get; set; }

    // Payment Method
    public string? PaymentMethod { get; set; } // "card", "sepa_debit", etc.
    public string? CardBrand { get; set; }
    public string? CardLast4 { get; set; }

    // Timestamps
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public DateTime? DisputedAt { get; set; }

    // Webhook Details
    public string? WebhookEventId { get; set; }
    public string? WebhookEventType { get; set; }
    public string? RawWebhookData { get; set; } // Full JSON payload from Stripe

    // Invoice/Receipt
    public string? ReceiptUrl { get; set; }
    public string? InvoiceUrl { get; set; }

    // Metadata
    public string? Metadata { get; set; } // JSON object

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual CreditPackage? CreditPackage { get; set; }
    public virtual CreditTransaction? CreditTransaction { get; set; }
}
