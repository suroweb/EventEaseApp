using EventEase.Domain.Common;
using EventEase.Domain.Enums;

namespace EventEase.Domain.Entities;

/// <summary>
/// Represents an AI agent usage instance with full audit trail
/// </summary>
public class AIAgentUsage : BaseAuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }

    // Agent Details
    public AIAgentType AgentType { get; set; }
    public AIProvider Provider { get; set; }
    public string? ModelUsed { get; set; } // e.g., "gpt-4", "claude-3-5-sonnet-20241022"

    // Request Details
    public string? PromptInput { get; set; } // User input
    public string? SystemPrompt { get; set; } // System instructions sent to AI
    public string? AgentResponse { get; set; } // AI output

    // Cost & Credits
    public decimal CreditsCost { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public int? TotalTokens { get; set; }
    public decimal? ProviderCost { get; set; } // Actual cost from AI provider (for analytics)

    // Performance Metrics
    public int ResponseTimeMs { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public int? RetryCount { get; set; } = 0;

    // Related Entities
    public Guid? EventId { get; set; } // If related to an event
    public Guid? CreditTransactionId { get; set; }

    // Metadata
    public string? RequestMetadata { get; set; } // JSON object
    public string? ResponseMetadata { get; set; } // JSON object

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual Event? Event { get; set; }
    public virtual CreditTransaction? CreditTransaction { get; set; }
}
