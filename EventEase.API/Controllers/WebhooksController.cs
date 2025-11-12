using EventEase.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventEase.API.Controllers;

/// <summary>
/// Webhook endpoints for Stripe events
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Webhooks don't use JWT auth, they use Stripe signature verification
[Produces("application/json")]
public class WebhooksController : ControllerBase
{
    private readonly IPaymentWebhookService _webhookService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IPaymentWebhookService webhookService,
        ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Stripe webhook endpoint for payment events
    /// </summary>
    /// <response code="200">Webhook processed</response>
    /// <response code="400">Invalid webhook</response>
    [HttpPost("stripe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StripeWebhook()
    {
        try
        {
            // Read the raw request body
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();

            // Get Stripe signature from headers
            var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Webhook received without Stripe-Signature header");
                return BadRequest(new { Error = "Missing Stripe-Signature header" });
            }

            _logger.LogInformation("Processing Stripe webhook with signature: {Signature}", signature);

            // Process the webhook
            var result = await _webhookService.ProcessWebhookAsync(payload, signature);

            if (!result.Success)
            {
                _logger.LogError("Webhook processing failed: {Error}", result.ErrorMessage);
                return BadRequest(new { Error = result.ErrorMessage });
            }

            _logger.LogInformation("Webhook processed successfully: {EventType} - {EventId}",
                result.EventType, result.EventId);

            return Ok(new
            {
                Success = true,
                EventType = result.EventType,
                EventId = result.EventId,
                Processed = result.Processed
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing webhook");
            return StatusCode(500, new { Error = "Webhook processing failed" });
        }
    }

    /// <summary>
    /// Health check endpoint for webhook verification
    /// </summary>
    /// <response code="200">Webhook endpoint is healthy</response>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult WebhookHealth()
    {
        return Ok(new
        {
            Status = "Healthy",
            Endpoint = "/api/webhooks/stripe",
            Message = "Webhook endpoint is ready to receive Stripe events"
        });
    }
}
