namespace EventEase.Application.Interfaces;

/// <summary>
/// Service for generating invoices
/// </summary>
public interface IInvoiceService
{
    /// <summary>
    /// Generate invoice for credit purchase
    /// </summary>
    Task<InvoiceResult> GenerateInvoiceAsync(Guid purchaseId);

    /// <summary>
    /// Generate invoice PDF
    /// </summary>
    Task<byte[]> GenerateInvoicePdfAsync(Guid purchaseId);

    /// <summary>
    /// Send invoice email
    /// </summary>
    Task<bool> SendInvoiceEmailAsync(Guid purchaseId, string recipientEmail);

    /// <summary>
    /// Get invoice details
    /// </summary>
    Task<InvoiceResult?> GetInvoiceAsync(Guid purchaseId);
}

/// <summary>
/// Invoice result
/// </summary>
public class InvoiceResult
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }

    // Seller info
    public string SellerName { get; set; } = "EventEase";
    public string SellerAddress { get; set; } = string.Empty;
    public string SellerVatNumber { get; set; } = string.Empty;

    // Buyer info
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public string? BuyerAddress { get; set; }
    public string? BuyerVatNumber { get; set; }

    // Line items
    public List<InvoiceLineItem> LineItems { get; set; } = new();

    // Totals
    public decimal Subtotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "EUR";

    // Payment info
    public string PaymentStatus { get; set; } = "Paid";
    public DateTime? PaidDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }

    // Notes
    public string? Notes { get; set; }
}

/// <summary>
/// Invoice line item
/// </summary>
public class InvoiceLineItem
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount => Quantity * UnitPrice;
}
