using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EventEase.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Ollama local AI service connectivity
/// </summary>
public class OllamaHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OllamaHealthCheck> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public OllamaHealthCheck(
        IConfiguration configuration,
        ILogger<OllamaHealthCheck> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUrl = _configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";

            // Test Ollama connectivity
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(3);

            var response = await httpClient.GetAsync(
                $"{baseUrl}/api/tags",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = new Dictionary<string, object>
                {
                    { "status", "connected" },
                    { "provider", "Ollama (Local)" },
                    { "baseUrl", baseUrl }
                };

                return HealthCheckResult.Healthy(
                    "Ollama local AI service is accessible",
                    data);
            }
            else
            {
                return HealthCheckResult.Degraded(
                    $"Ollama service returned status {response.StatusCode}");
            }
        }
        catch (HttpRequestException)
        {
            // Ollama is optional (local service), so degraded instead of unhealthy
            return HealthCheckResult.Degraded(
                "Ollama local AI service not available (optional)");
        }
        catch (TaskCanceledException)
        {
            return HealthCheckResult.Degraded(
                "Ollama service timeout (may not be running)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama health check failed");
            return HealthCheckResult.Degraded(
                "Ollama connectivity check failed (optional service)",
                ex);
        }
    }
}
