using EventEase.Application.Interfaces;
using EventEase.Domain.Entities;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using HotChocolate.Authorization;
using HotChocolate.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace EventEase.API.GraphQL;

/// <summary>
/// Root GraphQL Mutation type
/// </summary>
public class Mutation
{
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ICurrentUserService _currentUserService;

    public Mutation(
        ICurrentTenantService currentTenantService,
        ICurrentUserService currentUserService)
    {
        _currentTenantService = currentTenantService;
        _currentUserService = currentUserService;
    }

    // ===== Event Mutations =====

    /// <summary>
    /// Create a new event
    /// </summary>
    [Authorize(Policy = "EventManagerOrAbove")]
    public async Task<Event> CreateEvent(
        CreateEventInput input,
        ApplicationDbContext context,
        [Service] ITopicEventSender eventSender,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        var userId = _currentUserService.UserId!.Value;

        var evt = new Event
        {
            TenantId = tenantId,
            CreatedByUserId = userId,
            Name = input.Name,
            Description = input.Description,
            Category = input.Category,
            Status = EventStatus.Draft,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            TimeZone = input.TimeZone,
            Location = input.Location,
            Venue = input.Venue,
            VenueAddress = input.VenueAddress,
            VenueCity = input.VenueCity,
            VenueCountry = input.VenueCountry,
            VenueLatitude = input.VenueLatitude,
            VenueLongitude = input.VenueLongitude,
            IsVirtual = input.IsVirtual,
            VirtualMeetingUrl = input.VirtualMeetingUrl,
            MaxAttendees = input.MaxAttendees,
            Price = input.Price,
            Currency = input.Currency ?? "EUR",
            IsFree = input.IsFree,
            ImageUrl = input.ImageUrl,
            BannerUrl = input.BannerUrl,
            RequiresApproval = input.RequiresApproval,
            RegistrationOpensAt = input.RegistrationOpensAt,
            RegistrationClosesAt = input.RegistrationClosesAt,
            AllowWaitlist = input.AllowWaitlist
        };

        context.Events.Add(evt);
        await context.SaveChangesAsync(cancellationToken);

        // Send real-time update
        await eventSender.SendAsync($"EventCreated_{tenantId}", evt, cancellationToken);

        return evt;
    }

    /// <summary>
    /// Update an existing event
    /// </summary>
    [Authorize(Policy = "EventManagerOrAbove")]
    public async Task<Event> UpdateEvent(
        Guid id,
        UpdateEventInput input,
        ApplicationDbContext context,
        [Service] ITopicEventSender eventSender,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;

        var evt = await context.Events
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, cancellationToken);

        if (evt == null)
            throw new GraphQLException("Event not found");

        if (input.Name != null) evt.Name = input.Name;
        if (input.Description != null) evt.Description = input.Description;
        if (input.Category != null) evt.Category = input.Category;
        if (input.Status.HasValue) evt.Status = input.Status.Value;
        if (input.StartDate.HasValue) evt.StartDate = input.StartDate.Value;
        if (input.EndDate.HasValue) evt.EndDate = input.EndDate.Value;
        if (input.TimeZone != null) evt.TimeZone = input.TimeZone;
        if (input.Location != null) evt.Location = input.Location;
        if (input.Venue != null) evt.Venue = input.Venue;
        if (input.VenueAddress != null) evt.VenueAddress = input.VenueAddress;
        if (input.VenueCity != null) evt.VenueCity = input.VenueCity;
        if (input.VenueCountry != null) evt.VenueCountry = input.VenueCountry;
        if (input.VenueLatitude.HasValue) evt.VenueLatitude = input.VenueLatitude;
        if (input.VenueLongitude.HasValue) evt.VenueLongitude = input.VenueLongitude;
        if (input.IsVirtual.HasValue) evt.IsVirtual = input.IsVirtual.Value;
        if (input.VirtualMeetingUrl != null) evt.VirtualMeetingUrl = input.VirtualMeetingUrl;
        if (input.MaxAttendees.HasValue) evt.MaxAttendees = input.MaxAttendees;
        if (input.Price.HasValue) evt.Price = input.Price;
        if (input.Currency != null) evt.Currency = input.Currency;
        if (input.IsFree.HasValue) evt.IsFree = input.IsFree.Value;
        if (input.ImageUrl != null) evt.ImageUrl = input.ImageUrl;
        if (input.BannerUrl != null) evt.BannerUrl = input.BannerUrl;
        if (input.RequiresApproval.HasValue) evt.RequiresApproval = input.RequiresApproval.Value;
        if (input.RegistrationOpensAt.HasValue) evt.RegistrationOpensAt = input.RegistrationOpensAt;
        if (input.RegistrationClosesAt.HasValue) evt.RegistrationClosesAt = input.RegistrationClosesAt;
        if (input.AllowWaitlist.HasValue) evt.AllowWaitlist = input.AllowWaitlist.Value;

        evt.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        // Send real-time update
        await eventSender.SendAsync($"EventUpdated_{tenantId}", evt, cancellationToken);

