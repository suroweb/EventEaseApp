using EventEase.API.GraphQL.DataLoaders;
using EventEase.Domain.Entities;

namespace EventEase.API.GraphQL.Types;

/// <summary>
/// GraphQL ObjectType for Event entity
/// </summary>
public class EventType : ObjectType<Event>
{
    protected override void Configure(IObjectTypeDescriptor<Event> descriptor)
    {
        descriptor.Description("Represents an event created by a tenant");

        descriptor
            .Field(e => e.Id)
            .Description("Unique identifier for the event");

        descriptor
            .Field(e => e.TenantId)
            .Description("Tenant that owns this event");

        descriptor
            .Field(e => e.CreatedByUserId)
            .Description("User who created this event");

        descriptor
            .Field(e => e.Name)
            .Description("Event name");

        descriptor
            .Field(e => e.Description)
            .Description("Event description");

        descriptor
            .Field(e => e.Category)
            .Description("Event category");

        descriptor
            .Field(e => e.Status)
            .Description("Current status of the event");

        descriptor
            .Field(e => e.StartDate)
            .Description("Event start date and time");

        descriptor
            .Field(e => e.EndDate)
            .Description("Event end date and time");

        descriptor
            .Field(e => e.TimeZone)
            .Description("Timezone for the event");

        descriptor
            .Field(e => e.Location)
            .Description("Event location description");

        descriptor
            .Field(e => e.Venue)
            .Description("Event venue name");

        descriptor
            .Field(e => e.VenueAddress)
            .Description("Venue address");

        descriptor
            .Field(e => e.VenueLatitude)
            .Description("Venue latitude coordinate");

        descriptor
            .Field(e => e.VenueLongitude)
            .Description("Venue longitude coordinate");

        descriptor
            .Field(e => e.IsVirtual)
            .Description("Whether this is a virtual event");

        descriptor
            .Field(e => e.VirtualMeetingUrl)
            .Description("Virtual meeting URL for online events");

        descriptor
            .Field(e => e.MaxAttendees)
            .Description("Maximum number of attendees allowed");

        descriptor
            .Field(e => e.Price)
            .Description("Event ticket price");

        descriptor
            .Field(e => e.Currency)
            .Description("Currency for pricing");

        descriptor
            .Field(e => e.IsFree)
            .Description("Whether the event is free");

        descriptor
            .Field(e => e.ImageUrl)
            .Description("Event image URL");

        descriptor
            .Field(e => e.BannerUrl)
            .Description("Event banner URL");

        descriptor
            .Field(e => e.RequiresApproval)
            .Description("Whether registrations require approval");

        descriptor
            .Field(e => e.RegistrationOpensAt)
            .Description("When registration opens");

        descriptor
            .Field(e => e.RegistrationClosesAt)
            .Description("When registration closes");

        descriptor
            .Field(e => e.AllowWaitlist)
            .Description("Whether to allow waitlist");

        descriptor
            .Field(e => e.CreatedAt)
            .Description("When the event was created");

        descriptor
            .Field(e => e.UpdatedAt)
            .Description("When the event was last updated");

        // Navigation properties with DataLoaders
        descriptor
            .Field(e => e.Tenant)
            .ResolveWith<EventResolvers>(r => r.GetTenantAsync(default!, default!, default))
            .Description("Tenant that owns this event");

        descriptor
            .Field(e => e.CreatedByUser)
            .ResolveWith<EventResolvers>(r => r.GetCreatedByUserAsync(default!, default!, default))
            .Description("User who created this event");

        descriptor
            .Field(e => e.Registrations)
            .ResolveWith<EventResolvers>(r => r.GetRegistrationsAsync(default!, default!, default))
            .Description("All registrations for this event");

        // Computed fields
        descriptor
            .Field("registrationCount")
            .Type<IntType>()
            .ResolveWith<EventResolvers>(r => r.GetRegistrationCountAsync(default!, default!, default))
            .Description("Total number of registrations");

        descriptor
            .Field("availableSpots")
            .Type<IntType>()
            .ResolveWith<EventResolvers>(r => r.GetAvailableSpotsAsync(default!, default!, default))
            .Description("Number of available spots");
    }
}

/// <summary>
/// Resolvers for Event GraphQL type
/// </summary>
public class EventResolvers
{
    public async Task<Tenant> GetTenantAsync(
        [Parent] Event evt,
        TenantByIdDataLoader tenantLoader,
        CancellationToken cancellationToken)
    {
        return await tenantLoader.LoadAsync(evt.TenantId, cancellationToken);
    }

    public async Task<User> GetCreatedByUserAsync(
        [Parent] Event evt,
        UserByIdDataLoader userLoader,
        CancellationToken cancellationToken)
    {
        return await userLoader.LoadAsync(evt.CreatedByUserId, cancellationToken);
    }

    public async Task<IEnumerable<EventRegistration>> GetRegistrationsAsync(
        [Parent] Event evt,
        RegistrationsByEventIdDataLoader registrationsLoader,
        CancellationToken cancellationToken)
    {
        return await registrationsLoader.LoadAsync(evt.Id, cancellationToken);
    }

    public async Task<int> GetRegistrationCountAsync(
        [Parent] Event evt,
        RegistrationsByEventIdDataLoader registrationsLoader,
        CancellationToken cancellationToken)
    {
        var registrations = await registrationsLoader.LoadAsync(evt.Id, cancellationToken);
        return registrations.Count();
    }

    public async Task<int?> GetAvailableSpotsAsync(
        [Parent] Event evt,
        RegistrationsByEventIdDataLoader registrationsLoader,
        CancellationToken cancellationToken)
    {
        if (!evt.MaxAttendees.HasValue)
            return null;

        var registrations = await registrationsLoader.LoadAsync(evt.Id, cancellationToken);
        var confirmedCount = registrations.Count(r => r.Status == Domain.Enums.RegistrationStatus.Confirmed);

        return Math.Max(0, evt.MaxAttendees.Value - confirmedCount);
    }
}
