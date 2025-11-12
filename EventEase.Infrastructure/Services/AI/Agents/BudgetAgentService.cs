using EventEase.Application.Interfaces;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventEase.Infrastructure.Services.AI.Agents;

/// <summary>
/// Budget Agent - Cost estimation and financial planning (variable cost based on complexity)
/// </summary>
public class BudgetAgentService : IBudgetAgentService
{
    private const decimal BASIC_BUDGET_COST = 15m;
    private const decimal ADVANCED_BUDGET_COST = 25m;

    private readonly ApplicationDbContext _context;
    private readonly IOpenAIService _openAI;
    private readonly IAnthropicService _anthropic;
    private readonly ICreditDeductionService _creditDeduction;
    private readonly ILogger<BudgetAgentService> _logger;

    public BudgetAgentService(
        ApplicationDbContext context,
        IOpenAIService openAI,
        IAnthropicService anthropic,
        ICreditDeductionService creditDeduction,
        ILogger<BudgetAgentService> logger)
    {
        _context = context;
        _openAI = openAI;
        _anthropic = anthropic;
        _creditDeduction = creditDeduction;
        _logger = logger;
    }

    public async Task<BudgetAgentResult> EstimateEventBudgetAsync(
        Guid tenantId,
        string eventType,
        int expectedAttendees,
        string? location = null,
        Dictionary<string, object>? requirements = null,
        AIProvider? preferredProvider = null)
    {
        var cost = BASIC_BUDGET_COST;

        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, cost))
        {
            return new BudgetAgentResult
            {
                Success = false,
                ErrorMessage = $"Insufficient credits. Required: {cost}",
                CreditsCost = cost
            };
        }

        try
        {
            var provider = preferredProvider ?? AIProvider.OpenAI;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var requirementsJson = requirements != null ? JsonSerializer.Serialize(requirements) : "{}";

            var systemPrompt = @"You are an expert event budget consultant. Estimate comprehensive event budgets including:
- Venue rental
- Catering (food & beverages)
- AV equipment and technical support
- Marketing and promotions
- Staff and security
- Decorations and setup
- Insurance
- Contingency fund (10-15%)

Provide minimum, recommended, and maximum budget ranges with detailed category breakdowns.";

            var userPrompt = $@"Estimate budget for:
Event Type: {eventType}
Expected Attendees: {expectedAttendees}
Location: {location ?? "not specified"}

Additional Requirements:
{requirementsJson}

Provide realistic budget estimates with:
- Minimum budget (basic)
- Recommended budget (optimal)
- Maximum budget (premium)
- Detailed category breakdown
- Cost-saving recommendations
- Budget assumptions";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.5, 2000);

            if (!response.Success)
            {
                return new BudgetAgentResult
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage,
                    Provider = provider,
                    CreditsCost = cost
                };
            }

            await _creditDeduction.DeductCreditsAsync(
                tenantId,
                cost,
                AIAgentType.BudgetAgent,
                provider,
                $"Estimate budget for {eventType} with {expectedAttendees} attendees",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: userPrompt,
                agentResponse: response.Content
            );

            return new BudgetAgentResult
            {
                Success = true,
                Response = response.Content,
                CreditsCost = cost,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error estimating budget for tenant {TenantId}", tenantId);

            return new BudgetAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = cost
            };
        }
    }

    public async Task<BudgetAgentResult> CompareVendorsAsync(
        Guid tenantId,
        string serviceType,
        List<VendorQuote> quotes,
        AIProvider? preferredProvider = null)
    {
        var cost = BASIC_BUDGET_COST;

        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, cost))
        {
            return new BudgetAgentResult
            {
                Success = false,
                ErrorMessage = $"Insufficient credits. Required: {cost}",
                CreditsCost = cost
            };
        }

        try
        {
            var provider = preferredProvider ?? AIProvider.Anthropic;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var quotesJson = JsonSerializer.Serialize(quotes);

            var systemPrompt = @"You are a vendor comparison expert. Analyze vendor quotes considering:
- Price competitiveness
- Value for money
- Service quality indicators
- Scope of services
- Hidden costs
- Reputation and reliability
- Terms and conditions

Score each vendor and provide clear recommendations.";

            var userPrompt = $@"Compare vendors for:
Service Type: {serviceType}

Vendor Quotes:
{quotesJson}

Provide:
- Detailed comparison analysis
- Score for each vendor (0-100)
- Pros and cons for each
- Best value recommendation
- Best quality recommendation
- Best overall recommendation
- Red flags to watch for";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.5, 1500);

            if (!response.Success)
            {
                return new BudgetAgentResult
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage,
                    Provider = provider,
                    CreditsCost = cost
                };
            }

            await _creditDeduction.DeductCreditsAsync(
                tenantId,
                cost,
                AIAgentType.BudgetAgent,
                provider,
                $"Compare {quotes.Count} vendors for {serviceType}",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: $"Compare {quotes.Count} vendor quotes",
                agentResponse: response.Content
            );

            return new BudgetAgentResult
            {
                Success = true,
                Response = response.Content,
                CreditsCost = cost,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing vendors for tenant {TenantId}", tenantId);

            return new BudgetAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = cost
            };
        }
    }

    public async Task<BudgetAgentResult> AnalyzeExpensesAsync(
        Guid tenantId,
        Guid eventId,
        List<Expense> expenses,
        AIProvider? preferredProvider = null)
    {
        var cost = BASIC_BUDGET_COST;

        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, cost))
        {
            return new BudgetAgentResult
            {
                Success = false,
                ErrorMessage = $"Insufficient credits. Required: {cost}",
                CreditsCost = cost
            };
        }

        try
        {
            var eventEntity = await _context.Events.FindAsync(eventId);

            if (eventEntity == null)
            {
                return new BudgetAgentResult
                {
                    Success = false,
                    ErrorMessage = "Event not found",
                    CreditsCost = cost
                };
            }

            var provider = preferredProvider ?? AIProvider.OpenAI;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var expensesJson = JsonSerializer.Serialize(expenses);

            var systemPrompt = @"You are a financial analyst specializing in event expense tracking. Analyze expenses and provide:
- Total spending breakdown by category
- Budget utilization analysis
- Over-budget categories identification
- Cost trends
- Unusual or unexpected costs
- Cost-saving opportunities
- Recommendations for budget reallocation";

            var userPrompt = $@"Analyze expenses for:
Event: {eventEntity.Name}

Expenses:
{expensesJson}

Total Expenses: €{expenses.Sum(e => e.Amount)}

Provide comprehensive expense analysis with insights and recommendations.";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.5, 1500);

            if (!response.Success)
            {
                return new BudgetAgentResult
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage,
                    Provider = provider,
                    CreditsCost = cost
                };
            }

            await _creditDeduction.DeductCreditsAsync(
                tenantId,
                cost,
                AIAgentType.BudgetAgent,
                provider,
                $"Analyze {expenses.Count} expenses for event: {eventEntity.Name}",
                relatedEntityId: eventId,
                relatedEntityType: "Event",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: $"Analyze {expenses.Count} expenses",
                agentResponse: response.Content
            );

            return new BudgetAgentResult
            {
                Success = true,
                Response = response.Content,
                CreditsCost = cost,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing expenses for event {EventId}", eventId);

            return new BudgetAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = cost
            };
        }
    }

    public async Task<BudgetAgentResult> GetBudgetOptimizationAsync(
        Guid tenantId,
        Guid eventId,
        decimal currentBudget,
        AIProvider? preferredProvider = null)
    {
        var cost = ADVANCED_BUDGET_COST;

        if (!await _creditDeduction.HasSufficientCreditsAsync(tenantId, cost))
        {
            return new BudgetAgentResult
            {
                Success = false,
                ErrorMessage = $"Insufficient credits. Required: {cost}",
                CreditsCost = cost
            };
        }

        try
        {
            var eventEntity = await _context.Events.FindAsync(eventId);

            if (eventEntity == null)
            {
                return new BudgetAgentResult
                {
                    Success = false,
                    ErrorMessage = "Event not found",
                    CreditsCost = cost
                };
            }

            var provider = preferredProvider ?? AIProvider.Anthropic;
            var aiService = provider == AIProvider.OpenAI ? (IAIProviderService)_openAI : _anthropic;

            var systemPrompt = @"You are a budget optimization specialist. Provide strategies to maximize value and minimize waste including:
- Alternative vendor options
- Negotiation strategies
- DIY vs outsourcing analysis
- Timing optimizations
- Bulk discount opportunities
- Sponsorship opportunities
- Revenue generation ideas
- Risk mitigation strategies

Focus on practical, actionable recommendations.";

            var userPrompt = $@"Optimize budget for:
Event: {eventEntity.Name}
Type: {eventEntity.Category}
Expected Attendees: {eventEntity.MaxAttendees ?? 100}
Current Budget: €{currentBudget}
Event Date: {eventEntity.StartDate:yyyy-MM-dd}

Provide:
- Cost reduction strategies
- Value maximization tactics
- Revenue generation opportunities
- Negotiation tips
- Timeline considerations
- Risk management
- Prioritized action plan";

            var response = await aiService.SendPromptAsync(systemPrompt, userPrompt, 0.7, 2000);

            if (!response.Success)
            {
                return new BudgetAgentResult
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage,
                    Provider = provider,
                    CreditsCost = cost
                };
            }

            await _creditDeduction.DeductCreditsAsync(
                tenantId,
                cost,
                AIAgentType.BudgetAgent,
                provider,
                $"Budget optimization for event: {eventEntity.Name}",
                relatedEntityId: eventId,
                relatedEntityType: "Event",
                inputTokens: response.InputTokens,
                outputTokens: response.OutputTokens,
                promptInput: userPrompt,
                agentResponse: response.Content
            );

            return new BudgetAgentResult
            {
                Success = true,
                Response = response.Content,
                CreditsCost = cost,
                Provider = provider,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing budget for event {EventId}", eventId);

            return new BudgetAgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                CreditsCost = cost
            };
        }
    }
}
