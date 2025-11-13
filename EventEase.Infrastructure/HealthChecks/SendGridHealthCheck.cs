using Microsoft.Extensions.Diagnostics.HealthChecks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace EventEase.Infrastructure.HealthChecks;

/// <summary>
/// Health check for SendGrid email service connectivity
/// </summary>
public class SendGridHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridHealthCheck> _logger;

    public SendGridHealthCheck(
        IConfiguration configuration,
        ILogger<SendGridHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = _configuration["SendGrid:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return HealthCheckResult.Unhealthy(
                    "SendGrid API key not configured");
            }

            // Test SendGrid connectivity by validating API key
            var client = new SendGridClient(apiKey);

            // Use GET /v3/user/email endpoint to verify API key
            var response = await client.RequestAsync(
                method: SendGrid.Helpers.BaseClient.SendGridClient.Method.GET,
                urlPath: "user/email",
                cancellationToken: cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = new Dictionary<string, object>
                {
                    { "status", "connected" },
                    { "statusCode", (int)response.StatusCode }
                };

                return HealthCheckResult.Healthy(
                    "SendGrid API is accessible",
                    data);
            }
            else
            {
                return HealthCheckResult.Degraded(
                    $"SendGrid API returned status {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendGrid health check failed");
            return HealthCheckResult.Unhealthy(
                "SendGrid connectivity check failed",
                ex);
        }
    }
}
