namespace EventEase.Application.Interfaces;

/// <summary>
/// Anthropic Claude service interface
/// </summary>
public interface IAnthropicService : IAIProviderService
{
    /// <summary>
    /// Analyze text for content safety
    /// </summary>
    Task<ContentSafetyResult> AnalyzeContentSafetyAsync(string text);
}

/// <summary>
/// Content safety analysis result
/// </summary>
public class ContentSafetyResult
{
    public bool IsSafe { get; set; }
    public List<string> Flags { get; set; } = new();
    public double ConfidenceScore { get; set; }
}
