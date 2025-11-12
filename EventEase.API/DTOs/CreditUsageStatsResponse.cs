namespace EventEase.API.DTOs;

/// <summary>
/// Response model for credit usage statistics
/// </summary>
public class CreditUsageStatsResponse
{
    public Guid TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalCreditsSpent { get; set; }
    public int TotalOperations { get; set; }
    public decimal AverageCostPerOperation { get; set; }

    /// <summary>
    /// Credit usage by AI agent type
    /// </summary>
    public List<AgentUsageStats> UsageByAgent { get; set; } = new();

    /// <summary>
    /// Credit usage by AI provider (OpenAI vs Anthropic)
    /// </summary>
    public List<ProviderUsageStats> UsageByProvider { get; set; } = new();

    /// <summary>
    /// Daily credit usage trend
    /// </summary>
    public List<DailyUsageStats> DailyUsage { get; set; } = new();
}

/// <summary>
/// Credit usage statistics by AI agent
/// </summary>
public class AgentUsageStats
{
    public string AgentType { get; set; } = string.Empty;
    public int OperationCount { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal AverageCostPerOperation { get; set; }
    public int TotalInputTokens { get; set; }
    public int TotalOutputTokens { get; set; }
}

/// <summary>
/// Credit usage statistics by AI provider
/// </summary>
public class ProviderUsageStats
{
    public string Provider { get; set; } = string.Empty;
    public int OperationCount { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal AverageCostPerOperation { get; set; }
}

/// <summary>
/// Daily credit usage statistics
/// </summary>
public class DailyUsageStats
{
    public DateTime Date { get; set; }
    public decimal CreditsSpent { get; set; }
    public int OperationCount { get; set; }
}
