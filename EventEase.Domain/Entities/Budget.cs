using EventEase.Domain.Common;

namespace EventEase.Domain.Entities;

/// <summary>
/// Represents a budget for an event (managed by Budget Agent)
/// </summary>
public class Budget : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }

    // Budget Details
    public decimal TotalBudget { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal AllocatedAmount { get; set; } = 0;
    public decimal SpentAmount { get; set; } = 0;
    public decimal RemainingAmount => TotalBudget - SpentAmount;

    // AI Generated
    public bool IsAIGenerated { get; set; } = false;
    public Guid? AIAgentUsageId { get; set; }

    // Status
    public bool IsApproved { get; set; } = false;
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Notes
    public string? Notes { get; set; }
    public string? AIRecommendations { get; set; }

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Event Event { get; set; } = null!;
    public virtual User? ApprovedByUser { get; set; }
    public virtual AIAgentUsage? AIAgentUsage { get; set; }
    public virtual ICollection<BudgetItem> BudgetItems { get; set; } = new List<BudgetItem>();
}
