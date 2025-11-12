using EventEase.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventEase.Infrastructure.Services.AI;

/// <summary>
/// Intelligent AI model router that automatically selects the best model for each task
/// Balances cost, performance, speed, and privacy requirements
/// </summary>
public class AIModelRouter : IAIModelRouter
{
    private readonly IOpenAIService _openAIService;
    private readonly IAnthropicService _anthropicService;
    private readonly IDeepSeekService _deepSeekService;
    private readonly IOllamaService _ollamaService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIModelRouter> _logger;

    private readonly Dictionary<AITaskType, List<AIModelRecommendation>> _modelRecommendations;

    public AIModelRouter(
        IOpenAIService openAIService,
        IAnthropicService anthropicService,
        IDeepSeekService deepSeekService,
        IOllamaService ollamaService,
        IConfiguration configuration,
        ILogger<AIModelRouter> logger)
    {
        _openAIService = openAIService;
        _anthropicService = anthropicService;
        _deepSeekService = deepSeekService;
        _ollamaService = ollamaService;
        _configuration = configuration;
        _logger = logger;

        _modelRecommendations = BuildModelRecommendations();
    }

    public async Task<AIResponse> RouteRequestAsync(
        AIRoutingRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // If specific model is requested, use it
            if (!string.IsNullOrEmpty(request.PreferredModel))
            {
                return await ExecuteWithSpecificModel(request, request.PreferredModel);
            }

            // Get recommended model based on task type and criteria
            var recommendation = GetRecommendedModel(request.TaskType, request.SelectionCriteria);

            _logger.LogInformation(
                "Routing {TaskType} to {Provider}/{Model} (Reason: {Reason})",
                request.TaskType,
                recommendation.Provider,
                recommendation.ModelId,
                recommendation.Reason
            );

            // Execute with recommended model
            return await ExecuteWithRecommendation(request, recommendation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing AI request");
            return new AIResponse
            {
                Success = false,
                ErrorMessage = $"Routing error: {ex.Message}"
            };
        }
    }

    public AIModelRecommendation GetRecommendedModel(
        AITaskType taskType,
        ModelSelectionCriteria? criteria = null)
    {
        criteria ??= new ModelSelectionCriteria();

        var candidates = _modelRecommendations.GetValueOrDefault(taskType) ?? new List<AIModelRecommendation>();

        // Filter by criteria
        candidates = FilterByCriteria(candidates, criteria);

        if (!candidates.Any())
        {
            // Fallback to DeepSeek as default (good balance of cost/performance)
            return GetDeepSeekRecommendation();
        }

        // Select based on priority
        return criteria.Priority switch
        {
            ModelPriority.Cost => candidates.OrderBy(c => c.EstimatedCostPer1KTokens).First(),
            ModelPriority.Performance => candidates.OrderByDescending(c => c.Details.BenchmarkScores.GetValueOrDefault("overall", 0)).First(),
            ModelPriority.Speed => candidates.OrderBy(c => c.Details.ContextWindow).First(), // Smaller context = faster
            ModelPriority.Privacy => candidates.Where(c => c.Details.IsLocal || c.Details.IsOpenSource).FirstOrDefault() ?? candidates.First(),
            ModelPriority.Balanced => candidates.OrderBy(c => c.EstimatedCostPer1KTokens * (1 / (c.Details.BenchmarkScores.GetValueOrDefault("overall", 0.5) + 0.1))).First(),
            _ => candidates.First()
        };
    }

