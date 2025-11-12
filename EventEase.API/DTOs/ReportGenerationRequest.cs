using System.ComponentModel.DataAnnotations;

namespace EventEase.API.DTOs;

/// <summary>
/// Request to generate a report
/// </summary>
public class ReportGenerationRequest
{
    /// <summary>
    /// Type of report to generate
    /// </summary>
    [Required]
    public string ReportType { get; set; } = string.Empty; // "event", "tenant", "registration", "credit"

    /// <summary>
    /// Output format
    /// </summary>
    [Required]
    public string Format { get; set; } = "pdf"; // "pdf", "excel", "csv"

    /// <summary>
    /// Start date for the report period
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date for the report period
    /// </summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Optional: Specific event ID for event reports
    /// </summary>
    public Guid? EventId { get; set; }

    /// <summary>
    /// Optional: Include detailed data
    /// </summary>
    public bool IncludeDetails { get; set; } = true;

    /// <summary>
    /// Optional: Include charts and visualizations
    /// </summary>
    public bool IncludeCharts { get; set; } = true;

    /// <summary>
    /// Optional: Custom report title
    /// </summary>
    public string? CustomTitle { get; set; }

    /// <summary>
    /// Optional: Additional filters as JSON
    /// </summary>
    public string? Filters { get; set; }
}

/// <summary>
/// Response after report generation
/// </summary>
public class ReportGenerationResponse
{
    public Guid ReportId { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public long FileSizeBytes { get; set; }
}

/// <summary>
/// Scheduled report configuration
/// </summary>
public class ScheduledReportRequest
{
    [Required]
    public string ReportName { get; set; } = string.Empty;

    [Required]
    public string ReportType { get; set; } = string.Empty;

    [Required]
    public string Format { get; set; } = "pdf";

    [Required]
    public string Schedule { get; set; } = "weekly"; // "daily", "weekly", "monthly"

    [Required]
    [EmailAddress]
    public List<string> Recipients { get; set; } = new();

    public bool IncludeDetails { get; set; } = true;
    public bool IncludeCharts { get; set; } = true;
    public string? Filters { get; set; }
    public bool IsActive { get; set; } = true;
}