        return evt;
    }

    /// <summary>
    /// Delete an event
    /// </summary>
    [Authorize(Policy = "EventManagerOrAbove")]
    public async Task<bool> DeleteEvent(
        Guid id,
        ApplicationDbContext context,
        [Service] ITopicEventSender eventSender,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;

        var evt = await context.Events
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, cancellationToken);

        if (evt == null)
            return false;

        context.Events.Remove(evt);
        await context.SaveChangesAsync(cancellationToken);

        // Send real-time update
        await eventSender.SendAsync($"EventDeleted_{tenantId}", evt.Id, cancellationToken);

        return true;
    }

    // ===== Guest Mutations =====

    /// <summary>
    /// Create a new guest
    /// </summary>
    [Authorize]
    public async Task<Guest> CreateGuest(
        CreateGuestInput input,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;

        var guest = new Guest
        {
            TenantId = tenantId,
            Email = input.Email,
            FirstName = input.FirstName,
            LastName = input.LastName,
            PhoneNumber = input.PhoneNumber,
            Company = input.Company,
            JobTitle = input.JobTitle,
            Industry = input.Industry,
            Department = input.Department,
            City = input.City,
            Country = input.Country,
            PreferredLanguage = input.PreferredLanguage,
            TimeZone = input.TimeZone,
            Source = "manual"
        };

        context.Guests.Add(guest);
        await context.SaveChangesAsync(cancellationToken);

        return guest;
    }

    /// <summary>
    /// Update an existing guest
    /// </summary>
    [Authorize]
    public async Task<Guest> UpdateGuest(
        Guid id,
        UpdateGuestInput input,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;

        var guest = await context.Guests
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId, cancellationToken);

        if (guest == null)
            throw new GraphQLException("Guest not found");

        if (input.Email != null) guest.Email = input.Email;
        if (input.FirstName != null) guest.FirstName = input.FirstName;
        if (input.LastName != null) guest.LastName = input.LastName;
        if (input.PhoneNumber != null) guest.PhoneNumber = input.PhoneNumber;
        if (input.Company != null) guest.Company = input.Company;
        if (input.JobTitle != null) guest.JobTitle = input.JobTitle;
        if (input.Industry != null) guest.Industry = input.Industry;
        if (input.Department != null) guest.Department = input.Department;
        if (input.City != null) guest.City = input.City;
        if (input.Country != null) guest.Country = input.Country;
        if (input.PreferredLanguage != null) guest.PreferredLanguage = input.PreferredLanguage;
        if (input.TimeZone != null) guest.TimeZone = input.TimeZone;

        guest.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return guest;
    }

    /// <summary>
    /// Delete a guest
    /// </summary>
    [Authorize]
    public async Task<bool> DeleteGuest(
        Guid id,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;

        var guest = await context.Guests
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId, cancellationToken);

        if (guest == null)
            return false;

        context.Guests.Remove(guest);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    // ===== Registration Mutations =====

    /// <summary>
    /// Create a new registration
    /// </summary>
    [Authorize]
    public async Task<EventRegistration> CreateRegistration(
        CreateRegistrationInput input,
        ApplicationDbContext context,
        [Service] ITopicEventSender eventSender,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        var userId = _currentUserService.UserId;

        // Check if event exists
        var evt = await context.Events
            .FirstOrDefaultAsync(e => e.Id == input.EventId && e.TenantId == tenantId, cancellationToken);

        if (evt == null)
            throw new GraphQLException("Event not found");

        // Check capacity
        if (evt.MaxAttendees.HasValue)
        {
            var confirmedCount = await context.EventRegistrations
                .CountAsync(r => r.EventId == input.EventId && r.Status == RegistrationStatus.Confirmed, cancellationToken);

            if (confirmedCount >= evt.MaxAttendees.Value)
            {
                if (evt.AllowWaitlist)
                {
                    // Add to waitlist
                    var registration = CreateRegistrationEntity(input, tenantId, userId);
                    registration.Status = RegistrationStatus.Waitlisted;
                    context.EventRegistrations.Add(registration);
                    await context.SaveChangesAsync(cancellationToken);
                    await eventSender.SendAsync($"RegistrationCreated_{tenantId}", registration, cancellationToken);
                    return registration;
                }
                else
                {
                    throw new GraphQLException("Event is full and waitlist is not enabled");
                }
            }
        }

        var newRegistration = CreateRegistrationEntity(input, tenantId, userId);
        newRegistration.Status = evt.RequiresApproval ? RegistrationStatus.Pending : RegistrationStatus.Confirmed;

        context.EventRegistrations.Add(newRegistration);
        await context.SaveChangesAsync(cancellationToken);

        // Send real-time update
        await eventSender.SendAsync($"RegistrationCreated_{tenantId}", newRegistration, cancellationToken);

        return newRegistration;
    }

    /// <summary>
    /// Update registration status
    /// </summary>
    [Authorize]
    public async Task<EventRegistration> UpdateRegistrationStatus(
        Guid id,
        RegistrationStatus status,
        ApplicationDbContext context,
        [Service] ITopicEventSender eventSender,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;

        var registration = await context.EventRegistrations
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, cancellationToken);

        if (registration == null)
            throw new GraphQLException("Registration not found");

        registration.Status = status;

        if (status == RegistrationStatus.Confirmed)
            registration.ConfirmedAt = DateTime.UtcNow;
        else if (status == RegistrationStatus.Cancelled)
            registration.CancelledAt = DateTime.UtcNow;
        else if (status == RegistrationStatus.CheckedIn)
            registration.CheckedInAt = DateTime.UtcNow;

        registration.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        // Send real-time update
        await eventSender.SendAsync($"RegistrationUpdated_{tenantId}", registration, cancellationToken);

        return registration;
    }

    /// <summary>
    /// Check-in attendee (mobile app use case)
    /// </summary>
    [Authorize]
    public async Task<EventRegistration> CheckInAttendee(
        Guid registrationId,
        ApplicationDbContext context,
        [Service] ITopicEventSender eventSender,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;

        var registration = await context.EventRegistrations
            .FirstOrDefaultAsync(r => r.Id == registrationId && r.TenantId == tenantId, cancellationToken);

        if (registration == null)
            throw new GraphQLException("Registration not found");

        if (registration.Status != RegistrationStatus.Confirmed)
            throw new GraphQLException("Only confirmed registrations can be checked in");

        registration.Status = RegistrationStatus.CheckedIn;
        registration.CheckedInAt = DateTime.UtcNow;
        registration.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        // Send real-time update
        await eventSender.SendAsync($"AttendeeCheckedIn_{tenantId}", registration, cancellationToken);

        return registration;
    }

    /// <summary>
    /// Delete a registration
    /// </summary>
    [Authorize]
    public async Task<bool> DeleteRegistration(
        Guid id,
        ApplicationDbContext context,
        [Service] ITopicEventSender eventSender,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;

        var registration = await context.EventRegistrations
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, cancellationToken);

        if (registration == null)
            return false;

        context.EventRegistrations.Remove(registration);
        await context.SaveChangesAsync(cancellationToken);

        // Send real-time update
        await eventSender.SendAsync($"RegistrationDeleted_{tenantId}", registration.Id, cancellationToken);

        return true;
    }

    // Helper Methods

    private EventRegistration CreateRegistrationEntity(CreateRegistrationInput input, Guid tenantId, Guid? userId)
    {
        return new EventRegistration
        {
            TenantId = tenantId,
            EventId = input.EventId,
            UserId = userId,
            FirstName = input.FirstName,
            LastName = input.LastName,
            Email = input.Email,
            PhoneNumber = input.PhoneNumber,
            Company = input.Company,
            JobTitle = input.JobTitle,
            NumberOfTickets = input.NumberOfTickets,
            TotalAmount = input.TotalAmount,
            SpecialRequests = input.SpecialRequests,
            DietaryRestrictions = input.DietaryRestrictions,
            RegistrationSource = input.RegistrationSource ?? "graphql_mobile",
            RegisteredAt = DateTime.UtcNow
        };
    }
}

