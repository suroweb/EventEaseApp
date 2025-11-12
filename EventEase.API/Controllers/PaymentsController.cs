using EventEase.API.DTOs;
using EventEase.Application.Interfaces;
using EventEase.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.API.Controllers;

/// <summary>
/// Payment processing endpoints for credit purchases
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "TenantOwnerOrAdmin")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IStripePaymentService _stripePayment;
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICurrentTenantService currentTenantService,
        IStripePaymentService stripePayment,
        IInvoiceService invoiceService,
        ILogger<PaymentsController> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _currentTenantService = currentTenantService;
        _stripePayment = stripePayment;
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Initiate payment for credit package purchase
    /// </summary>
    /// <param name="request">Payment initiation request</param>
    /// <response code="200">Payment intent created successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">Package not found</response>
    [HttpPost("initiate")]
    [ProducesResponseType(typeof(PaymentInitiationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentInitiationResponse>> InitiatePayment(
        [FromBody] InitiatePaymentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _stripePayment.CreatePaymentIntentAsync(
            _currentTenantService.TenantId,
            request.PackageId,
            request.PaymentMethodId,
            request.SavePaymentMethod);

        if (!result.Success)
        {
            return BadRequest(new PaymentInitiationResponse
            {
                Success = false,
                ErrorMessage = result.ErrorMessage,
                Message = "Payment initiation failed"
            });
        }

        _logger.LogInformation("Payment initiated: {PaymentIntentId} for tenant {TenantId}",
            result.PaymentIntentId, _currentTenantService.TenantId);

        return Ok(new PaymentInitiationResponse
        {
            Success = true,
            PaymentIntentId = result.PaymentIntentId,
            ClientSecret = result.ClientSecret,
            Status = result.Status,
            Amount = result.Amount,
            Currency = result.Currency,
            CustomerId = result.CustomerId,
            Message = "Payment intent created successfully. Use the client secret to complete payment on the frontend."
        });
    }

    /// <summary>
    /// Confirm a payment intent
    /// </summary>
    /// <param name="request">Payment confirmation request</param>
    /// <response code="200">Payment confirmed</response>
    /// <response code="400">Confirmation failed</response>
    [HttpPost("confirm")]
    [ProducesResponseType(typeof(PaymentConfirmationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentConfirmationResponse>> ConfirmPayment(
        [FromBody] ConfirmPaymentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _stripePayment.ConfirmPaymentIntentAsync(request.PaymentIntentId);

        if (!result.Success)
        {
            return BadRequest(new PaymentConfirmationResponse
            {
                Success = false,
                PaymentIntentId = request.PaymentIntentId,
                ErrorMessage = result.ErrorMessage,
                Message = "Payment confirmation failed"
            });
        }

        _logger.LogInformation("Payment confirmed: {PaymentIntentId}", request.PaymentIntentId);

        return Ok(new PaymentConfirmationResponse
        {
            Success = true,
            PaymentIntentId = result.PaymentIntentId,
            Status = result.Status,
            Message = "Payment confirmed successfully"
        });
    }

    /// <summary>
    /// Cancel a payment intent
    /// </summary>
    /// <param name="request">Payment cancellation request</param>
    /// <response code="200">Payment cancelled</response>
    /// <response code="400">Cancellation failed</response>
    [HttpPost("cancel")]
    [ProducesResponseType(typeof(PaymentConfirmationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentConfirmationResponse>> CancelPayment(
        [FromBody] CancelPaymentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var cancelled = await _stripePayment.CancelPaymentIntentAsync(request.PaymentIntentId);

        if (!cancelled)
        {
            return BadRequest(new PaymentConfirmationResponse
            {
                Success = false,
                PaymentIntentId = request.PaymentIntentId,
                Message = "Payment cancellation failed"
            });
        }

        _logger.LogInformation("Payment cancelled: {PaymentIntentId}", request.PaymentIntentId);

        return Ok(new PaymentConfirmationResponse
        {
            Success = true,
            PaymentIntentId = request.PaymentIntentId,
            Status = "canceled",
            Message = "Payment cancelled successfully"
        });
    }

    /// <summary>
    /// Create refund for a payment
    /// </summary>
    /// <param name="request">Refund request</param>
    /// <response code="200">Refund created</response>
    /// <response code="400">Refund failed</response>
    [HttpPost("refund")]
    [Authorize(Policy = "SystemAdminOnly")]
    [ProducesResponseType(typeof(RefundResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RefundResponse>> CreateRefund(
        [FromBody] CreateRefundRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _stripePayment.CreateRefundAsync(
            request.PaymentIntentId,
            request.Amount,
            request.Reason);

        if (!result.Success)
        {
            return BadRequest(new RefundResponse
            {
                Success = false,
                ErrorMessage = result.ErrorMessage,
                Message = "Refund creation failed"
            });
        }

        _logger.LogInformation("Refund created: {RefundId} for payment {PaymentIntentId}",
            result.RefundId, request.PaymentIntentId);

        return Ok(new RefundResponse
        {
            Success = true,
            RefundId = result.RefundId,
            Status = result.Status,
            Amount = result.Amount,
            Currency = result.Currency,
            Message = "Refund created successfully"
        });
    }

    /// <summary>
    /// Get saved payment methods
    /// </summary>
    /// <response code="200">Returns saved payment methods</response>
    [HttpGet("payment-methods")]
    [ProducesResponseType(typeof(List<PaymentMethodResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PaymentMethodResponse>>> GetPaymentMethods()
    {
        var customerId = await _stripePayment.GetOrCreateCustomerAsync(_currentTenantService.TenantId);

        var paymentMethods = await _stripePayment.GetCustomerPaymentMethodsAsync(customerId);

        var response = paymentMethods.Select(pm => new PaymentMethodResponse
        {
            Id = pm.Id,
            Type = pm.Type,
            Brand = pm.Brand,
            Last4 = pm.Last4,
            ExpMonth = pm.ExpMonth,
            ExpYear = pm.ExpYear,
            IsDefault = pm.IsDefault
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Get invoice for a purchase
    /// </summary>
    /// <param name="purchaseId">Purchase ID</param>
    /// <response code="200">Returns invoice</response>
    /// <response code="404">Invoice not found</response>
    [HttpGet("invoice/{purchaseId}")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceResponse>> GetInvoice(Guid purchaseId)
    {
        // Verify purchase belongs to tenant
        var purchase = await _context.CreditPurchases
            .FirstOrDefaultAsync(p => p.Id == purchaseId && p.TenantId == _currentTenantService.TenantId);

        if (purchase == null)
        {
            return NotFound(new { Error = "Invoice not found" });
        }

        var invoice = await _invoiceService.GetInvoiceAsync(purchaseId);

        if (invoice == null)
        {
            return NotFound(new { Error = "Invoice not available" });
        }

        var response = new InvoiceResponse
        {
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            BuyerName = invoice.BuyerName,
            BuyerEmail = invoice.BuyerEmail,
            Subtotal = invoice.Subtotal,
            TaxAmount = invoice.TaxAmount,
            Total = invoice.Total,
            Currency = invoice.Currency,
            PaymentStatus = invoice.PaymentStatus,
            PaidDate = invoice.PaidDate,
            LineItems = invoice.LineItems.Select(li => new InvoiceLineItemResponse
            {
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                Amount = li.Amount
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Download invoice PDF
    /// </summary>
    /// <param name="purchaseId">Purchase ID</param>
    /// <response code="200">Returns PDF file</response>
    /// <response code="404">Invoice not found</response>
    [HttpGet("invoice/{purchaseId}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadInvoicePdf(Guid purchaseId)
    {
        // Verify purchase belongs to tenant
        var purchase = await _context.CreditPurchases
            .FirstOrDefaultAsync(p => p.Id == purchaseId && p.TenantId == _currentTenantService.TenantId);

        if (purchase == null)
        {
            return NotFound(new { Error = "Invoice not found" });
        }

        try
        {
            var pdfBytes = await _invoiceService.GenerateInvoicePdfAsync(purchaseId);

            var invoice = await _invoiceService.GetInvoiceAsync(purchaseId);
            var fileName = $"{invoice?.InvoiceNumber ?? "Invoice"}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch
        {
            return NotFound(new { Error = "Invoice not available" });
        }
    }

    /// <summary>
    /// Send invoice via email
    /// </summary>
    /// <param name="purchaseId">Purchase ID</param>
    /// <response code="200">Invoice sent</response>
    /// <response code="404">Invoice not found</response>
    [HttpPost("invoice/{purchaseId}/send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendInvoiceEmail(Guid purchaseId)
    {
        // Verify purchase belongs to tenant
        var purchase = await _context.CreditPurchases
            .Include(p => p.Tenant)
            .FirstOrDefaultAsync(p => p.Id == purchaseId && p.TenantId == _currentTenantService.TenantId);

        if (purchase == null)
        {
            return NotFound(new { Error = "Invoice not found" });
        }

        var recipientEmail = purchase.Tenant.ContactEmail ?? _currentUserService.Email;

        var sent = await _invoiceService.SendInvoiceEmailAsync(purchaseId, recipientEmail);

        if (!sent)
        {
            return BadRequest(new { Error = "Failed to send invoice email" });
        }

        return Ok(new { Message = $"Invoice sent to {recipientEmail}" });
    }
}
