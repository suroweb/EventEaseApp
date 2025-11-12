using EventEase.Application.Interfaces;
using EventEase.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace EventEase.Infrastructure.Services.AI;

/// <summary>
/// DeepSeek AI service implementation
/// DeepSeek is a powerful open-source AI with strong reasoning and coding capabilities
/// Much cheaper than GPT-4 with comparable performance
/// </summary>
public class DeepSeekService : IDeepSeekService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DeepSeekService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public AIProvider Provider => AIProvider.DeepSeek;

    public DeepSeekService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<DeepSeekService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _apiKey = configuration["DeepSeek:ApiKey"] ?? throw new InvalidOperationException("DeepSeek API key not configured");
        _baseUrl = configuration["DeepSeek:BaseUrl"] ?? "https://api.deepseek.com";

        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<AIResponse> SendPromptAsync(
        string systemPrompt,
        string userPrompt,
        double temperature = 0.7,
        int maxTokens = 2000)
    {
        return await GenerateChatCompletionAsync(systemPrompt, userPrompt, temperature, maxTokens);
    }

    public async Task<AIResponse<T>> SendPromptJsonAsync<T>(
        string systemPrompt,
        string userPrompt,
        double temperature = 0.7,
        int maxTokens = 2000) where T : class
    {
        var response = await SendPromptAsync(systemPrompt, userPrompt, temperature, maxTokens);

        if (!response.Success)
        {
            return new AIResponse<T>
            {
                Success = false,
                ErrorMessage = response.ErrorMessage,
                Content = response.Content,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens,
                Model = response.Model
            };
        }

        try
        {
            var data = JsonSerializer.Deserialize<T>(response.Content);
            return new AIResponse<T>
            {
                Success = true,
                Content = response.Content,
                Data = data,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens,
                Model = response.Model
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing DeepSeek JSON response");
            return new AIResponse<T>
            {
                Success = false,
                ErrorMessage = $"Failed to parse JSON: {ex.Message}",
                Content = response.Content,
                InputTokens = response.InputTokens,
                OutputTokens = response.OutputTokens,
                Model = response.Model
            };
        }
    }

    public async Task<AIResponse> GenerateChatCompletionAsync(
        string systemPrompt,
        string userPrompt,
        double temperature = 0.7,
        int maxTokens = 2000)
    {
        try
        {
            var model = _configuration["DeepSeek:Model"] ?? "deepseek-chat";

            var requestBody = new
            {
                model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature,
                max_tokens = maxTokens,
                top_p = 1.0,
                frequency_penalty = 0.0,
                presence_penalty = 0.0,
                stream = false
            };

            var response = await _httpClient.PostAsJsonAsync("/v1/chat/completions", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<DeepSeekResponse>();

            if (result?.Choices?.FirstOrDefault()?.Message?.Content != null)
            {
                return new AIResponse
                {
                    Success = true,
                    Content = result.Choices[0].Message.Content,
                    InputTokens = result.Usage?.PromptTokens ?? 0,
                    OutputTokens = result.Usage?.CompletionTokens ?? 0,
                    Model = result.Model ?? model
                };
            }

            return new AIResponse
            {
                Success = false,
                ErrorMessage = "No content in DeepSeek response",
                Model = model
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling DeepSeek API");
            return new AIResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                Model = "deepseek-chat"
            };
        }
    }

    public async Task<AIResponse> GenerateCodeAsync(
        string instruction,
        string? context = null,
        string? language = null)
    {
        var systemPrompt = "You are an expert programmer. Generate clean, efficient, well-documented code.";
        if (!string.IsNullOrEmpty(language))
        {
            systemPrompt += $" Use {language}.";
        }

        var userPrompt = instruction;
        if (!string.IsNullOrEmpty(context))
        {
            userPrompt = $"Context:\n{context}\n\nInstruction:\n{instruction}";
        }

        // Use deepseek-coder for better code generation
        var originalModel = _configuration["DeepSeek:Model"];
        _configuration["DeepSeek:Model"] = "deepseek-coder";

        var response = await GenerateChatCompletionAsync(systemPrompt, userPrompt, 0.3, 4000);

        // Restore original model
        if (originalModel != null)
        {
            _configuration["DeepSeek:Model"] = originalModel;
        }

        return response;
    }

    public async Task<AIResponse> ReasonAsync(
        string problem,
        string? context = null)
    {
        var systemPrompt = @"You are a highly capable reasoning AI. Break down complex problems step-by-step.
Provide clear explanations of your reasoning process.";

        var userPrompt = problem;
        if (!string.IsNullOrEmpty(context))
        {
            userPrompt = $"Context:\n{context}\n\nProblem:\n{problem}";
        }

        // Use lower temperature for more focused reasoning
        return await GenerateChatCompletionAsync(systemPrompt, userPrompt, 0.5, 3000);
    }

    public async Task<AIResponse> FillInTheMiddleAsync(
        string prefix,
        string suffix,
        string? language = null)
    {
        var systemPrompt = "You are a code completion expert. Fill in the missing code between the prefix and suffix.";
        if (!string.IsNullOrEmpty(language))
        {
            systemPrompt += $" Language: {language}";
        }

        var userPrompt = $@"Prefix:
```
{prefix}
```

Suffix:
```
{suffix}
```

Fill in the code between prefix and suffix. Provide ONLY the middle code, no explanations.";

        return await GenerateChatCompletionAsync(systemPrompt, userPrompt, 0.2, 2000);
    }

    // DeepSeek API response models
    private class DeepSeekResponse
    {
        public string? Id { get; set; }
        public string? Model { get; set; }
        public List<DeepSeekChoice>? Choices { get; set; }
        public DeepSeekUsage? Usage { get; set; }
    }

    private class DeepSeekChoice
    {
        public int Index { get; set; }
        public DeepSeekMessage? Message { get; set; }
        public string? FinishReason { get; set; }
    }

    private class DeepSeekMessage
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    private class DeepSeekUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
