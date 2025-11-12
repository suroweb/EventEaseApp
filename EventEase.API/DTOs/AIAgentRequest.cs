using EventEase.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EventEase.API.DTOs;

/// <summary>
/// Base request for AI agent operations
/// </summary>
public class AIAgentRequest
{
    /// <summary>
    /// Preferred AI provider (OpenAI or Anthropic)
    /// </summary>
    public AIProvider? PreferredProvider { get; set; }
}

/// <summary>
/// Planning Agent - Create event from description
/// </summary>
public class PlanningAgentCreateEventRequest : AIAgentRequest
{
    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Planning Agent - Get venue recommendations
/// </summary>
public class PlanningAgentVenueRequest : AIAgentRequest
{
    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = string.Empty;

    [Range(1, 10000)]
    public int ExpectedAttendees { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    public decimal? Budget { get; set; }
}

/// <summary>
/// Planning Agent - Optimize event schedule
/// </summary>
public class PlanningAgentScheduleRequest : AIAgentRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    [StringLength(1000)]
    public string Requirements { get; set; } = string.Empty;
}

/// <summary>
/// Planning Agent - Generate event content
/// </summary>
public class PlanningAgentContentRequest : AIAgentRequest
{
    [Required]
    [StringLength(200)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string TargetAudience { get; set; } = string.Empty;
}

/// <summary>
/// Invitation Agent - Select guests for event
/// </summary>
public class InvitationAgentSelectGuestsRequest : AIAgentRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Range(1, 1000)]
    public int MaxGuests { get; set; }

    [StringLength(500)]
    public string? SelectionCriteria { get; set; }
}

/// <summary>
/// Invitation Agent - Generate personalized invitations
/// </summary>
public class InvitationAgentPersonalizeRequest : AIAgentRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    [MinLength(1)]
    public List<Guid> GuestIds { get; set; } = new();

    [StringLength(500)]
    public string? CustomMessage { get; set; }
}

/// <summary>
/// Invitation Agent - Determine optimal send time
/// </summary>
public class InvitationAgentSendTimeRequest : AIAgentRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    [MinLength(1)]
    public List<Guid> GuestIds { get; set; } = new();
}

/// <summary>
/// Invitation Agent - Predict guest attendance
/// </summary>
public class InvitationAgentPredictRequest : AIAgentRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    public Guid GuestId { get; set; }
}

/// <summary>
/// Analytics Agent - Analyze sentiment
/// </summary>
public class AnalyticsAgentSentimentRequest : AIAgentRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    [MinLength(1)]
    public List<string> FeedbackTexts { get; set; } = new();
}

/// <summary>
/// Analytics Agent - Calculate ROI
/// </summary>
public class AnalyticsAgentROIRequest : AIAgentRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    [Range(0, 999999)]
    public decimal TotalCost { get; set; }
}

/// <summary>
/// Analytics Agent - Identify trends
/// </summary>
public class AnalyticsAgentTrendsRequest : AIAgentRequest
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Budget Agent - Estimate budget
/// </summary>
public class BudgetAgentEstimateRequest : AIAgentRequest
{
    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = string.Empty;

    [Range(1, 10000)]
    public int ExpectedAttendees { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    public Dictionary<string, object>? Requirements { get; set; }
}

/// <summary>
/// Budget Agent - Compare vendors
/// </summary>
public class BudgetAgentCompareVendorsRequest : AIAgentRequest
{
    [Required]
    [StringLength(100)]
    public string ServiceType { get; set; } = string.Empty;

    [Required]
    [MinLength(2)]
    public List<VendorQuoteDTO> Quotes { get; set; } = new();
}

/// <summary>
/// Vendor quote DTO
/// </summary>
public class VendorQuoteDTO
{
    [Required]
    [StringLength(200)]
    public string VendorName { get; set; } = string.Empty;

    [Required]
    [Range(0, 999999)]
    public decimal Price { get; set; }

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    public Dictionary<string, string>? Details { get; set; }
}

/// <summary>
/// Budget Agent - Analyze expenses
/// </summary>
public class BudgetAgentAnalyzeExpensesRequest : AIAgentRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    [MinLength(1)]
    public List<ExpenseDTO> Expenses { get; set; } = new();
}

/// <summary>
/// Expense DTO
/// </summary>
public class ExpenseDTO
{
    [Required]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [Range(0, 999999)]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; }
}

/// <summary>
/// Budget Agent - Get budget optimization
/// </summary>
public class BudgetAgentOptimizeRequest : AIAgentRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    [Range(0, 999999)]
    public decimal CurrentBudget { get; set; }
}

/// <summary>
/// Integration Agent - Generate calendar invite
/// </summary>
public class IntegrationAgentCalendarRequest : AIAgentRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    [StringLength(50)]
    public string CalendarType { get; set; } = "Google"; // Google, Outlook, Apple
}

/// <summary>
/// Integration Agent - Prepare CRM sync
/// </summary>
public class IntegrationAgentCRMRequest : AIAgentRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    [StringLength(50)]
    public string CRMType { get; set; } = "Salesforce"; // Salesforce, HubSpot, etc.
}

/// <summary>
/// Integration Agent - Generate team notifications
/// </summary>
public class IntegrationAgentNotificationRequest : AIAgentRequest
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    [StringLength(50)]
    public string Platform { get; set; } = "Slack"; // Slack, Teams

    [Required]
    [StringLength(50)]
    public string NotificationType { get; set; } = "reminder"; // reminder, update, summary
}

/// <summary>
/// Integration Agent - Parse external data
/// </summary>
public class IntegrationAgentParseRequest : AIAgentRequest
{
    [Required]
    [StringLength(5000)]
    public string SourceData { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string SourceType { get; set; } = "email"; // email, webpage, document
}
