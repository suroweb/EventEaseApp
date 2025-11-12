namespace EventEase.Application.Interfaces;

/// <summary>
/// OpenAI GPT service interface
/// </summary>
public interface IOpenAIService : IAIProviderService
{
    /// <summary>
    /// Generate embeddings for text (for similarity search)
    /// </summary>
    Task<float[]> GenerateEmbeddingsAsync(string text);

    /// <summary>
    /// Analyze sentiment of text
    /// </summary>
    Task<SentimentAnalysisResult> AnalyzeSentimentAsync(string text);
}

/// <summary>
/// Sentiment analysis result
/// </summary>
public class SentimentAnalysisResult
{
    public string Sentiment { get; set; } = "Neutral"; // Positive, Negative, Neutral
    public double Score { get; set; } // -1.0 to 1.0
    public Dictionary<string, double> Emotions { get; set; } = new();
}
