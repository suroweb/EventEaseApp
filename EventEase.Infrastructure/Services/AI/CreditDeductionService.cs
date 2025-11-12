using EventEase.Application.Interfaces;
using EventEase.Domain.Entities;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventEase.Infrastructure.Services.AI;

/// <summary>
/// Service for deducting credits for AI operations
/// </summary>
public class CreditDeductionService : ICreditDeductionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CreditDeductionService> _logger;

    public CreditDeductionService(
        ApplicationDbContext context,
        ILogger<CreditDeductionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CreditDeductionResult> DeductCreditsAsync(
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
        string? agentResponse = null)
    {
        try
        {
            // Get tenant
            var tenant = await _context.Tenants.FindAsync(tenantId);

            if (tenant == null)
            {
                return new CreditDeductionResult
                {
                    Success = false,
                    ErrorMessage = "Tenant not found"
                };
            }

            // Check sufficient credits
            if (tenant.AvailableCredits < amount)
            {
                _logger.LogWarning("Insufficient credits for tenant {TenantId}: Available={Available}, Required={Required}",
                    tenantId, tenant.AvailableCredits, amount);

                return new CreditDeductionResult
                {
                    Success = false,
                    ErrorMessage = $"Insufficient credits. Available: {tenant.AvailableCredits}, Required: {amount}",
                    BalanceAfter = tenant.AvailableCredits
                };
            }

            // Deduct credits
            tenant.AvailableCredits -= amount;

            // Create AI agent usage record
            var usageRecord = new AIAgentUsage
            {
                TenantId = tenantId,
                AgentType = agentType,
                Provider = provider,
                CreditsCost = amount,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                PromptInput = promptInput,
                AgentResponse = agentResponse,
                ExecutedAt = DateTime.UtcNow
            };

            _context.AIAgentUsages.Add(usageRecord);

            // Create credit transaction record
            var transaction = new CreditTransaction
            {
                TenantId = tenantId,
                Type = CreditTransactionType.Deduction,
                Amount = -amount,
                BalanceAfter = tenant.AvailableCredits,
                Description = description,
                RelatedEntityId = relatedEntityId,
                RelatedEntityType = relatedEntityType
            };

            _context.CreditTransactions.Add(transaction);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Credits deducted: {Amount} credits from tenant {TenantId} for {AgentType} using {Provider}",
                amount, tenantId, agentType, provider);

            return new CreditDeductionResult
            {
                Success = true,
                AmountDeducted = amount,
                BalanceAfter = tenant.AvailableCredits,
                TransactionId = transaction.Id,
                UsageRecordId = usageRecord.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deducting credits for tenant {TenantId}", tenantId);

            return new CreditDeductionResult
            {
                Success = false,
                ErrorMessage = $"Error deducting credits: {ex.Message}"
            };
        }
    }

    public async Task<bool> HasSufficientCreditsAsync(Guid tenantId, decimal amount)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);

        if (tenant == null)
        {
            return false;
        }

        return tenant.AvailableCredits >= amount;
    }

    public async Task<decimal> GetAvailableCreditsAsync(Guid tenantId)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);

        return tenant?.AvailableCredits ?? 0;
    }
}
