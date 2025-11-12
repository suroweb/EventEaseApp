namespace EventEase.API.DTOs;

/// <summary>
/// Event-specific analytics data
/// </summary>
public class EventAnalyticsResponse
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;

    // Attendance Metrics
    public int TotalRegistrations { get; set; }
    public int ConfirmedAttendees { get; set; }
    public int CancelledRegistrations { get; set; }
    public int CheckedInAttendees { get; set; }
    public int NoShowAttendees { get; set; }
    public decimal AttendanceRate { get; set; } // (CheckedIn / Confirmed) * 100
    public decimal CancellationRate { get; set; } // (Cancelled / Total) * 100
    public decimal NoShowRate { get; set; } // (NoShow / Confirmed) * 100

    // Capacity Metrics
    public int? MaxAttendees { get; set; }
    public decimal? CapacityUtilization { get; set; } // (Confirmed / MaxAttendees) * 100

    // Revenue Metrics
    public bool IsFree { get; set; }
    public decimal? TicketPrice { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal TotalRevenue { get; set; }
    public decimal AverageRevenuePerAttendee { get; set; }

    // Engagement Metrics
    public int InvitationsSent { get; set; }
    public int RegistrationsViaInvitation { get; set; }
    public decimal InvitationConversionRate { get; set; } // (RegViaInvite / InvitationsSent) * 100
    public decimal OverallConversionRate { get; set; } // (Confirmed / TotalRegistrations) * 100

    // Registration Timeline
    public Dictionary<string, int> RegistrationsByDate { get; set; } = new();
    public Dictionary<string, int> RegistrationsBySource { get; set; } = new(); // web, email, invitation, etc.

    // Demographics
    public Dictionary<string, int> AttendeesByCompany { get; set; } = new();
    public int UniqueCompanies { get; set; }
    public List<string> TopCompanies { get; set; } = new();

    // AI Usage
    public bool IsAIGenerated { get; set; }
    public decimal? AIGenerationCost { get; set; }
}