    public async Task<List<AIModelDetails>> GetAvailableModelsAsync()
    {
        var models = new List<AIModelDetails>
        {
            // OpenAI Models
            new AIModelDetails
            {
                Id = "gpt-4o",
                Name = "GPT-4o",
                Provider = "OpenAI",
                Description = "Most capable OpenAI model, multimodal",
                ContextWindow = 128000,
                InputCostPerMillionTokens = 5.00m,
                OutputCostPerMillionTokens = 15.00m,
                IsOpenSource = false,
                IsLocal = false,
                Capabilities = new List<string> { "text", "vision", "function-calling" },
                BenchmarkScores = new Dictionary<string, double> { { "overall", 0.95 }, { "reasoning", 0.96 }, { "coding", 0.94 } },
                IsAvailable = true
            },
            new AIModelDetails
            {
                Id = "gpt-4o-mini",
                Name = "GPT-4o Mini",
                Provider = "OpenAI",
                Description = "Faster, cheaper version of GPT-4o",
                ContextWindow = 128000,
                InputCostPerMillionTokens = 0.15m,
                OutputCostPerMillionTokens = 0.60m,
                IsOpenSource = false,
                IsLocal = false,
                Capabilities = new List<string> { "text", "vision" },
                BenchmarkScores = new Dictionary<string, double> { { "overall", 0.85 }, { "reasoning", 0.84 }, { "coding", 0.86 } },
                IsAvailable = true
            },

            // Anthropic Models
            new AIModelDetails
            {
                Id = "claude-3-5-sonnet-20241022",
                Name = "Claude 3.5 Sonnet",
                Provider = "Anthropic",
                Description = "Best reasoning and analysis model",
                ContextWindow = 200000,
                InputCostPerMillionTokens = 3.00m,
                OutputCostPerMillionTokens = 15.00m,
                IsOpenSource = false,
                IsLocal = false,
                Capabilities = new List<string> { "text", "vision", "reasoning" },
                BenchmarkScores = new Dictionary<string, double> { { "overall", 0.97 }, { "reasoning", 0.98 }, { "coding", 0.92 } },
                IsAvailable = true
            },

            // DeepSeek Models
            new AIModelDetails
            {
                Id = "deepseek-chat",
                Name = "DeepSeek Chat",
                Provider = "DeepSeek",
                Description = "Open-source AI with strong reasoning, very cost-effective",
                ContextWindow = 64000,
                InputCostPerMillionTokens = 0.14m,
                OutputCostPerMillionTokens = 0.28m,
                IsOpenSource = true,
                IsLocal = false,
                Capabilities = new List<string> { "text", "reasoning" },
                BenchmarkScores = new Dictionary<string, double> { { "overall", 0.88 }, { "reasoning", 0.90 }, { "coding", 0.87 } },
                IsAvailable = true
            },
            new AIModelDetails
            {
                Id = "deepseek-coder",
                Name = "DeepSeek Coder",
                Provider = "DeepSeek",
                Description = "Specialized for code generation, very affordable",
                ContextWindow = 64000,
                InputCostPerMillionTokens = 0.14m,
                OutputCostPerMillionTokens = 0.28m,
                IsOpenSource = true,
                IsLocal = false,
                Capabilities = new List<string> { "text", "code" },
                BenchmarkScores = new Dictionary<string, double> { { "overall", 0.86 }, { "reasoning", 0.82 }, { "coding", 0.93 } },
                IsAvailable = true
            },

            // Ollama Local Models
            new AIModelDetails
            {
                Id = "llama3:latest",
                Name = "Llama 3 (8B)",
                Provider = "Ollama",
                Description = "FREE local model, good all-around performance",
                ContextWindow = 8192,
                InputCostPerMillionTokens = 0.00m, // FREE (local)
                OutputCostPerMillionTokens = 0.00m,
                IsOpenSource = true,
                IsLocal = true,
                Capabilities = new List<string> { "text" },
                BenchmarkScores = new Dictionary<string, double> { { "overall", 0.75 }, { "reasoning", 0.72 }, { "coding", 0.70 } },
                IsAvailable = await _ollamaService.IsAvailableAsync()
            },
            new AIModelDetails
            {
                Id = "mistral:latest",
                Name = "Mistral (7B)",
                Provider = "Ollama",
                Description = "FREE local model, fast and efficient",
                ContextWindow = 32768,
                InputCostPerMillionTokens = 0.00m, // FREE (local)
                OutputCostPerMillionTokens = 0.00m,
                IsOpenSource = true,
                IsLocal = true,
                Capabilities = new List<string> { "text" },
                BenchmarkScores = new Dictionary<string, double> { { "overall", 0.73 }, { "reasoning", 0.70 }, { "coding", 0.68 } },
                IsAvailable = await _ollamaService.IsAvailableAsync()
            },
            new AIModelDetails
            {
                Id = "codellama:latest",
                Name = "CodeLlama (7B)",
                Provider = "Ollama",
                Description = "FREE local model specialized for code",
                ContextWindow = 16384,
                InputCostPerMillionTokens = 0.00m, // FREE (local)
                OutputCostPerMillionTokens = 0.00m,
                IsOpenSource = true,
                IsLocal = true,
                Capabilities = new List<string> { "code" },
                BenchmarkScores = new Dictionary<string, double> { { "overall", 0.70 }, { "reasoning", 0.65 }, { "coding", 0.80 } },
                IsAvailable = await _ollamaService.IsAvailableAsync()
            }
        };

        return models.Where(m => m.IsAvailable).ToList();
    }

