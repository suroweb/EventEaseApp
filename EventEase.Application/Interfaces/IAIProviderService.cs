using EventEase.Domain.Enums;

namespace EventEase.Application.Interfaces;

/// <summary>
/// Base interface for AI provider services (OpenAI, Anthropic)
/// </summary>
public interface IAIProviderService
{
    /// <summary>
    /// Get the provider type
    /// </summary>
    AIProvider Provider { get; }

    /// <summary>
    /// Send a prompt to the AI model and get a response
    /// </summary>
    /// <param name="systemPrompt">System instructions for the AI</param>
    /// <param name="userPrompt">User prompt/query</param>
    /// <param name="temperature">Temperature for response randomness (0.0-1.0)</param>
    /// <param name="maxTokens">Maximum tokens in response</param>
    /// <returns>AI response with token usage</returns>
    Task<AIResponse> SendPromptAsync(
        string systemPrompt,
        string userPrompt,
        double temperature = 0.7,
        int maxTokens = 2000);

    /// <summary>
    /// Send a prompt with JSON response mode
    /// </summary>
    Task<AIResponse<T>> SendPromptJsonAsync<T>(
        string systemPrompt,
        string userPrompt,
        double temperature = 0.7,
        int maxTokens = 2000) where T : class;
}

/// <summary>
/// AI response model
/// </summary>
public class AIResponse
{
    public string Content { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens => InputTokens + OutputTokens;
    public string Model { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// AI response with typed JSON content
/// </summary>
public class AIResponse<T> : AIResponse where T : class
{
    public T? Data { get; set; }
}
