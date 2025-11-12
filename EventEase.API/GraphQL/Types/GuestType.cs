using EventEase.API.GraphQL.DataLoaders;
using EventEase.Domain.Entities;

namespace EventEase.API.GraphQL.Types;

/// <summary>
/// GraphQL ObjectType for Guest entity
/// </summary>
public class GuestType : ObjectType<Guest>
{
    protected override void Configure(IObjectTypeDescriptor<Guest> descriptor)
    {
        descriptor.Description("Represents a guest/contact for invitation purposes");

        descriptor
            .Field(g => g.Id)
            .Description("Unique identifier for the guest");

        descriptor
            .Field(g => g.TenantId)
            .Description("Tenant that owns this guest");

        descriptor
            .Field(g => g.Email)
            .Description("Guest email address");

        descriptor
            .Field(g => g.FirstName)
            .Description("Guest first name");

        descriptor
            .Field(g => g.LastName)
            .Description("Guest last name");

        descriptor
            .Field(g => g.PhoneNumber)
            .Description("Guest phone number");

        descriptor
            .Field(g => g.Company)
            .Description("Guest company");

        descriptor
            .Field(g => g.JobTitle)
            .Description("Guest job title");

        descriptor
            .Field(g => g.Industry)
            .Description("Guest industry");

        descriptor
            .Field(g => g.Department)
            .Description("Guest department");

        descriptor
            .Field(g => g.City)
            .Description("Guest city");

        descriptor
            .Field(g => g.Country)
            .Description("Guest country");

        descriptor
            .Field(g => g.TotalEventsAttended)
            .Description("Total number of events attended");

        descriptor
            .Field(g => g.TotalInvitationsSent)
            .Description("Total number of invitations sent");

        descriptor
            .Field(g => g.TotalInvitationsAccepted)
            .Description("Total number of invitations accepted");

        descriptor
            .Field(g => g.EngagementScore)
            .Description("Engagement score (ML-calculated)");

        descriptor
            .Field(g => g.LastInvitationSentAt)
            .Description("When the last invitation was sent");

        descriptor
            .Field(g => g.LastEventAttendedAt)
            .Description("When the guest last attended an event");

        descriptor
            .Field(g => g.OptedOutOfMarketing)
            .Description("Whether guest opted out of marketing");

        descriptor
            .Field(g => g.PreferredLanguage)
            .Description("Guest's preferred language");

        descriptor
            .Field(g => g.TimeZone)
            .Description("Guest's timezone");

        descriptor
            .Field(g => g.Source)
            .Description("Source of the guest (manual, import, crm, etc.)");

        descriptor
            .Field(g => g.CreatedAt)
            .Description("When the guest was created");

        descriptor
            .Field(g => g.UpdatedAt)
            .Description("When the guest was last updated");

        // Navigation properties
        descriptor
            .Field(g => g.Tenant)
            .ResolveWith<GuestResolvers>(r => r.GetTenantAsync(default!, default!, default))
            .Description("Tenant that owns this guest");

        // Computed field
        descriptor
            .Field("fullName")
            .Type<StringType>()
            .ResolveWith<GuestResolvers>(r => r.GetFullName(default!))
            .Description("Full name of the guest");

        descriptor
            .Field("acceptanceRate")
            .Type<FloatType>()
            .ResolveWith<GuestResolvers>(r => r.GetAcceptanceRate(default!))
            .Description("Invitation acceptance rate");
    }
}

/// <summary>
/// Resolvers for Guest GraphQL type
/// </summary>
public class GuestResolvers
{
    public async Task<Tenant> GetTenantAsync(
        [Parent] Guest guest,
        TenantByIdDataLoader tenantLoader,
        CancellationToken cancellationToken)
    {
        return await tenantLoader.LoadAsync(guest.TenantId, cancellationToken);
    }

    public string GetFullName([Parent] Guest guest)
    {
        if (!string.IsNullOrEmpty(guest.FirstName) && !string.IsNullOrEmpty(guest.LastName))
            return $"{guest.FirstName} {guest.LastName}";

        return guest.FirstName ?? guest.LastName ?? guest.Email;
    }

    public double GetAcceptanceRate([Parent] Guest guest)
    {
        if (guest.TotalInvitationsSent == 0)
            return 0;

        return (double)guest.TotalInvitationsAccepted / guest.TotalInvitationsSent * 100;
    }
}