    public async Task<CostEstimate> EstimateCostAsync(
        string prompt,
        string? modelId = null)
    {
        // Rough token estimation (1 token ≈ 4 characters)
        var estimatedInputTokens = prompt.Length / 4;
        var estimatedOutputTokens = 500; // Assume average response

        var models = await GetAvailableModelsAsync();
        var model = models.FirstOrDefault(m => m.Id == modelId) ?? models.First();

        var inputCost = (model.InputCostPerMillionTokens / 1_000_000m) * estimatedInputTokens;
        var outputCost = (model.OutputCostPerMillionTokens / 1_000_000m) * estimatedOutputTokens;

        return new CostEstimate
        {
            EstimatedCost = inputCost + outputCost,
            EstimatedInputTokens = estimatedInputTokens,
            EstimatedOutputTokens = estimatedOutputTokens,
            ModelUsed = model.Id,
            Provider = model.Provider
        };
    }

    public async Task<Dictionary<string, ProviderHealth>> CheckProvidersHealthAsync()
    {
        var health = new Dictionary<string, ProviderHealth>();

        // Check OpenAI
        health["OpenAI"] = await CheckProviderHealth("OpenAI", async () =>
        {
            // Basic check - would need actual API call in production
            return true;
        });

        // Check Anthropic
        health["Anthropic"] = await CheckProviderHealth("Anthropic", async () =>
        {
            return true;
        });

        // Check DeepSeek
        health["DeepSeek"] = await CheckProviderHealth("DeepSeek", async () =>
        {
            return true;
        });

        // Check Ollama
        health["Ollama"] = await CheckProviderHealth("Ollama", async () =>
        {
            return await _ollamaService.IsAvailableAsync();
        });

        return health;
    }

    // Private helper methods

    private async Task<AIResponse> ExecuteWithSpecificModel(AIRoutingRequest request, string modelId)
    {
        // Determine provider from model ID
        if (modelId.StartsWith("gpt-"))
        {
            return await _openAIService.SendPromptAsync(request.SystemPrompt, request.UserPrompt, request.Temperature, request.MaxTokens);
        }
        else if (modelId.StartsWith("claude-"))
        {
            return await _anthropicService.SendPromptAsync(request.SystemPrompt, request.UserPrompt, request.Temperature, request.MaxTokens);
        }
        else if (modelId.StartsWith("deepseek-"))
        {
            return await _deepSeekService.SendPromptAsync(request.SystemPrompt, request.UserPrompt, request.Temperature, request.MaxTokens);
        }
        else
        {
            return await _ollamaService.GenerateAsync(modelId, request.UserPrompt, request.SystemPrompt, request.Temperature, request.MaxTokens);
        }
    }

    private async Task<AIResponse> ExecuteWithRecommendation(AIRoutingRequest request, AIModelRecommendation recommendation)
    {
        return await ExecuteWithSpecificModel(request, recommendation.ModelId);
    }

    private List<AIModelRecommendation> FilterByCriteria(
        List<AIModelRecommendation> candidates,
        ModelSelectionCriteria criteria)
    {
        var filtered = candidates.AsEnumerable();

        if (criteria.MaxCostPerRequest.HasValue)
        {
            filtered = filtered.Where(c => c.EstimatedCostPer1KTokens <= criteria.MaxCostPerRequest.Value);
        }

        if (criteria.MinContextWindow.HasValue)
        {
            filtered = filtered.Where(c => c.ContextWindow >= criteria.MinContextWindow.Value);
        }

        if (criteria.RequireLocal)
        {
            filtered = filtered.Where(c => c.Details.IsLocal);
        }

        if (criteria.RequireOpenSource)
        {
            filtered = filtered.Where(c => c.Details.IsOpenSource);
        }

        if (criteria.ExcludedProviders?.Any() == true)
        {
            filtered = filtered.Where(c => !criteria.ExcludedProviders.Contains(c.Provider));
        }

        return filtered.ToList();
    }

