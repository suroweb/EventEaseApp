namespace EventEase.API.DTOs;

/// <summary>
/// Credit usage and patterns analytics
/// </summary>
public class CreditAnalyticsResponse
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    // Current Balance
    public decimal CurrentBalance { get; set; }
    public decimal TotalPurchased { get; set; }
    public decimal TotalUsed { get; set; }
    public decimal TotalRefunded { get; set; }

    // Usage Patterns
    public Dictionary<string, decimal> UsageByAgent { get; set; } = new();
    public Dictionary<string, int> RequestsByAgent { get; set; } = new();
    public Dictionary<string, decimal> UsageByDate { get; set; } = new();

    // Transaction Metrics
    public int TotalPurchaseTransactions { get; set; }
    public int TotalUsageTransactions { get; set; }
    public decimal AverageTransactionAmount { get; set; }
    public decimal AverageCostPerRequest { get; set; }

    // Spending Velocity
    public decimal DailyAverageSpend { get; set; }
    public decimal WeeklyAverageSpend { get; set; }
    public decimal MonthlyAverageSpend { get; set; }
    public int EstimatedDaysUntilDepletion { get; set; }

    // Package Analysis
    public Dictionary<string, int> PurchasesByPackageType { get; set; } = new();
    public decimal MostCommonPackageSize { get; set; }

    // Efficiency Metrics
    public decimal SuccessfulRequestRate { get; set; }
    public decimal AverageTokensPerRequest { get; set; }
    public decimal CostPerToken { get; set; }

    // Trends
    public Dictionary<string, decimal> MonthlySpend { get; set; } = new();
    public decimal SpendTrend { get; set; } // Percentage change month-over-month
}
