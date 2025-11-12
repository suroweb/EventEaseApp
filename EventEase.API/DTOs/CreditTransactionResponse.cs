using EventEase.Domain.Enums;

namespace EventEase.API.DTOs;

/// <summary>
/// Response model for credit transaction details
/// </summary>
public class CreditTransactionResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public CreditTransactionType Type { get; set; }
    public string TypeDisplay => Type.ToString();
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? PurchaseId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public DateTime CreatedAt { get; set; }
}
