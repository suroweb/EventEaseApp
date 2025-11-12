namespace EventEase.API.DTOs;

/// <summary>
/// Tenant-wide analytics data
/// </summary>
public class TenantAnalyticsResponse
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    // Event Metrics
    public int TotalEvents { get; set; }
    public int PublishedEvents { get; set; }
    public int DraftEvents { get; set; }
    public int CompletedEvents { get; set; }
    public int CancelledEvents { get; set; }
    public int UpcomingEvents { get; set; }

    // Attendee Metrics
    public int TotalRegistrations { get; set; }
    public int ConfirmedAttendees { get; set; }
    public int TotalCheckIns { get; set; }
    public int UniqueAttendees { get; set; } // Unique email addresses
    public decimal AverageAttendeesPerEvent { get; set; }

    // Revenue Metrics
    public decimal TotalRevenue { get; set; }
    public decimal AverageRevenuePerEvent { get; set; }
    public decimal AverageRevenuePerAttendee { get; set; }
    public string Currency { get; set; } = "EUR";

    // Engagement Metrics
    public int TotalInvitationsSent { get; set; }
    public int TotalGuestsManaged { get; set; }
    public decimal OverallConversionRate { get; set; }

    // Credit Metrics
    public decimal AvailableCredits { get; set; }
    public decimal TotalCreditsPurchased { get; set; }
    public decimal TotalCreditsUsed { get; set; }
    public decimal CreditUsageRate { get; set; } // (Used / Purchased) * 100

    // AI Usage Metrics
    public int TotalAIRequests { get; set; }
    public decimal TotalAICreditsCost { get; set; }
    public Dictionary<string, int> AIRequestsByAgent { get; set; } = new();
    public Dictionary<string, decimal> AICostByAgent { get; set; } = new();

    // Payment Metrics
    public int TotalPayments { get; set; }
    public decimal TotalPaymentAmount { get; set; }
    public int SuccessfulPayments { get; set; }
    public int FailedPayments { get; set; }
    public decimal PaymentSuccessRate { get; set; }

    // Trends
    public Dictionary<string, int> EventsByMonth { get; set; } = new();
    public Dictionary<string, int> AttendeesByMonth { get; set; } = new();
    public Dictionary<string, decimal> RevenueByMonth { get; set; } = new();

    // Health Score (0-100)
    public decimal TenantHealthScore { get; set; }
}
