using EventEase.Application.Interfaces;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace EventEase.Infrastructure.Services.Payment;

/// <summary>
/// Invoice generation service
/// </summary>
public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<InvoiceService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<InvoiceResult> GenerateInvoiceAsync(Guid purchaseId)
    {
        try
        {
            var purchase = await _context.CreditPurchases
                .Include(p => p.Package)
                .Include(p => p.Tenant)
                .FirstOrDefaultAsync(p => p.Id == purchaseId);

            if (purchase == null)
            {
                throw new InvalidOperationException("Purchase not found");
            }

            if (purchase.PaymentStatus != PaymentStatus.Completed)
            {
                throw new InvalidOperationException("Cannot generate invoice for unpaid purchase");
            }

            // Generate invoice number
            var invoiceNumber = GenerateInvoiceNumber(purchase);

            // Calculate tax (assuming 19% VAT for EU)
            var taxRate = 0.19m;
            var subtotal = purchase.Amount;
            var taxAmount = subtotal * taxRate;
            var total = subtotal + taxAmount;

            // Create invoice
            var invoice = new InvoiceResult
            {
                InvoiceNumber = invoiceNumber,
                InvoiceDate = purchase.PaidAt ?? purchase.CreatedAt,
                DueDate = purchase.PaidAt, // Already paid

                // Seller info (EventEase)
                SellerName = "EventEase GmbH",
                SellerAddress = "123 Tech Street, 10115 Berlin, Germany",
                SellerVatNumber = "DE123456789",

                // Buyer info
                BuyerName = purchase.Tenant.Name,
                BuyerEmail = purchase.Tenant.ContactEmail ?? "Unknown",

                // Line items
                LineItems = new List<InvoiceLineItem>
                {
                    new InvoiceLineItem
                    {
                        Description = $"{purchase.Package?.Name ?? "Credits"} - {purchase.CreditsGranted} credits",
                        Quantity = 1,
                        UnitPrice = subtotal
                    }
                },

                // Totals
                Subtotal = subtotal,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                Total = total,
                Currency = purchase.Currency,

                // Payment info
                PaymentStatus = "Paid",
                PaidDate = purchase.PaidAt,
                PaymentMethod = "Card", // Would get from Stripe
                TransactionId = purchase.StripePaymentIntentId,

                // Notes
                Notes = $"Thank you for your purchase! {purchase.CreditsGranted} credits have been added to your account and will expire on {purchase.ExpiresAt:yyyy-MM-dd}."
            };

            _logger.LogInformation("Generated invoice {InvoiceNumber} for purchase {PurchaseId}",
                invoiceNumber, purchaseId);

            return invoice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice for purchase {PurchaseId}", purchaseId);
            throw;
        }
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(Guid purchaseId)
    {
        try
        {
            var invoice = await GenerateInvoiceAsync(purchaseId);

            // NOTE: In production, would use a PDF library like:
            // - QuestPDF (modern, fluent API)
            // - iTextSharp / iText7
            // - PdfSharpCore
            // - Rotativa (for HTML to PDF)

            // For now, generate simple text representation
            var pdfContent = GenerateInvoiceText(invoice);
            var pdfBytes = Encoding.UTF8.GetBytes(pdfContent);

            _logger.LogInformation("Generated PDF for invoice {InvoiceNumber}", invoice.InvoiceNumber);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for purchase {PurchaseId}", purchaseId);
            throw;
        }
    }

    public async Task<bool> SendInvoiceEmailAsync(Guid purchaseId, string recipientEmail)
    {
        try
        {
            var invoice = await GenerateInvoiceAsync(purchaseId);

            // TODO: Phase 1.6 - Implement with SendGrid
            // 1. Generate PDF
            // 2. Create email with SendGrid
            // 3. Attach PDF
            // 4. Send to recipient

            _logger.LogInformation("Invoice email would be sent to {Email} for invoice {InvoiceNumber}",
                recipientEmail, invoice.InvoiceNumber);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invoice email for purchase {PurchaseId}", purchaseId);
            return false;
        }
    }

    public async Task<InvoiceResult?> GetInvoiceAsync(Guid purchaseId)
    {
        try
        {
            return await GenerateInvoiceAsync(purchaseId);
        }
        catch
        {
            return null;
        }
    }

    private string GenerateInvoiceNumber(Domain.Entities.CreditPurchase purchase)
    {
        // Format: INV-YYYYMMDD-XXXXX
        // Example: INV-20251112-00001
        var date = purchase.PaidAt ?? purchase.CreatedAt;
        var datePrefix = date.ToString("yyyyMMdd");
        var sequenceNumber = purchase.Id.ToString()[..5].ToUpper();

        return $"INV-{datePrefix}-{sequenceNumber}";
    }

    private string GenerateInvoiceText(InvoiceResult invoice)
    {
        var sb = new StringBuilder();

        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("                         INVOICE                                ");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"Invoice Number: {invoice.InvoiceNumber}");
        sb.AppendLine($"Invoice Date:   {invoice.InvoiceDate:yyyy-MM-dd}");
        sb.AppendLine($"Due Date:       {invoice.DueDate:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine("FROM:");
        sb.AppendLine($"  {invoice.SellerName}");
        sb.AppendLine($"  {invoice.SellerAddress}");
        sb.AppendLine($"  VAT: {invoice.SellerVatNumber}");
        sb.AppendLine();
        sb.AppendLine("BILL TO:");
        sb.AppendLine($"  {invoice.BuyerName}");
        sb.AppendLine($"  {invoice.BuyerEmail}");
        if (!string.IsNullOrEmpty(invoice.BuyerVatNumber))
        {
            sb.AppendLine($"  VAT: {invoice.BuyerVatNumber}");
        }
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine();
        sb.AppendLine("DESCRIPTION                                QTY    UNIT PRICE    AMOUNT");
        sb.AppendLine("───────────────────────────────────────────────────────────────");

        foreach (var item in invoice.LineItems)
        {
            sb.AppendLine($"{item.Description,-42} {item.Quantity,3}  {item.UnitPrice,10:F2}  {item.Amount,10:F2}");
        }

        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine($"{"Subtotal:",-59} {invoice.Subtotal,10:F2} {invoice.Currency}");
        sb.AppendLine($"{"Tax (" + (invoice.TaxRate * 100):F0}%):",-59} {invoice.TaxAmount,10:F2} {invoice.Currency}");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"{"TOTAL:",-59} {invoice.Total,10:F2} {invoice.Currency}");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"Payment Status: {invoice.PaymentStatus}");
        sb.AppendLine($"Paid Date:      {invoice.PaidDate:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Payment Method: {invoice.PaymentMethod}");
        sb.AppendLine($"Transaction ID: {invoice.TransactionId}");
        sb.AppendLine();
        if (!string.IsNullOrEmpty(invoice.Notes))
        {
            sb.AppendLine("NOTES:");
            sb.AppendLine(invoice.Notes);
            sb.AppendLine();
        }
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine("Thank you for your business!");
        sb.AppendLine("For support, contact: support@eventease.com");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");

        return sb.ToString();
    }
}
