using System.ComponentModel.DataAnnotations;

namespace EventEase.API.DTOs;

/// <summary>
/// Request model for purchasing a credit package
/// </summary>
public class PurchaseCreditPackageRequest
{
    [Required(ErrorMessage = "Package ID is required")]
    public Guid PackageId { get; set; }

    /// <summary>
    /// Payment method ID from Stripe (for card payments)
    /// </summary>
    [StringLength(255)]
    public string? PaymentMethodId { get; set; }

    /// <summary>
    /// Whether to save payment method for future use
    /// </summary>
    public bool SavePaymentMethod { get; set; } = false;
}
