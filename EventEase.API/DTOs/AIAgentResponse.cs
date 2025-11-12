using EventEase.Domain.Enums;

namespace EventEase.API.DTOs;

/// <summary>
/// Base response for AI agent operations
/// </summary>
public class AIAgentResponse
{
    public bool Success { get; set; }
    public string Response { get; set; } = string.Empty;
    public decimal CreditsCost { get; set; }
    public string Provider { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens => InputTokens + OutputTokens;
    public string? ErrorMessage { get; set; }
    public object? Data { get; set; }
}

/// <summary>
/// AI Agent usage statistics response
/// </summary>
public class AIAgentUsageResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string AgentType { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public decimal CreditsCost { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string? PromptSummary { get; set; }
}
