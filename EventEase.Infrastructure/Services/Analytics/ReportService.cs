using ClosedXML.Excel;
using EventEase.API.DTOs;
using EventEase.Application.Interfaces;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text;

namespace EventEase.Infrastructure.Services.Analytics;

/// <summary>
/// Service for generating reports in various formats (PDF, Excel, CSV)
/// </summary>
public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IAnalyticsService _analyticsService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        ApplicationDbContext context,
        IAnalyticsService analyticsService,
        ICurrentTenantService currentTenantService,
        ILogger<ReportService> logger)
    {
        _context = context;
        _analyticsService = analyticsService;
        _currentTenantService = currentTenantService;
        _logger = logger;

        // Configure QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<(byte[] Data, string FileName, string ContentType)> GenerateReportAsync(
        ReportGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        return request.Format.ToLower() switch
        {
            "pdf" => (
                await GeneratePdfReportAsync(request, cancellationToken),
                GetFileName(request, "pdf"),
                "application/pdf"
            ),
            "excel" or "xlsx" => (
                await GenerateExcelReportAsync(request, cancellationToken),
                GetFileName(request, "xlsx"),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            ),
            "csv" => (
                await GenerateCsvReportAsync(request, cancellationToken),
                GetFileName(request, "csv"),
                "text/csv"
            ),
            _ => throw new ArgumentException($"Unsupported format: {request.Format}")
        };
    }

    public async Task<byte[]> GeneratePdfReportAsync(
        ReportGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants.FindAsync(
            new object[] { _currentTenantService.TenantId },
            cancellationToken);

        var reportData = await GetReportDataAsync(request, cancellationToken);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                // Header
                page.Header().Element(c => ComposeHeader(c, tenant?.Name ?? "EventEase", request));

                // Content
                page.Content().Element(c => ComposeContent(c, request.ReportType, reportData));

                // Footer
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                    text.Span($" | Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateExcelReportAsync(
        ReportGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants.FindAsync(
            new object[] { _currentTenantService.TenantId },
            cancellationToken);

        using var workbook = new XLWorkbook();

        switch (request.ReportType.ToLower())
        {
            case "event":
                if (request.EventId.HasValue)
                {
                    await AddEventAnalyticsSheet(workbook, request.EventId.Value, request, cancellationToken);
                }
                break;

            case "tenant":
                await AddTenantAnalyticsSheet(workbook, request, cancellationToken);
                break;

            case "registration":
                await AddRegistrationAnalyticsSheet(workbook, request, cancellationToken);
                break;

            case "credit":
                await AddCreditAnalyticsSheet(workbook, request, cancellationToken);
                break;

            default:
                throw new ArgumentException($"Unsupported report type: {request.ReportType}");
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GenerateCsvReportAsync(
        ReportGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var csv = new StringBuilder();

        switch (request.ReportType.ToLower())
        {
            case "event":
                if (request.EventId.HasValue)
                {
                    var analytics = await _analyticsService.GetEventAnalyticsAsync(
                        request.EventId.Value,
                        cancellationToken);
                    csv.AppendLine(GenerateEventAnalyticsCsv(analytics));
                }
                break;

            case "tenant":
                var tenantAnalytics = await _analyticsService.GetTenantAnalyticsAsync(
                    request.StartDate,
                    request.EndDate,
                    cancellationToken);
                csv.AppendLine(GenerateTenantAnalyticsCsv(tenantAnalytics));
                break;

            case "registration":
                var regAnalytics = await _analyticsService.GetRegistrationAnalyticsAsync(
                    request.EventId,
                    request.StartDate,
                    request.EndDate,
                    cancellationToken);
                csv.AppendLine(GenerateRegistrationAnalyticsCsv(regAnalytics));
                break;

            case "credit":
                var creditAnalytics = await _analyticsService.GetCreditAnalyticsAsync(
                    request.StartDate,
                    request.EndDate,
                    cancellationToken);
                csv.AppendLine(GenerateCreditAnalyticsCsv(creditAnalytics));
                break;
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<byte[]> ExportEventRegistrationsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var registrations = await _context.EventRegistrations
            .Include(r => r.Event)
            .Where(r => r.EventId == eventId)
            .OrderBy(r => r.RegisteredAt)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Registrations");

        // Headers
        var headers = new[] {
            "Registration ID", "Event Name", "First Name", "Last Name", "Email",
            "Phone", "Company", "Job Title", "Status", "Tickets", "Amount",
            "Registered At", "Confirmed At", "Checked In At", "Source"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data
        int row = 2;
        foreach (var reg in registrations)
        {
            worksheet.Cell(row, 1).Value = reg.Id.ToString();
            worksheet.Cell(row, 2).Value = reg.Event.Name;
            worksheet.Cell(row, 3).Value = reg.FirstName;
            worksheet.Cell(row, 4).Value = reg.LastName;
            worksheet.Cell(row, 5).Value = reg.Email;
            worksheet.Cell(row, 6).Value = reg.PhoneNumber ?? "";
            worksheet.Cell(row, 7).Value = reg.Company ?? "";
            worksheet.Cell(row, 8).Value = reg.JobTitle ?? "";
            worksheet.Cell(row, 9).Value = reg.Status.ToString();
            worksheet.Cell(row, 10).Value = reg.NumberOfTickets;
            worksheet.Cell(row, 11).Value = reg.TotalAmount;
            worksheet.Cell(row, 12).Value = reg.RegisteredAt.ToString("yyyy-MM-dd HH:mm");
            worksheet.Cell(row, 13).Value = reg.ConfirmedAt?.ToString("yyyy-MM-dd HH:mm") ?? "";
            worksheet.Cell(row, 14).Value = reg.CheckedInAt?.ToString("yyyy-MM-dd HH:mm") ?? "";
            worksheet.Cell(row, 15).Value = reg.RegistrationSource ?? "";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportGuestListAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var guests = await _context.Guests
            .Include(g => g.Event)
            .Where(g => g.EventId == eventId)
            .OrderBy(g => g.LastName)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Guest List");

        // Headers
        var headers = new[] {
            "Guest ID", "Event Name", "First Name", "Last Name", "Email",
            "Phone", "Company", "Job Title", "VIP Status", "Plus One Allowed",
            "Dietary Restrictions", "Special Requirements", "Notes", "Created At"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data
        int row = 2;
        foreach (var guest in guests)
        {
            worksheet.Cell(row, 1).Value = guest.Id.ToString();
            worksheet.Cell(row, 2).Value = guest.Event.Name;
            worksheet.Cell(row, 3).Value = guest.FirstName;
            worksheet.Cell(row, 4).Value = guest.LastName;
            worksheet.Cell(row, 5).Value = guest.Email;
            worksheet.Cell(row, 6).Value = guest.PhoneNumber ?? "";
            worksheet.Cell(row, 7).Value = guest.Company ?? "";
            worksheet.Cell(row, 8).Value = guest.JobTitle ?? "";
            worksheet.Cell(row, 9).Value = guest.IsVIP ? "Yes" : "No";
            worksheet.Cell(row, 10).Value = guest.AllowPlusOne ? "Yes" : "No";
            worksheet.Cell(row, 11).Value = guest.DietaryRestrictions ?? "";
            worksheet.Cell(row, 12).Value = guest.SpecialRequirements ?? "";
            worksheet.Cell(row, 13).Value = guest.Notes ?? "";
            worksheet.Cell(row, 14).Value = guest.CreatedAt.ToString("yyyy-MM-dd HH:mm");
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportCreditTransactionsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var transactions = await _context.CreditTransactions
            .Include(t => t.User)
            .Include(t => t.AIAgentUsage)
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Credit Transactions");

        // Headers
        var headers = new[] {
            "Transaction ID", "Date", "Type", "Amount", "Balance Before", "Balance After",
            "Description", "User", "Agent Type", "Notes"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data
        int row = 2;
        foreach (var txn in transactions)
        {
            worksheet.Cell(row, 1).Value = txn.Id.ToString();
            worksheet.Cell(row, 2).Value = txn.CreatedAt.ToString("yyyy-MM-dd HH:mm");
            worksheet.Cell(row, 3).Value = txn.Type.ToString();
            worksheet.Cell(row, 4).Value = txn.Amount;
            worksheet.Cell(row, 5).Value = txn.BalanceBefore;
            worksheet.Cell(row, 6).Value = txn.BalanceAfter;
            worksheet.Cell(row, 7).Value = txn.Description;
            worksheet.Cell(row, 8).Value = txn.User != null
                ? $"{txn.User.FirstName} {txn.User.LastName}"
                : "";
            worksheet.Cell(row, 9).Value = txn.AIAgentUsage?.AgentType.ToString() ?? "";
            worksheet.Cell(row, 10).Value = txn.Notes ?? "";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportPaymentTransactionsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var payments = await _context.PaymentTransactions
            .Include(p => p.CreditPackage)
            .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Payment Transactions");

        // Headers
        var headers = new[] {
            "Transaction ID", "Date", "Payment Intent ID", "Amount", "Currency",
            "Status", "Payment Method", "Card Brand", "Card Last 4",
            "Package Type", "Paid At", "Refunded Amount"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data
        int row = 2;
        foreach (var payment in payments)
        {
            worksheet.Cell(row, 1).Value = payment.Id.ToString();
            worksheet.Cell(row, 2).Value = payment.CreatedAt.ToString("yyyy-MM-dd HH:mm");
            worksheet.Cell(row, 3).Value = payment.StripePaymentIntentId;
            worksheet.Cell(row, 4).Value = payment.Amount;
            worksheet.Cell(row, 5).Value = payment.Currency;
            worksheet.Cell(row, 6).Value = payment.Status.ToString();
            worksheet.Cell(row, 7).Value = payment.PaymentMethod ?? "";
            worksheet.Cell(row, 8).Value = payment.CardBrand ?? "";
            worksheet.Cell(row, 9).Value = payment.CardLast4 ?? "";
            worksheet.Cell(row, 10).Value = payment.CreditPackage?.Type.ToString() ?? "";
            worksheet.Cell(row, 11).Value = payment.PaidAt?.ToString("yyyy-MM-dd HH:mm") ?? "";
            worksheet.Cell(row, 12).Value = payment.RefundedAmount ?? 0;
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public Task<Guid> ScheduleReportAsync(
        ScheduledReportRequest request,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement scheduled reports using background jobs (Hangfire/Quartz)
        _logger.LogInformation("Scheduled report created: {ReportName} ({Schedule})",
            request.ReportName, request.Schedule);

        return Task.FromResult(Guid.NewGuid());
    }

    public Task CancelScheduledReportAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement cancellation of scheduled reports
        _logger.LogInformation("Scheduled report cancelled: {ScheduleId}", scheduleId);

        return Task.CompletedTask;
    }

    public Task<List<ScheduledReportRequest>> GetScheduledReportsAsync(
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement retrieval of scheduled reports
        return Task.FromResult(new List<ScheduledReportRequest>());
    }

    // Helper methods for PDF generation
    private void ComposeHeader(IContainer container, string tenantName, ReportGenerationRequest request)
    {
        container.Column(column =>
        {
            column.Item().BorderBottom(1).PaddingBottom(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(request.CustomTitle ?? $"{request.ReportType} Report")
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    col.Item().Text(tenantName)
                        .FontSize(12)
                        .FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(100).AlignRight().Column(col =>
                {
                    col.Item().Text($"{DateTime.UtcNow:yyyy-MM-dd}")
                        .FontSize(10);

                    col.Item().Text($"Period: {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                });
            });
        });
    }

    private void ComposeContent(IContainer container, string reportType, object reportData)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(15);

            column.Item().Text($"Report Type: {reportType}")
                .FontSize(12)
                .Bold();

            column.Item().Text("This is a comprehensive analytics report generated by EventEase.")
                .FontSize(10);

            column.Item().Text("For detailed data, please refer to the Excel or CSV export.")
                .FontSize(10)
                .Italic();

            // Add summary metrics table
            column.Item().PaddingTop(20).Text("Summary Metrics")
                .FontSize(14)
                .Bold();

            column.Item().Text("Detailed metrics are available in the full report export.")
                .FontSize(10)
                .FontColor(Colors.Grey.Darken1);
        });
    }

    private async Task<object> GetReportDataAsync(
        ReportGenerationRequest request,
        CancellationToken cancellationToken)
    {
        return request.ReportType.ToLower() switch
        {
            "event" when request.EventId.HasValue =>
                await _analyticsService.GetEventAnalyticsAsync(request.EventId.Value, cancellationToken),
            "tenant" =>
                await _analyticsService.GetTenantAnalyticsAsync(request.StartDate, request.EndDate, cancellationToken),
            "registration" =>
                await _analyticsService.GetRegistrationAnalyticsAsync(request.EventId, request.StartDate, request.EndDate, cancellationToken),
            "credit" =>
                await _analyticsService.GetCreditAnalyticsAsync(request.StartDate, request.EndDate, cancellationToken),
            _ => throw new ArgumentException($"Unsupported report type: {request.ReportType}")
        };
    }

    private string GetFileName(ReportGenerationRequest request, string extension)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var reportName = request.ReportType.ToLower();
        return $"{reportName}_report_{timestamp}.{extension}";
    }

    // Helper methods for Excel generation
    private async Task AddEventAnalyticsSheet(
        XLWorkbook workbook,
        Guid eventId,
        ReportGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var analytics = await _analyticsService.GetEventAnalyticsAsync(eventId, cancellationToken);
        var worksheet = workbook.Worksheets.Add("Event Analytics");

        int row = 1;

        // Title
        worksheet.Cell(row, 1).Value = "Event Analytics Report";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Font.FontSize = 16;
        row += 2;

        // Event Info
        AddKeyValue(worksheet, ref row, "Event Name", analytics.EventName);
        AddKeyValue(worksheet, ref row, "Start Date", analytics.StartDate.ToString("yyyy-MM-dd HH:mm"));
        AddKeyValue(worksheet, ref row, "End Date", analytics.EndDate.ToString("yyyy-MM-dd HH:mm"));
        AddKeyValue(worksheet, ref row, "Status", analytics.Status);
        row++;

        // Attendance Metrics
        worksheet.Cell(row, 1).Value = "Attendance Metrics";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        row++;

        AddKeyValue(worksheet, ref row, "Total Registrations", analytics.TotalRegistrations);
        AddKeyValue(worksheet, ref row, "Confirmed Attendees", analytics.ConfirmedAttendees);
        AddKeyValue(worksheet, ref row, "Checked In", analytics.CheckedInAttendees);
        AddKeyValue(worksheet, ref row, "Cancelled", analytics.CancelledRegistrations);
        AddKeyValue(worksheet, ref row, "Attendance Rate", $"{analytics.AttendanceRate}%");
        AddKeyValue(worksheet, ref row, "Cancellation Rate", $"{analytics.CancellationRate}%");
        row++;

        // Revenue Metrics
        if (!analytics.IsFree)
        {
            worksheet.Cell(row, 1).Value = "Revenue Metrics";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            row++;

            AddKeyValue(worksheet, ref row, "Ticket Price", $"{analytics.TicketPrice} {analytics.Currency}");
            AddKeyValue(worksheet, ref row, "Total Revenue", $"{analytics.TotalRevenue} {analytics.Currency}");
            AddKeyValue(worksheet, ref row, "Avg Revenue/Attendee", $"{analytics.AverageRevenuePerAttendee} {analytics.Currency}");
        }

        worksheet.Columns().AdjustToContents();
    }

    private async Task AddTenantAnalyticsSheet(
        XLWorkbook workbook,
        ReportGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var analytics = await _analyticsService.GetTenantAnalyticsAsync(
            request.StartDate,
            request.EndDate,
            cancellationToken);

        var worksheet = workbook.Worksheets.Add("Tenant Analytics");

        int row = 1;

        // Title
        worksheet.Cell(row, 1).Value = "Tenant Analytics Report";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Font.FontSize = 16;
        row += 2;

        // Period
        AddKeyValue(worksheet, ref row, "Tenant", analytics.TenantName);
        AddKeyValue(worksheet, ref row, "Period Start", analytics.PeriodStart.ToString("yyyy-MM-dd"));
        AddKeyValue(worksheet, ref row, "Period End", analytics.PeriodEnd.ToString("yyyy-MM-dd"));
        row++;

        // Event Metrics
        worksheet.Cell(row, 1).Value = "Event Metrics";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        row++;

        AddKeyValue(worksheet, ref row, "Total Events", analytics.TotalEvents);
        AddKeyValue(worksheet, ref row, "Published Events", analytics.PublishedEvents);
        AddKeyValue(worksheet, ref row, "Upcoming Events", analytics.UpcomingEvents);
        AddKeyValue(worksheet, ref row, "Completed Events", analytics.CompletedEvents);
        row++;

        // Attendee Metrics
        worksheet.Cell(row, 1).Value = "Attendee Metrics";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        row++;

        AddKeyValue(worksheet, ref row, "Total Registrations", analytics.TotalRegistrations);
        AddKeyValue(worksheet, ref row, "Confirmed Attendees", analytics.ConfirmedAttendees);
        AddKeyValue(worksheet, ref row, "Total Check-Ins", analytics.TotalCheckIns);
        AddKeyValue(worksheet, ref row, "Unique Attendees", analytics.UniqueAttendees);
        AddKeyValue(worksheet, ref row, "Avg Attendees/Event", analytics.AverageAttendeesPerEvent);
        row++;

        // Revenue Metrics
        worksheet.Cell(row, 1).Value = "Revenue Metrics";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        row++;

        AddKeyValue(worksheet, ref row, "Total Revenue", $"{analytics.TotalRevenue} {analytics.Currency}");
        AddKeyValue(worksheet, ref row, "Avg Revenue/Event", $"{analytics.AverageRevenuePerEvent} {analytics.Currency}");
        AddKeyValue(worksheet, ref row, "Avg Revenue/Attendee", $"{analytics.AverageRevenuePerAttendee} {analytics.Currency}");
        row++;

        // Credit Metrics
        worksheet.Cell(row, 1).Value = "Credit Metrics";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        row++;

        AddKeyValue(worksheet, ref row, "Available Credits", analytics.AvailableCredits);
        AddKeyValue(worksheet, ref row, "Total Purchased", analytics.TotalCreditsPurchased);
        AddKeyValue(worksheet, ref row, "Total Used", analytics.TotalCreditsUsed);
        AddKeyValue(worksheet, ref row, "Usage Rate", $"{analytics.CreditUsageRate}%");
        row++;

        // Health Score
        worksheet.Cell(row, 1).Value = "Tenant Health Score";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        row++;

        AddKeyValue(worksheet, ref row, "Health Score", $"{analytics.TenantHealthScore}/100");

        worksheet.Columns().AdjustToContents();
    }

    private async Task AddRegistrationAnalyticsSheet(
        XLWorkbook workbook,
        ReportGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var analytics = await _analyticsService.GetRegistrationAnalyticsAsync(
            request.EventId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        var worksheet = workbook.Worksheets.Add("Registration Analytics");

        int row = 1;

        worksheet.Cell(row, 1).Value = "Registration Analytics Report";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Font.FontSize = 16;
        row += 2;

        AddKeyValue(worksheet, ref row, "Period Start", analytics.PeriodStart.ToString("yyyy-MM-dd"));
        AddKeyValue(worksheet, ref row, "Period End", analytics.PeriodEnd.ToString("yyyy-MM-dd"));

        if (!string.IsNullOrEmpty(analytics.EventName))
        {
            AddKeyValue(worksheet, ref row, "Event", analytics.EventName);
        }

        row++;

        worksheet.Cell(row, 1).Value = "Funnel Metrics";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        row++;

        AddKeyValue(worksheet, ref row, "Total Registrations", analytics.TotalRegistrations);
        AddKeyValue(worksheet, ref row, "Pending", analytics.PendingRegistrations);
        AddKeyValue(worksheet, ref row, "Confirmed", analytics.ConfirmedRegistrations);
        AddKeyValue(worksheet, ref row, "Cancelled", analytics.CancelledRegistrations);
        AddKeyValue(worksheet, ref row, "Waitlisted", analytics.WaitlistedRegistrations);
        row++;

        worksheet.Cell(row, 1).Value = "Conversion Rates";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        row++;

        AddKeyValue(worksheet, ref row, "Pending to Confirmed", $"{analytics.PendingToConfirmedRate}%");
        AddKeyValue(worksheet, ref row, "Cancellation Rate", $"{analytics.CancellationRate}%");
        AddKeyValue(worksheet, ref row, "Check-In Rate", $"{analytics.CheckInRate}%");

        worksheet.Columns().AdjustToContents();
    }

    private async Task AddCreditAnalyticsSheet(
        XLWorkbook workbook,
        ReportGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var analytics = await _analyticsService.GetCreditAnalyticsAsync(
            request.StartDate,
            request.EndDate,
            cancellationToken);

        var worksheet = workbook.Worksheets.Add("Credit Analytics");

        int row = 1;

        worksheet.Cell(row, 1).Value = "Credit Analytics Report";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Font.FontSize = 16;
        row += 2;

        AddKeyValue(worksheet, ref row, "Tenant", analytics.TenantName);
        AddKeyValue(worksheet, ref row, "Period Start", analytics.PeriodStart.ToString("yyyy-MM-dd"));
        AddKeyValue(worksheet, ref row, "Period End", analytics.PeriodEnd.ToString("yyyy-MM-dd"));
        row++;

        worksheet.Cell(row, 1).Value = "Current Balance";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        row++;

        AddKeyValue(worksheet, ref row, "Available Credits", analytics.CurrentBalance);
        AddKeyValue(worksheet, ref row, "Total Purchased", analytics.TotalPurchased);
        AddKeyValue(worksheet, ref row, "Total Used", analytics.TotalUsed);
        AddKeyValue(worksheet, ref row, "Total Refunded", analytics.TotalRefunded);
        row++;

        worksheet.Cell(row, 1).Value = "Spending Velocity";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        row++;

        AddKeyValue(worksheet, ref row, "Daily Avg Spend", analytics.DailyAverageSpend);
        AddKeyValue(worksheet, ref row, "Weekly Avg Spend", analytics.WeeklyAverageSpend);
        AddKeyValue(worksheet, ref row, "Monthly Avg Spend", analytics.MonthlyAverageSpend);
        AddKeyValue(worksheet, ref row, "Days Until Depletion", analytics.EstimatedDaysUntilDepletion);

        worksheet.Columns().AdjustToContents();
    }

    private void AddKeyValue(IXLWorksheet worksheet, ref int row, string key, object value)
    {
        worksheet.Cell(row, 1).Value = key;
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 2).Value = value?.ToString() ?? "";
        row++;
    }

    // Helper methods for CSV generation
    private string GenerateEventAnalyticsCsv(EventAnalyticsResponse analytics)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Metric,Value");
        csv.AppendLine($"Event Name,{EscapeCsv(analytics.EventName)}");
        csv.AppendLine($"Start Date,{analytics.StartDate:yyyy-MM-dd HH:mm}");
        csv.AppendLine($"Total Registrations,{analytics.TotalRegistrations}");
        csv.AppendLine($"Confirmed Attendees,{analytics.ConfirmedAttendees}");
        csv.AppendLine($"Checked In,{analytics.CheckedInAttendees}");
        csv.AppendLine($"Attendance Rate,{analytics.AttendanceRate}%");
        csv.AppendLine($"Total Revenue,{analytics.TotalRevenue} {analytics.Currency}");
        return csv.ToString();
    }

    private string GenerateTenantAnalyticsCsv(TenantAnalyticsResponse analytics)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Metric,Value");
        csv.AppendLine($"Tenant,{EscapeCsv(analytics.TenantName)}");
        csv.AppendLine($"Total Events,{analytics.TotalEvents}");
        csv.AppendLine($"Published Events,{analytics.PublishedEvents}");
        csv.AppendLine($"Total Registrations,{analytics.TotalRegistrations}");
        csv.AppendLine($"Confirmed Attendees,{analytics.ConfirmedAttendees}");
        csv.AppendLine($"Total Revenue,{analytics.TotalRevenue} {analytics.Currency}");
        csv.AppendLine($"Available Credits,{analytics.AvailableCredits}");
        csv.AppendLine($"Health Score,{analytics.TenantHealthScore}/100");
        return csv.ToString();
    }

    private string GenerateRegistrationAnalyticsCsv(RegistrationAnalyticsResponse analytics)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Metric,Value");
        csv.AppendLine($"Total Registrations,{analytics.TotalRegistrations}");
        csv.AppendLine($"Confirmed,{analytics.ConfirmedRegistrations}");
        csv.AppendLine($"Cancelled,{analytics.CancelledRegistrations}");
        csv.AppendLine($"Cancellation Rate,{analytics.CancellationRate}%");
        csv.AppendLine($"Check-In Rate,{analytics.CheckInRate}%");
        return csv.ToString();
    }

    private string GenerateCreditAnalyticsCsv(CreditAnalyticsResponse analytics)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Metric,Value");
        csv.AppendLine($"Current Balance,{analytics.CurrentBalance}");
        csv.AppendLine($"Total Purchased,{analytics.TotalPurchased}");
        csv.AppendLine($"Total Used,{analytics.TotalUsed}");
        csv.AppendLine($"Daily Avg Spend,{analytics.DailyAverageSpend}");
        csv.AppendLine($"Days Until Depletion,{analytics.EstimatedDaysUntilDepletion}");
        return csv.ToString();
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
