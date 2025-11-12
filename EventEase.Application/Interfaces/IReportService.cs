using EventEase.API.DTOs;

namespace EventEase.Application.Interfaces;

/// <summary>
/// Service for generating reports in various formats
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Generate a PDF report
    /// </summary>
    Task<byte[]> GeneratePdfReportAsync(
        ReportGenerationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate an Excel report
    /// </summary>
    Task<byte[]> GenerateExcelReportAsync(
        ReportGenerationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a CSV report
    /// </summary>
    Task<byte[]> GenerateCsvReportAsync(
        ReportGenerationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate report based on format specified in request
    /// </summary>
    Task<(byte[] Data, string FileName, string ContentType)> GenerateReportAsync(
        ReportGenerationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export event registrations to Excel
    /// </summary>
    Task<byte[]> ExportEventRegistrationsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export guest list to Excel
    /// </summary>
    Task<byte[]> ExportGuestListAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export credit transactions to Excel
    /// </summary>
    Task<byte[]> ExportCreditTransactionsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export payment transactions to Excel
    /// </summary>
    Task<byte[]> ExportPaymentTransactionsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedule a recurring report
    /// </summary>
    Task<Guid> ScheduleReportAsync(
        ScheduledReportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a scheduled report
    /// </summary>
    Task CancelScheduledReportAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of scheduled reports for the current tenant
    /// </summary>
    Task<List<ScheduledReportRequest>> GetScheduledReportsAsync(
        CancellationToken cancellationToken = default);
}
