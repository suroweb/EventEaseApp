using EventEase.Domain.Enums;

namespace EventEase.Application.Interfaces;

/// <summary>
/// Intelligent AI model router that selects the best model for each task
/// Supports: OpenAI (GPT-4o), Anthropic (Claude), DeepSeek, Ollama (Llama, Mistral, etc.)
/// </summary>
public interface IAIModelRouter
{
    /// <summary>
    /// Route request to best model based on task type, cost, and performance requirements
    /// </summary>
    Task<AIResponse> RouteRequestAsync(
        AIRoutingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recommended model for a specific task type
    /// </summary>
    AIModelRecommendation GetRecommendedModel(
        AITaskType taskType,
        ModelSelectionCriteria? criteria = null);

    /// <summary>
    /// Get all available models across all providers
    /// </summary>
    Task<List<AIModelDetails>> GetAvailableModelsAsync();

    /// <summary>
    /// Get cost estimate for a request
    /// </summary>
    Task<CostEstimate> EstimateCostAsync(
        string prompt,
        string? modelId = null);

    /// <summary>
    /// Check health of all AI providers
    /// </summary>
    Task<Dictionary<string, ProviderHealth>> CheckProvidersHealthAsync();
}

public class AIRoutingRequest
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public AITaskType TaskType { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public ModelSelectionCriteria? SelectionCriteria { get; set; }
    public string? PreferredModel { get; set; }
    public string? PreferredProvider { get; set; }
}

public class ModelSelectionCriteria
{
    public ModelPriority Priority { get; set; } = ModelPriority.Balanced;
    public decimal? MaxCostPerRequest { get; set; }
    public int? MinContextWindow { get; set; }
    public bool RequireLocal { get; set; } = false;
    public bool RequireOpenSource { get; set; } = false;
    public List<string>? ExcludedProviders { get; set; }
    public List<string>? RequiredCapabilities { get; set; }
}

public enum ModelPriority
{
    Cost,           // Cheapest model
    Performance,    // Best quality model
    Speed,          // Fastest response
    Balanced,       // Balance of cost/performance
    Privacy         // Local/open-source only
}

public enum AITaskType
{
    ContentGeneration,      // Blog posts, descriptions (DeepSeek good)
    CodeGeneration,         // Code writing (GPT-4o, DeepSeek-Coder)
    Reasoning,              // Complex logic (Claude 3.5 Sonnet, GPT-4o)
    ChatConversation,       // Chat/dialogue (Llama 3, Mistral)
    DataAnalysis,           // Analytics (Claude, GPT-4o)
    Translation,            // Language translation (any)
    Summarization,          // Text summarization (DeepSeek, Llama)
    Classification,         // Text classification (fast/cheap)
    Embeddings,             // Vector embeddings
    QuestionAnswering,      // Q&A (Llama, Mistral)
    CreativeWriting        // Creative content (GPT-4o, Claude)
}

public class AIModelRecommendation
{
    public string ModelId { get; set; } = null!;
    public string ModelName { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public decimal EstimatedCostPer1KTokens { get; set; }
    public int ContextWindow { get; set; }
    public List<string> Strengths { get; set; } = new();
    public AIModelDetails Details { get; set; } = null!;
}

public class AIModelDetails
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int ContextWindow { get; set; }
    public decimal InputCostPerMillionTokens { get; set; }
    public decimal OutputCostPerMillionTokens { get; set; }
    public bool IsOpenSource { get; set; }
    public bool IsLocal { get; set; }
    public List<string> Capabilities { get; set; } = new();
    public Dictionary<string, double> BenchmarkScores { get; set; } = new();
    public bool IsAvailable { get; set; } = true;
}

public class CostEstimate
{
    public decimal EstimatedCost { get; set; }
    public int EstimatedInputTokens { get; set; }
    public int EstimatedOutputTokens { get; set; }
    public string ModelUsed { get; set; } = null!;
    public string Provider { get; set; } = null!;
}

public class ProviderHealth
{
    public string Provider { get; set; } = null!;
    public bool IsHealthy { get; set; }
    public int AvailableModels { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public DateTime LastChecked { get; set; }
}