    private async Task<ProviderHealth> CheckProviderHealth(string provider, Func<Task<bool>> healthCheck)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var isHealthy = await healthCheck();
            var responseTime = DateTime.UtcNow - startTime;

            return new ProviderHealth
            {
                Provider = provider,
                IsHealthy = isHealthy,
                AvailableModels = isHealthy ? 1 : 0,
                ResponseTime = responseTime,
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new ProviderHealth
            {
                Provider = provider,
                IsHealthy = false,
                AvailableModels = 0,
                ErrorMessage = ex.Message,
                ResponseTime = DateTime.UtcNow - startTime,
                LastChecked = DateTime.UtcNow
            };
        }
    }

    private Dictionary<AITaskType, List<AIModelRecommendation>> BuildModelRecommendations()
    {
        // This would be loaded from configuration in production
        return new Dictionary<AITaskType, List<AIModelRecommendation>>
        {
            [AITaskType.CodeGeneration] = new List<AIModelRecommendation>
            {
                CreateRecommendation("deepseek-coder", "DeepSeek Coder", "DeepSeek", "Best cost/performance for code", 0.21m, 64000, new[] { "Excellent at code", "Very affordable", "Fast" }),
                CreateRecommendation("gpt-4o", "GPT-4o", "OpenAI", "Highest quality code generation", 10.0m, 128000, new[] { "Best overall quality", "Multimodal" }),
                CreateRecommendation("codellama:latest", "CodeLlama", "Ollama", "FREE local code generation", 0.0m, 16384, new[] { "FREE", "Private", "Local" })
            },
            [AITaskType.Reasoning] = new List<AIModelRecommendation>
            {
                CreateRecommendation("claude-3-5-sonnet-20241022", "Claude 3.5 Sonnet", "Anthropic", "Best reasoning model", 9.0m, 200000, new[] { "Superior reasoning", "Long context" }),
                CreateRecommendation("gpt-4o", "GPT-4o", "OpenAI", "Excellent reasoning", 10.0m, 128000, new[] { "Strong reasoning", "Multimodal" }),
                CreateRecommendation("deepseek-chat", "DeepSeek Chat", "DeepSeek", "Good reasoning, very affordable", 0.21m, 64000, new[] { "Cost-effective", "Strong reasoning" })
            },
            [AITaskType.ContentGeneration] = new List<AIModelRecommendation>
            {
                CreateRecommendation("deepseek-chat", "DeepSeek Chat", "DeepSeek", "Best value for content", 0.21m, 64000, new[] { "Very affordable", "Good quality" }),
                CreateRecommendation("gpt-4o-mini", "GPT-4o Mini", "OpenAI", "Fast and affordable", 0.375m, 128000, new[] { "Good balance", "Fast" }),
                CreateRecommendation("llama3:latest", "Llama 3", "Ollama", "FREE local generation", 0.0m, 8192, new[] { "FREE", "Private" })
            }
            // Add more task types...
        };
    }

    private AIModelRecommendation CreateRecommendation(
        string modelId,
        string modelName,
        string provider,
        string reason,
        decimal costPer1K,
        int contextWindow,
        string[] strengths)
    {
        return new AIModelRecommendation
        {
            ModelId = modelId,
            ModelName = modelName,
            Provider = provider,
            Reason = reason,
            EstimatedCostPer1KTokens = costPer1K,
            ContextWindow = contextWindow,
            Strengths = strengths.ToList(),
            Details = new AIModelDetails
            {
                Id = modelId,
                Name = modelName,
                Provider = provider,
                ContextWindow = contextWindow,
                InputCostPerMillionTokens = costPer1K * 1000,
                IsOpenSource = provider == "DeepSeek" || provider == "Ollama",
                IsLocal = provider == "Ollama",
                IsAvailable = true
            }
        };
    }

    private AIModelRecommendation GetDeepSeekRecommendation()
    {
        return CreateRecommendation(
            "deepseek-chat",
            "DeepSeek Chat",
            "DeepSeek",
            "Default: best balance of cost and performance",
            0.21m,
            64000,
            new[] { "Cost-effective", "Good quality", "Open-source" }
        );
    }
}
