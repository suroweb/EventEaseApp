using System.ComponentModel.DataAnnotations;

namespace EventEase.API.DTOs;

/// <summary>
/// Request to initiate credit package purchase
/// </summary>
public class InitiatePaymentRequest
{
    [Required(ErrorMessage = "Package ID is required")]
    public Guid PackageId { get; set; }

    /// <summary>
    /// Stripe payment method ID (if using saved payment method)
    /// </summary>
    [StringLength(255)]
    public string? PaymentMethodId { get; set; }

    /// <summary>
    /// Whether to save the payment method for future use
    /// </summary>
    public bool SavePaymentMethod { get; set; } = false;
}

/// <summary>
/// Request to confirm a payment
/// </summary>
public class ConfirmPaymentRequest
{
    [Required(ErrorMessage = "Payment intent ID is required")]
    [StringLength(255)]
    public string PaymentIntentId { get; set; } = string.Empty;
}

/// <summary>
/// Request to cancel a payment
/// </summary>
public class CancelPaymentRequest
{
    [Required(ErrorMessage = "Payment intent ID is required")]
    [StringLength(255)]
    public string PaymentIntentId { get; set; } = string.Empty;
}

/// <summary>
/// Request to create a refund
/// </summary>
public class CreateRefundRequest
{
    [Required(ErrorMessage = "Payment intent ID is required")]
    [StringLength(255)]
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// Refund amount (null = full refund)
    /// </summary>
    [Range(0.01, 999999)]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Refund reason
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }
}
