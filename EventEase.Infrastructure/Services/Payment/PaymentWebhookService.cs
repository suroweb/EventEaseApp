using EventEase.Application.Interfaces;
using EventEase.Domain.Entities;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventEase.Infrastructure.Services.Payment;

/// <summary>
/// Stripe webhook event handler service
/// </summary>
public class PaymentWebhookService : IPaymentWebhookService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentWebhookService> _logger;
    private readonly string _webhookSecret;

    public PaymentWebhookService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<PaymentWebhookService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;

        _webhookSecret = configuration["Stripe:WebhookSecret"]
            ?? throw new InvalidOperationException("Stripe Webhook Secret not configured");
    }

    public async Task<WebhookProcessingResult> ProcessWebhookAsync(string payload, string signature)
    {
        try
        {
            // NOTE: Actual Stripe SDK webhook signature verification:
            /*
            var stripeEvent = Stripe.EventUtility.ConstructEvent(
                payload,
                signature,
                _webhookSecret
            );

            _logger.LogInformation("Received Stripe webhook: {EventType} - {EventId}",
                stripeEvent.Type, stripeEvent.Id);

            // Process based on event type
            switch (stripeEvent.Type)
            {
                case "payment_intent.succeeded":
                    var paymentIntent = stripeEvent.Data.Object as Stripe.PaymentIntent;
                    await HandlePaymentSucceededAsync(paymentIntent.Id);
                    return new WebhookProcessingResult
                    {
                        Success = true,
                        EventType = stripeEvent.Type,
                        EventId = stripeEvent.Id,
                        Processed = true
                    };

                case "payment_intent.payment_failed":
                    var failedPaymentIntent = stripeEvent.Data.Object as Stripe.PaymentIntent;
                    var errorMessage = failedPaymentIntent.LastPaymentError?.Message;
                    await HandlePaymentFailedAsync(failedPaymentIntent.Id, errorMessage);
                    return new WebhookProcessingResult
                    {
                        Success = true,
                        EventType = stripeEvent.Type,
                        EventId = stripeEvent.Id,
                        Processed = true
                    };

                case "charge.refunded":
                    var charge = stripeEvent.Data.Object as Stripe.Charge;
                    var refund = charge.Refunds?.Data?.FirstOrDefault();
                    if (refund != null)
                    {
                        await HandleRefundCompletedAsync(refund.Id);
                    }
                    return new WebhookProcessingResult
                    {
                        Success = true,
                        EventType = stripeEvent.Type,
                        EventId = stripeEvent.Id,
                        Processed = true
                    };

                default:
                    _logger.LogInformation("Unhandled webhook event type: {EventType}", stripeEvent.Type);
                    return new WebhookProcessingResult
                    {
                        Success = true,
                        EventType = stripeEvent.Type,
                        EventId = stripeEvent.Id,
                        Processed = false
                    };
            }
            */

            // Simulated webhook processing
            _logger.LogInformation("Processing webhook with signature: {Signature}", signature);

            // For simulation, assume it's a payment succeeded event
            var simulatedEventId = $"evt_sim_{Guid.NewGuid():N}";

            return new WebhookProcessingResult
            {
                Success = true,
                EventType = "payment_intent.succeeded",
                EventId = simulatedEventId,
                Processed = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");

            return new WebhookProcessingResult
            {
                Success = false,
                ErrorMessage = $"Webhook processing failed: {ex.Message}"
            };
        }
    }

    public async Task HandlePaymentSucceededAsync(string paymentIntentId)
    {
        try
        {
            _logger.LogInformation("Handling payment succeeded for: {PaymentIntentId}", paymentIntentId);

            // Find the purchase record
            var purchase = await _context.CreditPurchases
                .Include(p => p.Package)
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);

            if (purchase == null)
            {
                _logger.LogWarning("Purchase not found for payment intent: {PaymentIntentId}", paymentIntentId);
                return;
            }

            // Check if already processed
            if (purchase.PaymentStatus == PaymentStatus.Completed)
            {
                _logger.LogInformation("Payment already processed for: {PaymentIntentId}", paymentIntentId);
                return;
            }

            // Update purchase status
            purchase.PaymentStatus = PaymentStatus.Completed;
            purchase.PaidAt = DateTime.UtcNow;

            // Grant credits to tenant
            var tenant = await _context.Tenants.FindAsync(purchase.TenantId);
            if (tenant != null)
            {
                tenant.AvailableCredits += purchase.CreditsGranted;

                // Update tenant status if trial
                if (tenant.Status == TenantStatus.Trial)
                {
                    tenant.Status = TenantStatus.Active;
                }

                // Create credit transaction record
                var transaction = new CreditTransaction
                {
                    TenantId = purchase.TenantId,
                    Type = CreditTransactionType.Purchase,
                    Amount = purchase.CreditsGranted,
                    BalanceAfter = tenant.AvailableCredits,
                    Description = $"Credit purchase: {purchase.Package?.Name ?? "Unknown"}",
                    PurchaseId = purchase.Id,
                    UserId = purchase.CreatedByUserId
                };

                _context.CreditTransactions.Add(transaction);

                _logger.LogInformation("Granted {Credits} credits to tenant {TenantId}, new balance: {Balance}",
                    purchase.CreditsGranted, purchase.TenantId, tenant.AvailableCredits);
            }

            await _context.SaveChangesAsync();

            // TODO: Phase 1.6 - Send invoice email via SendGrid
            _logger.LogInformation("Payment processing completed for: {PaymentIntentId}", paymentIntentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment succeeded for: {PaymentIntentId}", paymentIntentId);
            throw;
        }
    }

    public async Task HandlePaymentFailedAsync(string paymentIntentId, string? errorMessage)
    {
        try
        {
            _logger.LogWarning("Handling payment failed for: {PaymentIntentId}, Error: {Error}",
                paymentIntentId, errorMessage);

            // Find the purchase record
            var purchase = await _context.CreditPurchases
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);

            if (purchase == null)
            {
                _logger.LogWarning("Purchase not found for payment intent: {PaymentIntentId}", paymentIntentId);
                return;
            }

            // Update purchase status
            purchase.PaymentStatus = PaymentStatus.Failed;

            await _context.SaveChangesAsync();

            // TODO: Phase 1.6 - Send payment failed notification email
            _logger.LogInformation("Payment failure recorded for: {PaymentIntentId}", paymentIntentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment failed for: {PaymentIntentId}", paymentIntentId);
            throw;
        }
    }

    public async Task HandleRefundCompletedAsync(string refundId)
    {
        try
        {
            _logger.LogInformation("Handling refund completed: {RefundId}", refundId);

            // NOTE: In full implementation, would need to:
            // 1. Find purchase by refund ID
            // 2. Deduct credits from tenant if they were used
            // 3. Update purchase status to Refunded
            // 4. Create credit transaction record with negative amount

            // For now, just log
            _logger.LogInformation("Refund processing completed for: {RefundId}", refundId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling refund completed for: {RefundId}", refundId);
            throw;
        }
    }
}
