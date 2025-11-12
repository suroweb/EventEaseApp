using EventEase.API.GraphQL.DataLoaders;
using EventEase.Domain.Entities;

namespace EventEase.API.GraphQL.Types;

/// <summary>
/// GraphQL ObjectType for EventRegistration entity
/// </summary>
public class RegistrationType : ObjectType<EventRegistration>
{
    protected override void Configure(IObjectTypeDescriptor<EventRegistration> descriptor)
    {
        descriptor.Description("Represents a registration for an event");

        descriptor
            .Field(r => r.Id)
            .Description("Unique identifier for the registration");

        descriptor
            .Field(r => r.TenantId)
            .Description("Tenant that owns this registration");

        descriptor
            .Field(r => r.EventId)
            .Description("Event ID for this registration");

        descriptor
            .Field(r => r.UserId)
            .Description("User ID (null if external guest)");

        descriptor
            .Field(r => r.FirstName)
            .Description("Attendee first name");

        descriptor
            .Field(r => r.LastName)
            .Description("Attendee last name");

        descriptor
            .Field(r => r.Email)
            .Description("Attendee email");

        descriptor
            .Field(r => r.PhoneNumber)
            .Description("Attendee phone number");

        descriptor
            .Field(r => r.Company)
            .Description("Attendee company");

        descriptor
            .Field(r => r.JobTitle)
            .Description("Attendee job title");

        descriptor
            .Field(r => r.Status)
            .Description("Registration status");

        descriptor
            .Field(r => r.NumberOfTickets)
            .Description("Number of tickets purchased");

        descriptor
            .Field(r => r.TotalAmount)
            .Description("Total amount paid");

        descriptor
            .Field(r => r.SpecialRequests)
            .Description("Special requests from attendee");

        descriptor
            .Field(r => r.DietaryRestrictions)
            .Description("Dietary restrictions");

        descriptor
            .Field(r => r.RegisteredAt)
            .Description("When registration was created");

        descriptor
            .Field(r => r.ConfirmedAt)
            .Description("When registration was confirmed");

        descriptor
            .Field(r => r.CancelledAt)
            .Description("When registration was cancelled");

        descriptor
            .Field(r => r.CheckedInAt)
            .Description("When attendee checked in");

        descriptor
            .Field(r => r.RegistrationSource)
            .Description("Source of registration (web, email, invitation, etc.)");

        descriptor
            .Field(r => r.CreatedAt)
            .Description("When the record was created");

        descriptor
            .Field(r => r.UpdatedAt)
            .Description("When the record was last updated");

        // Navigation properties
        descriptor
            .Field(r => r.Tenant)
            .ResolveWith<RegistrationResolvers>(rr => rr.GetTenantAsync(default!, default!, default))
            .Description("Tenant that owns this registration");

        descriptor
            .Field(r => r.Event)
            .ResolveWith<RegistrationResolvers>(rr => rr.GetEventAsync(default!, default!, default))
            .Description("Event for this registration");

        descriptor
            .Field(r => r.User)
            .ResolveWith<RegistrationResolvers>(rr => rr.GetUserAsync(default!, default!, default))
            .Description("User who registered (if applicable)");

        // Computed fields
        descriptor
            .Field("fullName")
            .Type<StringType>()
            .ResolveWith<RegistrationResolvers>(rr => rr.GetFullName(default!))
            .Description("Full name of the attendee");

        descriptor
            .Field("isCheckedIn")
            .Type<BooleanType>()
            .ResolveWith<RegistrationResolvers>(rr => rr.IsCheckedIn(default!))
            .Description("Whether the attendee has checked in");
    }
}

/// <summary>
/// Resolvers for EventRegistration GraphQL type
/// </summary>
public class RegistrationResolvers
{
    public async Task<Tenant> GetTenantAsync(
        [Parent] EventRegistration registration,
        TenantByIdDataLoader tenantLoader,
        CancellationToken cancellationToken)
    {
        return await tenantLoader.LoadAsync(registration.TenantId, cancellationToken);
    }

    public async Task<Event> GetEventAsync(
        [Parent] EventRegistration registration,
        EventByIdDataLoader eventLoader,
        CancellationToken cancellationToken)
    {
        return await eventLoader.LoadAsync(registration.EventId, cancellationToken);
    }

    public async Task<User?> GetUserAsync(
        [Parent] EventRegistration registration,
        UserByIdDataLoader userLoader,
        CancellationToken cancellationToken)
    {
        if (!registration.UserId.HasValue)
            return null;

        return await userLoader.LoadAsync(registration.UserId.Value, cancellationToken);
    }

    public string GetFullName([Parent] EventRegistration registration)
    {
        return $"{registration.FirstName} {registration.LastName}";
    }

    public bool IsCheckedIn([Parent] EventRegistration registration)
    {
        return registration.CheckedInAt.HasValue;
    }
}
