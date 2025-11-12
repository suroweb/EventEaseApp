namespace EventEase.API.DTOs;

/// <summary>
/// Registration funnel and trends analytics
/// </summary>
public class RegistrationAnalyticsResponse
{
    public Guid? EventId { get; set; }
    public string? EventName { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    // Funnel Metrics
    public int TotalRegistrations { get; set; }
    public int PendingRegistrations { get; set; }
    public int ConfirmedRegistrations { get; set; }
    public int CancelledRegistrations { get; set; }
    public int WaitlistedRegistrations { get; set; }

    // Conversion Rates
    public decimal PendingToConfirmedRate { get; set; }
    public decimal CancellationRate { get; set; }
    public decimal CheckInRate { get; set; }

    // Timeline Analysis
    public Dictionary<string, int> RegistrationsByDate { get; set; } = new();
    public Dictionary<string, int> RegistrationsByDayOfWeek { get; set; } = new();
    public Dictionary<string, int> RegistrationsByHour { get; set; } = new();

    // Source Analysis
    public Dictionary<string, int> RegistrationsBySource { get; set; } = new();
    public string MostEffectiveSource { get; set; } = string.Empty;

    // Performance Metrics
    public TimeSpan AverageTimeToConfirm { get; set; }
    public TimeSpan AverageTimeToCheckIn { get; set; }

    // Demographics
    public Dictionary<string, int> RegistrationsByCompany { get; set; } = new();
    public int ReturningAttendees { get; set; } // Attended previous events
    public int NewAttendees { get; set; }
    public decimal ReturningAttendeeRate { get; set; }
}
