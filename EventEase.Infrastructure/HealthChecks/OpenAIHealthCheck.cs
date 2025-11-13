using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EventEase.Infrastructure.HealthChecks;

/// <summary>
/// Health check for OpenAI API connectivity
/// </summary>
public class OpenAIHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIHealthCheck> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public OpenAIHealthCheck(
        IConfiguration configuration,
        ILogger<OpenAIHealthCheck> logger,
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
            var apiKey = _configuration["OpenAI:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                // OpenAI is optional, so degraded instead of unhealthy
                return HealthCheckResult.Degraded(
                    "OpenAI API key not configured (optional service)");
            }

            // Test OpenAI connectivity by listing models
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            var response = await httpClient.GetAsync(
                "https://api.openai.com/v1/models",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = new Dictionary<string, object>
                {
                    { "status", "connected" },
                    { "provider", "OpenAI" }
                };

                return HealthCheckResult.Healthy(
                    "OpenAI API is accessible",
                    data);
            }
            else
            {
                return HealthCheckResult.Degraded(
                    $"OpenAI API returned status {response.StatusCode}");
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("OpenAI health check timed out");
            return HealthCheckResult.Degraded(
                "OpenAI API timeout (may be experiencing high load)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI health check failed");
            return HealthCheckResult.Degraded(
                "OpenAI connectivity check failed (optional service)",
                ex);
        }
    }
}
