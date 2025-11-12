using EventEase.Domain.Enums;

namespace EventEase.Application.Interfaces;

/// <summary>
/// Service for deducting credits for AI operations
/// </summary>
public interface ICreditDeductionService
{
    /// <summary>
    /// Deduct credits for an AI operation
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="amount">Amount of credits to deduct</param>
    /// <param name="agentType">AI agent type</param>
    /// <param name="provider">AI provider</param>
    /// <param name="description">Operation description</param>
    /// <param name="relatedEntityId">Related entity ID</param>
    /// <param name="relatedEntityType">Related entity type</param>
    /// <param name="inputTokens">Input tokens used</param>
    /// <param name="outputTokens">Output tokens used</param>
    /// <param name="promptInput">User prompt</param>
    /// <param name="agentResponse">AI response</param>
    /// <returns>Success status and remaining balance</returns>
    Task<CreditDeductionResult> DeductCreditsAsync(
        Guid tenantId,
        decimal amount,
        AIAgentType agentType,
        AIProvider provider,
        string description,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        int? inputTokens = null,
        int? outputTokens = null,
        string? promptInput = null,
        string? agentResponse = null);

    /// <summary>
    /// Check if tenant has sufficient credits
    /// </summary>
    Task<bool> HasSufficientCreditsAsync(Guid tenantId, decimal amount);

    /// <summary>
    /// Get tenant's available credits
    /// </summary>
    Task<decimal> GetAvailableCreditsAsync(Guid tenantId);
}

/// <summary>
/// Result of credit deduction operation
/// </summary>
public class CreditDeductionResult
{
    public bool Success { get; set; }
    public decimal AmountDeducted { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? TransactionId { get; set; }
    public Guid? UsageRecordId { get; set; }
}
