using EventEase.Application.Interfaces;
using EventEase.Domain.Entities;
using HotChocolate.Authorization;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;

namespace EventEase.API.GraphQL;

/// <summary>
/// Root GraphQL Subscription type for real-time updates
/// </summary>
public class Subscription
{
    private readonly ICurrentTenantService _currentTenantService;

    public Subscription(ICurrentTenantService currentTenantService)
    {
        _currentTenantService = currentTenantService;
    }

    // ===== Event Subscriptions =====

    /// <summary>
    /// Subscribe to event creation notifications
    /// </summary>
    [Authorize]
    [Subscribe]
    [Topic("EventCreated_{tenantId}")]
    public Event OnEventCreated(
        [EventMessage] Event evt)
    {
        return evt;
    }

    /// <summary>
    /// Subscribe to event update notifications
    /// </summary>
    [Authorize]
    [Subscribe]
    [Topic("EventUpdated_{tenantId}")]
    public Event OnEventUpdated(
        [EventMessage] Event evt)
    {
        return evt;
    }

    /// <summary>
    /// Subscribe to event deletion notifications
    /// </summary>
    [Authorize]
    [Subscribe]
    [Topic("EventDeleted_{tenantId}")]
    public Guid OnEventDeleted(
        [EventMessage] Guid eventId)
    {
        return eventId;
    }

    // ===== Registration Subscriptions =====

    /// <summary>
    /// Subscribe to registration creation notifications
    /// </summary>
    [Authorize]
    [Subscribe]
    [Topic("RegistrationCreated_{tenantId}")]
    public EventRegistration OnRegistrationCreated(
        [EventMessage] EventRegistration registration)
    {
        return registration;
    }

    /// <summary>
    /// Subscribe to registration update notifications
    /// </summary>
    [Authorize]
    [Subscribe]
    [Topic("RegistrationUpdated_{tenantId}")]
    public EventRegistration OnRegistrationUpdated(
        [EventMessage] EventRegistration registration)
    {
        return registration;
    }

    /// <summary>
    /// Subscribe to registration deletion notifications
    /// </summary>
    [Authorize]
    [Subscribe]
    [Topic("RegistrationDeleted_{tenantId}")]
    public Guid OnRegistrationDeleted(
        [EventMessage] Guid registrationId)
    {
        return registrationId;
    }

    /// <summary>
    /// Subscribe to attendee check-in notifications (for specific event)
    /// </summary>
    [Authorize]
    [Subscribe]
    [Topic("AttendeeCheckedIn_{tenantId}")]
    public EventRegistration OnAttendeeCheckedIn(
        [EventMessage] EventRegistration registration)
    {
        return registration;
    }

    // ===== Event-Specific Subscriptions =====

    /// <summary>
    /// Subscribe to updates for a specific event
    /// Real-time updates for event details, registrations count, etc.
    /// </summary>
    [Authorize]
    [Subscribe]
    public async ValueTask<ISourceStream<EventUpdate>> OnEventUpdate(
        Guid eventId,
        [Service] ITopicEventReceiver eventReceiver,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        return await eventReceiver.SubscribeAsync<EventUpdate>(
            $"EventUpdate_{tenantId}_{eventId}",
            cancellationToken);
    }

    /// <summary>
    /// Subscribe to real-time attendee count updates for an event
    /// </summary>
    [Authorize]
    [Subscribe]
    public async ValueTask<ISourceStream<AttendeeCountUpdate>> OnAttendeeCountUpdate(
        Guid eventId,
        [Service] ITopicEventReceiver eventReceiver,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        return await eventReceiver.SubscribeAsync<AttendeeCountUpdate>(
            $"AttendeeCount_{tenantId}_{eventId}",
            cancellationToken);
    }

    /// <summary>
    /// Subscribe to real-time check-in updates for an event
    /// </summary>
    [Authorize]
    [Subscribe]
    public async ValueTask<ISourceStream<CheckInUpdate>> OnCheckInUpdate(
        Guid eventId,
        [Service] ITopicEventReceiver eventReceiver,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        return await eventReceiver.SubscribeAsync<CheckInUpdate>(
            $"CheckIn_{tenantId}_{eventId}",
            cancellationToken);
    }
}

// ===== Subscription Payload Types =====

/// <summary>
/// Event update notification payload
/// </summary>
public class EventUpdate
{
    public Guid EventId { get; set; }
    public string UpdateType { get; set; } = string.Empty; // "details", "registration", "cancellation"
    public DateTime Timestamp { get; set; }
    public object? Data { get; set; }
}

/// <summary>
/// Attendee count update notification payload
/// </summary>
public class AttendeeCountUpdate
{
    public Guid EventId { get; set; }
    public int TotalRegistrations { get; set; }
    public int ConfirmedCount { get; set; }
    public int CheckedInCount { get; set; }
    public int AvailableSpots { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Check-in update notification payload
/// </summary>
public class CheckInUpdate
{
    public Guid EventId { get; set; }
    public Guid RegistrationId { get; set; }
    public string AttendeeName { get; set; } = string.Empty;
    public DateTime CheckedInAt { get; set; }
}
