using EventEase.Domain.Common;
using EventEase.Domain.Enums;

namespace EventEase.Domain.Entities;

/// <summary>
/// Represents a credit transaction (purchase, deduction, refund, etc.)
/// </summary>
public class CreditTransaction : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; } // User who initiated the transaction

    // Transaction Details
    public CreditTransactionType Type { get; set; }
    public decimal Amount { get; set; } // Positive for additions, negative for deductions
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }

    // Description
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }

    // Related Entities
    public Guid? AIAgentUsageId { get; set; } // If deduction was for AI usage
    public Guid? PaymentTransactionId { get; set; } // If purchase was via payment
    public Guid? CreditPackageId { get; set; } // Package purchased

    // Expiration (for purchased credits)
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired { get; set; } = false;

    // Metadata
    public string? Metadata { get; set; } // JSON object for additional data

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual User? User { get; set; }
    public virtual AIAgentUsage? AIAgentUsage { get; set; }
    public virtual PaymentTransaction? PaymentTransaction { get; set; }
    public virtual CreditPackage? CreditPackage { get; set; }
}
