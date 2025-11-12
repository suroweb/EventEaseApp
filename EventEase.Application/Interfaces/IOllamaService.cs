namespace EventEase.Application.Interfaces;

/// <summary>
/// Ollama service for running open-source models locally
/// Supports: Llama 3, Mistral, Mixtral, CodeLlama, Phi, Gemma, and 100+ models
/// </summary>
public interface IOllamaService : IAIProviderService
{
    /// <summary>
    /// Generate completion using specified Ollama model
    /// </summary>
    Task<AIResponse> GenerateAsync(
        string model,
        string prompt,
        string? systemPrompt = null,
        double temperature = 0.7,
        int maxTokens = 2000);

    /// <summary>
    /// Generate streaming completion
    /// </summary>
    IAsyncEnumerable<string> GenerateStreamAsync(
        string model,
        string prompt,
        string? systemPrompt = null,
        double temperature = 0.7);

    /// <summary>
    /// Generate embeddings for similarity search
    /// </summary>
    Task<float[]> GenerateEmbeddingsAsync(
        string model,
        string text);

    /// <summary>
    /// List all available models in local Ollama instance
    /// </summary>
    Task<List<OllamaModel>> ListModelsAsync();

    /// <summary>
    /// Pull a model from Ollama registry
    /// </summary>
    Task<bool> PullModelAsync(
        string modelName,
        IProgress<ModelPullProgress>? progress = null);

    /// <summary>
    /// Delete a model from local system
    /// </summary>
    Task<bool> DeleteModelAsync(string modelName);

    /// <summary>
    /// Check if Ollama is running and accessible
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Get model information
    /// </summary>
    Task<OllamaModelInfo?> GetModelInfoAsync(string modelName);
}

public class OllamaModel
{
    public string Name { get; set; } = null!;
    public string Size { get; set; } = null!;
    public DateTime ModifiedAt { get; set; }
    public string Digest { get; set; } = null!;
}

public class OllamaModelInfo
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public long Size { get; set; }
    public string Family { get; set; } = null!;
    public List<string> Capabilities { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class ModelPullProgress
{
    public string Status { get; set; } = null!;
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public double PercentComplete => TotalBytes > 0 ? (double)BytesDownloaded / TotalBytes * 100 : 0;
}
