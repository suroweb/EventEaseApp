using EventEase.Domain.Entities;

namespace EventEase.API.GraphQL.Types;

/// <summary>
/// GraphQL ObjectType for Tenant entity
/// </summary>
public class TenantType : ObjectType<Tenant>
{
    protected override void Configure(IObjectTypeDescriptor<Tenant> descriptor)
    {
        descriptor.Description("Represents a tenant (company/organization) in the multi-tenant system");

        descriptor
            .Field(t => t.Id)
            .Description("Unique identifier for the tenant");

        descriptor
            .Field(t => t.Name)
            .Description("Tenant name");

        descriptor
            .Field(t => t.CompanyRegistrationNumber)
            .Description("Company registration number");

        descriptor
            .Field(t => t.VatNumber)
            .Description("VAT number");

        descriptor
            .Field(t => t.Website)
            .Description("Company website");

        descriptor
            .Field(t => t.LogoUrl)
            .Description("Company logo URL");

        descriptor
            .Field(t => t.PrimaryContactEmail)
            .Description("Primary contact email");

        descriptor
            .Field(t => t.PrimaryContactPhone)
            .Description("Primary contact phone");

        descriptor
            .Field(t => t.City)
            .Description("City");

        descriptor
            .Field(t => t.Country)
            .Description("Country");

        descriptor
            .Field(t => t.Status)
            .Description("Tenant status (Trial, Active, Suspended, etc.)");

        descriptor
            .Field(t => t.TrialEndsAt)
            .Description("When the trial period ends");

        descriptor
            .Field(t => t.SubscriptionStartedAt)
            .Description("When the subscription started");

        descriptor
            .Field(t => t.AvailableCredits)
            .Description("Available AI credits");

        descriptor
            .Field(t => t.TotalCreditsPurchased)
            .Description("Total credits purchased");

        descriptor
            .Field(t => t.TotalCreditsUsed)
            .Description("Total credits used");

        descriptor
            .Field(t => t.TimeZone)
            .Description("Default timezone");

        descriptor
            .Field(t => t.DefaultCurrency)
            .Description("Default currency");

        descriptor
            .Field(t => t.IsActive)
            .Description("Whether the tenant is active");

        descriptor
            .Field(t => t.CreatedAt)
            .Description("When the tenant was created");

        descriptor
            .Field(t => t.UpdatedAt)
            .Description("When the tenant was last updated");

        // Sensitive fields - exclude from GraphQL
        descriptor.Ignore(t => t.StripeCustomerId);
        descriptor.Ignore(t => t.StripeSubscriptionId);
    }
}
