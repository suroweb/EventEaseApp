namespace EventEase.Domain.Enums;

/// <summary>
/// AI providers supported by the system
/// </summary>
public enum AIProvider
{
    /// <summary>
    /// OpenAI GPT-4 and GPT-4o
    /// </summary>
    OpenAI = 0,

    /// <summary>
    /// Anthropic Claude 3.5 Sonnet
    /// </summary>
    Anthropic = 1,

    /// <summary>
    /// DeepSeek - Open-source AI with strong reasoning (API-based)
    /// </summary>
    DeepSeek = 2,

    /// <summary>
    /// Ollama - Local open-source models (Llama, Mistral, etc.)
    /// </summary>
    Ollama = 3
}
