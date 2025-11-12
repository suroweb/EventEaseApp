using EventEase.Application.Interfaces;
using EventEase.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventEase.Infrastructure.Services.AI;

/// <summary>
/// Anthropic Claude service implementation
/// </summary>
public class AnthropicService : IAnthropicService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AnthropicService> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public AIProvider Provider => AIProvider.Anthropic;

    public AnthropicService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AnthropicService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _apiKey = configuration["Anthropic:ApiKey"] ?? throw new InvalidOperationException("Anthropic API key not configured");
        _model = configuration["Anthropic:Model"] ?? "claude-3-5-sonnet-20241022";

        _httpClient.BaseAddress = new Uri("https://api.anthropic.com/v1/");
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
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
                max_tokens = maxTokens,
                temperature,
                system = systemPrompt,
                messages = new[]
                {
                    new { role = "user", content = userPrompt }
                }
            };

            var response = await _httpClient.PostAsJsonAsync("messages", requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Anthropic API error: {StatusCode} - {Error}", response.StatusCode, errorContent);

                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = $"Anthropic API error: {response.StatusCode}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<AnthropicMessageResponse>();

            if (result?.Content == null || result.Content.Length == 0)
            {
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = "No response from Anthropic"
                };
            }

            return new AIResponse
            {
                Success = true,
                Content = result.Content[0].Text,
                InputTokens = result.Usage?.InputTokens ?? 0,
                OutputTokens = result.Usage?.OutputTokens ?? 0,
                Model = result.Model
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Anthropic API");
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
            var enhancedSystemPrompt = systemPrompt + "\n\nYou must respond with valid JSON only. Do not include any markdown formatting or code blocks.";

            var requestBody = new
            {
                model = _model,
                max_tokens = maxTokens,
                temperature,
                system = enhancedSystemPrompt,
                messages = new[]
                {
                    new { role = "user", content = userPrompt }
                }
            };

            var response = await _httpClient.PostAsJsonAsync("messages", requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Anthropic API error: {StatusCode} - {Error}", response.StatusCode, errorContent);

                return new AIResponse<T>
                {
                    Success = false,
                    ErrorMessage = $"Anthropic API error: {response.StatusCode}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<AnthropicMessageResponse>();

            if (result?.Content == null || result.Content.Length == 0)
            {
                return new AIResponse<T>
                {
                    Success = false,
                    ErrorMessage = "No response from Anthropic"
                };
            }

            var content = result.Content[0].Text;

            // Clean up potential markdown code blocks
            content = content.Trim();
            if (content.StartsWith("```json"))
            {
                content = content.Substring(7);
            }
            if (content.StartsWith("```"))
            {
                content = content.Substring(3);
            }
            if (content.EndsWith("```"))
            {
                content = content.Substring(0, content.Length - 3);
            }
            content = content.Trim();

            var data = JsonSerializer.Deserialize<T>(content);

            return new AIResponse<T>
            {
                Success = true,
                Content = content,
                Data = data,
                InputTokens = result.Usage?.InputTokens ?? 0,
                OutputTokens = result.Usage?.OutputTokens ?? 0,
                Model = result.Model
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Anthropic API with JSON mode");
            return new AIResponse<T>
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ContentSafetyResult> AnalyzeContentSafetyAsync(string text)
    {
        var systemPrompt = @"You are a content safety analyzer. Analyze the provided text for potentially harmful, offensive, or inappropriate content. Respond with a JSON object containing:
- isSafe: boolean (true if content is safe)
- flags: array of strings (list any concerns like 'violence', 'hate-speech', 'explicit', etc.)
- confidenceScore: number from 0.0 to 1.0";

        var userPrompt = $"Analyze this content for safety:\n\n{text}";

        var response = await SendPromptJsonAsync<ContentSafetyResult>(systemPrompt, userPrompt, 0.3, 500);

        return response.Data ?? new ContentSafetyResult
        {
            IsSafe = true,
            ConfidenceScore = 0.5
        };
    }

    #region Anthropic Response Models

    private class AnthropicMessageResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public AnthropicContent[] Content { get; set; } = Array.Empty<AnthropicContent>();

        [JsonPropertyName("usage")]
        public AnthropicUsage? Usage { get; set; }

        [JsonPropertyName("stop_reason")]
        public string? StopReason { get; set; }
    }

    private class AnthropicContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private class AnthropicUsage
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
    }

    #endregion
}