// ===== Input Types =====

public record CreateEventInput(
    string Name,
    string? Description,
    string? Category,
    DateTime StartDate,
    DateTime EndDate,
    string? TimeZone,
    string? Location,
    string? Venue,
    string? VenueAddress,
    string? VenueCity,
    string? VenueCountry,
    decimal? VenueLatitude,
    decimal? VenueLongitude,
    bool IsVirtual,
    string? VirtualMeetingUrl,
    int? MaxAttendees,
    decimal? Price,
    string? Currency,
    bool IsFree,
    string? ImageUrl,
    string? BannerUrl,
    bool RequiresApproval,
    DateTime? RegistrationOpensAt,
    DateTime? RegistrationClosesAt,
    bool AllowWaitlist
);

public record UpdateEventInput(
    string? Name,
    string? Description,
    string? Category,
    EventStatus? Status,
    DateTime? StartDate,
    DateTime? EndDate,
    string? TimeZone,
    string? Location,
    string? Venue,
    string? VenueAddress,
    string? VenueCity,
    string? VenueCountry,
    decimal? VenueLatitude,
    decimal? VenueLongitude,
    bool? IsVirtual,
    string? VirtualMeetingUrl,
    int? MaxAttendees,
    decimal? Price,
    string? Currency,
    bool? IsFree,
    string? ImageUrl,
    string? BannerUrl,
    bool? RequiresApproval,
    DateTime? RegistrationOpensAt,
    DateTime? RegistrationClosesAt,
    bool? AllowWaitlist
);

public record CreateGuestInput(
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Company,
    string? JobTitle,
    string? Industry,
    string? Department,
    string? City,
    string? Country,
    string? PreferredLanguage,
    string? TimeZone
);

public record UpdateGuestInput(
    string? Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Company,
    string? JobTitle,
    string? Industry,
    string? Department,
    string? City,
    string? Country,
    string? PreferredLanguage,
    string? TimeZone
);

public record CreateRegistrationInput(
    Guid EventId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? Company,
    string? JobTitle,
    int NumberOfTickets,
    decimal TotalAmount,
    string? SpecialRequests,
    string? DietaryRestrictions,
    string? RegistrationSource
);
