using EventEase.Domain.Common;

namespace EventEase.Domain.Entities;

/// <summary>
/// Represents an individual item in an event budget
/// </summary>
public class BudgetItem : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid BudgetId { get; set; }

    // Item Details
    public string Category { get; set; } = string.Empty; // "Venue", "Catering", "Marketing", etc.
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Amounts
    public decimal EstimatedCost { get; set; }
    public decimal ActualCost { get; set; } = 0;
    public decimal Variance => ActualCost - EstimatedCost;

    // Vendor
    public string? VendorName { get; set; }
    public string? VendorContact { get; set; }
    public string? VendorEmail { get; set; }
    public string? VendorPhone { get; set; }

    // Status
    public bool IsPaid { get; set; } = false;
    public DateTime? PaidAt { get; set; }
    public string? PaymentReference { get; set; }

    // AI Recommendations
    public bool IsAIRecommended { get; set; } = false;
    public string? AIRecommendationReason { get; set; }
    public string? AlternativeVendors { get; set; } // JSON array

    // Documents
    public string? InvoiceUrl { get; set; }
    public string? ReceiptUrl { get; set; }
    public string? ContractUrl { get; set; }

    // Metadata
    public string? Notes { get; set; }
    public string? Metadata { get; set; } // JSON object

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Budget Budget { get; set; } = null!;
}
