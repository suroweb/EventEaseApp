namespace EventEase.API.DTOs;

/// <summary>
/// Payment initiation response
/// </summary>
public class PaymentInitiationResponse
{
    public bool Success { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? ClientSecret { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? CustomerId { get; set; }
    public string? ErrorMessage { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Payment confirmation response
/// </summary>
public class PaymentConfirmationResponse
{
    public bool Success { get; set; }
    public string? PaymentIntentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Refund response
/// </summary>
public class RefundResponse
{
    public bool Success { get; set; }
    public string? RefundId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? ErrorMessage { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Payment method response
/// </summary>
public class PaymentMethodResponse
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Last4 { get; set; }
    public int? ExpMonth { get; set; }
    public int? ExpYear { get; set; }
    public bool IsDefault { get; set; }
}

/// <summary>
/// Invoice response
/// </summary>
public class InvoiceResponse
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "EUR";
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaidDate { get; set; }
    public List<InvoiceLineItemResponse> LineItems { get; set; } = new();
}

/// <summary>
/// Invoice line item response
/// </summary>
public class InvoiceLineItemResponse
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}
