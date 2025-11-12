namespace EventEase.Application.Interfaces;

/// <summary>
/// Stripe payment service for credit purchases
/// </summary>
public interface IStripePaymentService
{
    /// <summary>
    /// Create payment intent for credit package purchase
    /// </summary>
    Task<PaymentIntentResult> CreatePaymentIntentAsync(
        Guid tenantId,
        Guid packageId,
        string? paymentMethodId = null,
        bool savePaymentMethod = false);

    /// <summary>
    /// Confirm payment intent
    /// </summary>
    Task<PaymentIntentResult> ConfirmPaymentIntentAsync(string paymentIntentId);

    /// <summary>
    /// Cancel payment intent
    /// </summary>
    Task<bool> CancelPaymentIntentAsync(string paymentIntentId);

    /// <summary>
    /// Create refund for a payment
    /// </summary>
    Task<RefundResult> CreateRefundAsync(
        string paymentIntentId,
        decimal? amount = null,
        string? reason = null);

    /// <summary>
    /// Get payment intent details
    /// </summary>
    Task<PaymentIntentResult> GetPaymentIntentAsync(string paymentIntentId);

    /// <summary>
    /// Attach payment method to customer
    /// </summary>
    Task<bool> AttachPaymentMethodAsync(string customerId, string paymentMethodId);

    /// <summary>
    /// List customer payment methods
    /// </summary>
    Task<List<PaymentMethodInfo>> GetCustomerPaymentMethodsAsync(string customerId);

    /// <summary>
    /// Create or get Stripe customer for tenant
    /// </summary>
    Task<string> GetOrCreateCustomerAsync(Guid tenantId);
}

/// <summary>
/// Payment intent result
/// </summary>
public class PaymentIntentResult
{
    public bool Success { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? ClientSecret { get; set; }
    public string Status { get; set; } = string.Empty; // requires_payment_method, requires_confirmation, succeeded, canceled
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "eur";
    public string? CustomerId { get; set; }
    public string? PaymentMethodId { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Refund result
/// </summary>
public class RefundResult
{
    public bool Success { get; set; }
    public string? RefundId { get; set; }
    public string Status { get; set; } = string.Empty; // pending, succeeded, failed, canceled
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "eur";
    public string? Reason { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Payment method info
/// </summary>
public class PaymentMethodInfo
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // card, sepa_debit, etc.
    public string? Brand { get; set; } // visa, mastercard, etc.
    public string? Last4 { get; set; }
    public int? ExpMonth { get; set; }
    public int? ExpYear { get; set; }
    public bool IsDefault { get; set; }
}
