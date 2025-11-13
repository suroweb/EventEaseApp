using Microsoft.Extensions.Diagnostics.HealthChecks;
using Stripe;

namespace EventEase.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Stripe payment service connectivity
/// </summary>
public class StripeHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeHealthCheck> _logger;

    public StripeHealthCheck(
        IConfiguration configuration,
        ILogger<StripeHealthCheck> logger)
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
            var apiKey = _configuration["Stripe:SecretKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return HealthCheckResult.Unhealthy(
                    "Stripe API key not configured");
            }

            // Test Stripe connectivity by fetching balance
            StripeConfiguration.ApiKey = apiKey;
            var balanceService = new BalanceService();
            var balance = await balanceService.GetAsync(cancellationToken: cancellationToken);

            var data = new Dictionary<string, object>
            {
                { "currency", balance.Available.FirstOrDefault()?.Currency ?? "unknown" },
                { "status", "connected" }
            };

            return HealthCheckResult.Healthy(
                "Stripe API is accessible",
                data);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe health check failed");
            return HealthCheckResult.Unhealthy(
                $"Stripe API error: {ex.Message}",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe health check failed unexpectedly");
            return HealthCheckResult.Unhealthy(
                "Stripe connectivity check failed",
                ex);
        }
    }
}
