namespace EventEase.Application.Interfaces;

/// <summary>
/// DeepSeek AI service - Open-source models via API
/// Models: deepseek-chat, deepseek-coder, deepseek-reasoner
/// </summary>
public interface IDeepSeekService : IAIProviderService
{
    /// <summary>
    /// Generate completion using DeepSeek Chat model
    /// </summary>
    Task<AIResponse> GenerateChatCompletionAsync(
        string systemPrompt,
        string userPrompt,
        double temperature = 0.7,
        int maxTokens = 2000);

    /// <summary>
    /// Generate code using DeepSeek Coder model (optimized for coding)
    /// </summary>
    Task<AIResponse> GenerateCodeAsync(
        string instruction,
        string? context = null,
        string? language = null);

    /// <summary>
    /// Use DeepSeek Reasoner for complex reasoning tasks
    /// </summary>
    Task<AIResponse> ReasonAsync(
        string problem,
        string? context = null);

    /// <summary>
    /// Fill-in-the-middle code completion
    /// </summary>
    Task<AIResponse> FillInTheMiddleAsync(
        string prefix,
        string suffix,
        string? language = null);
}
