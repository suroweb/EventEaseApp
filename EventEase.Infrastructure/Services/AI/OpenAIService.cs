using EventEase.Application.Interfaces;
using EventEase.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventEase.Infrastructure.Services.AI;

/// <summary>
/// OpenAI GPT service implementation
/// </summary>
public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIService> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public AIProvider Provider => AIProvider.OpenAI;

    public OpenAIService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAIService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not configured");
        _model = configuration["OpenAI:Model"] ?? "gpt-4";

        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<AIResponse> SendPromptAsync(
        string systemPrompt,
        string userPrompt,
        double temperature = 0.7,
        int maxTokens = 2000)
    {
        try
        {
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature,
                max_tokens = maxTokens
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI API error: {StatusCode} - {Error}", response.StatusCode, errorContent);

                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = $"OpenAI API error: {response.StatusCode}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAICompletionResponse>();

            if (result?.Choices == null || result.Choices.Length == 0)
            {
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = "No response from OpenAI"
                };
            }

            return new AIResponse
            {
                Success = true,
                Content = result.Choices[0].Message.Content,
                InputTokens = result.Usage?.PromptTokens ?? 0,
                OutputTokens = result.Usage?.CompletionTokens ?? 0,
                Model = result.Model
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API");
            return new AIResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<AIResponse<T>> SendPromptJsonAsync<T>(
        string systemPrompt,
        string userPrompt,
        double temperature = 0.7,
        int maxTokens = 2000) where T : class
    {
        try
        {
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt + "\n\nYou must respond with valid JSON only." },
                    new { role = "user", content = userPrompt }
                },
                temperature,
                max_tokens = maxTokens,
                response_format = new { type = "json_object" }
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI API error: {StatusCode} - {Error}", response.StatusCode, errorContent);

                return new AIResponse<T>
                {
                    Success = false,
                    ErrorMessage = $"OpenAI API error: {response.StatusCode}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAICompletionResponse>();

            if (result?.Choices == null || result.Choices.Length == 0)
            {
                return new AIResponse<T>
                {
                    Success = false,
                    ErrorMessage = "No response from OpenAI"
                };
            }

            var content = result.Choices[0].Message.Content;
            var data = JsonSerializer.Deserialize<T>(content);

            return new AIResponse<T>
            {
                Success = true,
                Content = content,
                Data = data,
                InputTokens = result.Usage?.PromptTokens ?? 0,
                OutputTokens = result.Usage?.CompletionTokens ?? 0,
                Model = result.Model
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API with JSON mode");
            return new AIResponse<T>
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<float[]> GenerateEmbeddingsAsync(string text)
    {
        try
        {
            var requestBody = new
            {
                model = "text-embedding-ada-002",
                input = text
            };

            var response = await _httpClient.PostAsJsonAsync("embeddings", requestBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI embeddings error: {StatusCode}", response.StatusCode);
                return Array.Empty<float>();
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>();

            return result?.Data?[0].Embedding ?? Array.Empty<float>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embeddings");
            return Array.Empty<float>();
        }
    }

    public async Task<SentimentAnalysisResult> AnalyzeSentimentAsync(string text)
    {
        var systemPrompt = @"You are a sentiment analysis expert. Analyze the sentiment of the provided text and respond with a JSON object containing:
- sentiment: 'Positive', 'Negative', or 'Neutral'
- score: a number from -1.0 (very negative) to 1.0 (very positive)
- emotions: an object with emotion names as keys and confidence scores as values";

        var userPrompt = $"Analyze the sentiment of this text:\n\n{text}";

        var response = await SendPromptJsonAsync<SentimentAnalysisResult>(systemPrompt, userPrompt, 0.3, 500);

        return response.Data ?? new SentimentAnalysisResult
        {
            Sentiment = "Neutral",
            Score = 0.0
        };
    }

    #region OpenAI Response Models

    private class OpenAICompletionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("choices")]
        public OpenAIChoice[] Choices { get; set; } = Array.Empty<OpenAIChoice>();

        [JsonPropertyName("usage")]
        public OpenAIUsage? Usage { get; set; }
    }

    private class OpenAIChoice
    {
        [JsonPropertyName("message")]
        public OpenAIMessage Message { get; set; } = new();

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; } = string.Empty;
    }

    private class OpenAIMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class OpenAIUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    private class OpenAIEmbeddingResponse
    {
        [JsonPropertyName("data")]
        public OpenAIEmbeddingData[]? Data { get; set; }
    }

    private class OpenAIEmbeddingData
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }

    #endregion
}
