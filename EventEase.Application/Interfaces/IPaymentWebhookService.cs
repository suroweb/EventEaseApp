namespace EventEase.Application.Interfaces;

/// <summary>
/// Service for handling Stripe webhook events
/// </summary>
public interface IPaymentWebhookService
{
    /// <summary>
    /// Verify and process Stripe webhook event
    /// </summary>
    Task<WebhookProcessingResult> ProcessWebhookAsync(string payload, string signature);

    /// <summary>
    /// Handle payment succeeded event
    /// </summary>
    Task HandlePaymentSucceededAsync(string paymentIntentId);

    /// <summary>
    /// Handle payment failed event
    /// </summary>
    Task HandlePaymentFailedAsync(string paymentIntentId, string? errorMessage);

    /// <summary>
    /// Handle refund completed event
    /// </summary>
    Task HandleRefundCompletedAsync(string refundId);
}

/// <summary>
/// Webhook processing result
/// </summary>
public class WebhookProcessingResult
{
    public bool Success { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public bool Processed { get; set; } // Whether the event was actually processed (vs ignored)
}
