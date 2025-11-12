using EventEase.Domain.Common;
using EventEase.Domain.Enums;

namespace EventEase.Domain.Entities;

/// <summary>
/// Represents a tenant (company/organization) in the multi-tenant system
/// </summary>
public class Tenant : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? CompanyRegistrationNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }

    // Contact Information
    public string PrimaryContactEmail { get; set; } = string.Empty;
    public string? PrimaryContactPhone { get; set; }

    // Address
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    // Subscription & Status
    public TenantStatus Status { get; set; } = TenantStatus.Trial;
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? SubscriptionStartedAt { get; set; }
    public DateTime? SubscriptionEndsAt { get; set; }

    // Credits
    public decimal AvailableCredits { get; set; } = 0;
    public decimal TotalCreditsPurchased { get; set; } = 0;
    public decimal TotalCreditsUsed { get; set; } = 0;

    // Stripe Integration
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }

    // Settings
    public string? TimeZone { get; set; } = "UTC";
    public string? DefaultCurrency { get; set; } = "EUR";
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    public virtual ICollection<CreditTransaction> CreditTransactions { get; set; } = new List<CreditTransaction>();
    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
