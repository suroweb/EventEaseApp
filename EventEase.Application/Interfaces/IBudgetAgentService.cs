using EventEase.Domain.Enums;

namespace EventEase.Application.Interfaces;

/// <summary>
/// Budget Agent - Cost estimation and financial planning
/// </summary>
public interface IBudgetAgentService
{
    /// <summary>
    /// Estimate event budget based on parameters
    /// </summary>
    Task<BudgetAgentResult> EstimateEventBudgetAsync(
        Guid tenantId,
        string eventType,
        int expectedAttendees,
        string? location = null,
        Dictionary<string, object>? requirements = null,
        AIProvider? preferredProvider = null);

    /// <summary>
    /// Compare vendor quotes and recommendations
    /// </summary>
    Task<BudgetAgentResult> CompareVendorsAsync(
        Guid tenantId,
        string serviceType,
        List<VendorQuote> quotes,
        AIProvider? preferredProvider = null);

    /// <summary>
    /// Track and analyze event expenses
    /// </summary>
    Task<BudgetAgentResult> AnalyzeExpensesAsync(
        Guid tenantId,
        Guid eventId,
        List<Expense> expenses,
        AIProvider? preferredProvider = null);

    /// <summary>
    /// Generate budget optimization recommendations
    /// </summary>
    Task<BudgetAgentResult> GetBudgetOptimizationAsync(
        Guid tenantId,
        Guid eventId,
        decimal currentBudget,
        AIProvider? preferredProvider = null);
}

/// <summary>
/// Budget agent operation result
/// </summary>
public class BudgetAgentResult
{
    public bool Success { get; set; }
    public string Response { get; set; } = string.Empty;
    public decimal CreditsCost { get; set; }
    public AIProvider Provider { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public string? ErrorMessage { get; set; }
    public BudgetEstimate? BudgetEstimate { get; set; }
    public List<VendorComparison>? VendorComparisons { get; set; }
    public ExpenseAnalysis? ExpenseAnalysis { get; set; }
}

/// <summary>
/// Budget estimate details
/// </summary>
public class BudgetEstimate
{
    public decimal TotalEstimate { get; set; }
    public decimal MinimumBudget { get; set; }
    public decimal RecommendedBudget { get; set; }
    public decimal MaximumBudget { get; set; }
    public Dictionary<string, decimal> CategoryBreakdown { get; set; } = new();
    public List<string> Assumptions { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Vendor quote for comparison
/// </summary>
public class VendorQuote
{
    public string VendorName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string>? Details { get; set; }
}

/// <summary>
/// Vendor comparison result
/// </summary>
public class VendorComparison
{
    public string VendorName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public List<string> Pros { get; set; } = new();
    public List<string> Cons { get; set; } = new();
    public string ValueForMoney { get; set; } = string.Empty;
}

/// <summary>
/// Expense item
/// </summary>
public class Expense
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

/// <summary>
/// Expense analysis result
/// </summary>
public class ExpenseAnalysis
{
    public decimal TotalSpent { get; set; }
    public decimal BudgetRemaining { get; set; }
    public Dictionary<string, decimal> SpendingByCategory { get; set; } = new();
    public List<string> OverBudgetCategories { get; set; } = new();
    public List<string> Insights { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}
