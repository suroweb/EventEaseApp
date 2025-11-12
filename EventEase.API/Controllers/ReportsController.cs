using EventEase.API.DTOs;
using EventEase.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventEase.API.Controllers;

/// <summary>
/// Report generation and export endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportService reportService,
        ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Generate a comprehensive report in specified format
    /// </summary>
    /// <param name="request">Report generation parameters</param>
    /// <response code="200">Returns the generated report file</response>
    /// <response code="400">Invalid request parameters</response>
    [HttpPost("generate")]
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateReport([FromBody] ReportGenerationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.EndDate <= request.StartDate)
            {
                return BadRequest(new { Error = "End date must be after start date" });
            }

            var validFormats = new[] { "pdf", "excel", "xlsx", "csv" };
            if (!validFormats.Contains(request.Format.ToLower()))
            {
                return BadRequest(new
                {
                    Error = $"Invalid format. Must be one of: {string.Join(", ", validFormats)}"
                });
            }

            var (data, fileName, contentType) = await _reportService.GenerateReportAsync(request);

            _logger.LogInformation("Report generated: {ReportType} in {Format} format",
                request.ReportType, request.Format);

            return File(data, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid report generation request");
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            return StatusCode(500, new { Error = "Failed to generate report" });
        }
    }

    /// <summary>
    /// Generate PDF report
    /// </summary>
    /// <param name="request">Report generation parameters</param>
    /// <response code="200">Returns PDF file</response>
    [HttpPost("pdf")]
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/pdf")]
    public async Task<IActionResult> GeneratePdfReport([FromBody] ReportGenerationRequest request)
    {
        try
        {
            request.Format = "pdf";
            var pdfData = await _reportService.GeneratePdfReportAsync(request);

            var fileName = $"{request.ReportType}_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

            _logger.LogInformation("PDF report generated: {ReportType}", request.ReportType);

            return File(pdfData, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF report");
            return StatusCode(500, new { Error = "Failed to generate PDF report" });
        }
    }

    /// <summary>
    /// Generate Excel report
    /// </summary>
    /// <param name="request">Report generation parameters</param>
    /// <response code="200">Returns Excel file</response>
    [HttpPost("excel")]
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> GenerateExcelReport([FromBody] ReportGenerationRequest request)
    {
        try
        {
            request.Format = "excel";
            var excelData = await _reportService.GenerateExcelReportAsync(request);

            var fileName = $"{request.ReportType}_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            _logger.LogInformation("Excel report generated: {ReportType}", request.ReportType);

            return File(excelData,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Excel report");
            return StatusCode(500, new { Error = "Failed to generate Excel report" });
        }
    }

    /// <summary>
    /// Generate CSV report
    /// </summary>
    /// <param name="request">Report generation parameters</param>
    /// <response code="200">Returns CSV file</response>
    [HttpPost("csv")]
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("text/csv")]
    public async Task<IActionResult> GenerateCsvReport([FromBody] ReportGenerationRequest request)
    {
        try
        {
            request.Format = "csv";
            var csvData = await _reportService.GenerateCsvReportAsync(request);

            var fileName = $"{request.ReportType}_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

            _logger.LogInformation("CSV report generated: {ReportType}", request.ReportType);

            return File(csvData, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating CSV report");
            return StatusCode(500, new { Error = "Failed to generate CSV report" });
        }
    }

    /// <summary>
    /// Export event registrations to Excel
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <response code="200">Returns Excel file with registrations</response>
    /// <response code="404">Event not found</response>
    [HttpGet("export/events/{eventId}/registrations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> ExportEventRegistrations(Guid eventId)
    {
        try
        {
            var excelData = await _reportService.ExportEventRegistrationsAsync(eventId);

            var fileName = $"event_registrations_{eventId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            _logger.LogInformation("Event registrations exported for event {EventId}", eventId);

            return File(excelData,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting event registrations for {EventId}", eventId);
            return StatusCode(500, new { Error = "Failed to export event registrations" });
        }
    }

    /// <summary>
    /// Export guest list to Excel
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <response code="200">Returns Excel file with guest list</response>
    [HttpGet("export/events/{eventId}/guests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> ExportGuestList(Guid eventId)
    {
        try
        {
            var excelData = await _reportService.ExportGuestListAsync(eventId);

            var fileName = $"guest_list_{eventId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            _logger.LogInformation("Guest list exported for event {EventId}", eventId);

            return File(excelData,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting guest list for {EventId}", eventId);
            return StatusCode(500, new { Error = "Failed to export guest list" });
        }
    }

    /// <summary>
    /// Export credit transactions to Excel
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <response code="200">Returns Excel file with credit transactions</response>
    [HttpGet("export/credits")]
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> ExportCreditTransactions(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            if (endDate <= startDate)
            {
                return BadRequest(new { Error = "End date must be after start date" });
            }

            var excelData = await _reportService.ExportCreditTransactionsAsync(startDate, endDate);

            var fileName = $"credit_transactions_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            _logger.LogInformation("Credit transactions exported for period {StartDate} to {EndDate}",
                startDate, endDate);

            return File(excelData,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting credit transactions");
            return StatusCode(500, new { Error = "Failed to export credit transactions" });
        }
    }

    /// <summary>
    /// Export payment transactions to Excel
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <response code="200">Returns Excel file with payment transactions</response>
    [HttpGet("export/payments")]
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> ExportPaymentTransactions(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            if (endDate <= startDate)
            {
                return BadRequest(new { Error = "End date must be after start date" });
            }

            var excelData = await _reportService.ExportPaymentTransactionsAsync(startDate, endDate);

            var fileName = $"payment_transactions_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            _logger.LogInformation("Payment transactions exported for period {StartDate} to {EndDate}",
                startDate, endDate);

            return File(excelData,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting payment transactions");
            return StatusCode(500, new { Error = "Failed to export payment transactions" });
        }
    }

    /// <summary>
    /// Schedule a recurring report
    /// </summary>
    /// <param name="request">Scheduled report configuration</param>
    /// <response code="201">Report scheduled successfully</response>
    [HttpPost("schedule")]
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScheduleReport([FromBody] ScheduledReportRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var scheduleId = await _reportService.ScheduleReportAsync(request);

            _logger.LogInformation("Scheduled report created: {ReportName} ({Schedule})",
                request.ReportName, request.Schedule);

            return CreatedAtAction(
                nameof(GetScheduledReports),
                new { id = scheduleId },
                new
                {
                    ScheduleId = scheduleId,
                    ReportName = request.ReportName,
                    Schedule = request.Schedule,
                    IsActive = request.IsActive,
                    Message = "Report scheduled successfully"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling report");
            return StatusCode(500, new { Error = "Failed to schedule report" });
        }
    }

    /// <summary>
    /// Get list of scheduled reports
    /// </summary>
    /// <response code="200">Returns list of scheduled reports</response>
    [HttpGet("schedule")]
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [ProducesResponseType(typeof(List<ScheduledReportRequest>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ScheduledReportRequest>>> GetScheduledReports()
    {
        try
        {
            var reports = await _reportService.GetScheduledReportsAsync();

            return Ok(reports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scheduled reports");
            return StatusCode(500, new { Error = "Failed to retrieve scheduled reports" });
        }
    }

    /// <summary>
    /// Cancel a scheduled report
    /// </summary>
    /// <param name="scheduleId">Schedule ID</param>
    /// <response code="204">Report schedule cancelled successfully</response>
    /// <response code="404">Schedule not found</response>
    [HttpDelete("schedule/{scheduleId}")]
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelScheduledReport(Guid scheduleId)
    {
        try
        {
            await _reportService.CancelScheduledReportAsync(scheduleId);

            _logger.LogInformation("Scheduled report cancelled: {ScheduleId}", scheduleId);

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Error = $"Schedule {scheduleId} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling scheduled report {ScheduleId}", scheduleId);
            return StatusCode(500, new { Error = "Failed to cancel scheduled report" });
        }
    }
}
