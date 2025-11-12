using EventEase.Application.Interfaces;
using EventEase.Domain.Entities;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EventEase.API.GraphQL;

/// <summary>
/// Root GraphQL Query type
/// </summary>
public class Query
{
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ICurrentUserService _currentUserService;

    public Query(
        ICurrentTenantService currentTenantService,
        ICurrentUserService currentUserService)
    {
        _currentTenantService = currentTenantService;
        _currentUserService = currentUserService;
    }

    // ===== Event Queries =====

    /// <summary>
    /// Get all events for the current tenant
    /// </summary>
    [Authorize]
    [UseFiltering]
    [UseSorting]
    [UsePaging]
    public IQueryable<Event> GetEvents(ApplicationDbContext context)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        return context.Events.Where(e => e.TenantId == tenantId);
    }

    /// <summary>
    /// Get a specific event by ID
    /// </summary>
    [Authorize]
    public async Task<Event?> GetEventById(
        Guid id,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        return await context.Events
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, cancellationToken);
    }

    /// <summary>
    /// Get upcoming events
    /// </summary>
    [Authorize]
    [UseFiltering]
    [UseSorting]
    [UsePaging]
    public IQueryable<Event> GetUpcomingEvents(ApplicationDbContext context)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        var now = DateTime.UtcNow;

        return context.Events
            .Where(e => e.TenantId == tenantId && e.StartDate > now && e.Status == EventStatus.Published)
            .OrderBy(e => e.StartDate);
    }

    /// <summary>
    /// Get past events
    /// </summary>
    [Authorize]
    [UseFiltering]
    [UseSorting]
    [UsePaging]
    public IQueryable<Event> GetPastEvents(ApplicationDbContext context)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        var now = DateTime.UtcNow;

        return context.Events
            .Where(e => e.TenantId == tenantId && e.EndDate < now)
            .OrderByDescending(e => e.StartDate);
    }

    /// <summary>
    /// Get published events (public endpoint for mobile app)
    /// </summary>
    [UseFiltering]
    [UseSorting]
    [UsePaging]
    public IQueryable<Event> GetPublishedEvents(ApplicationDbContext context)
    {
        return context.Events.Where(e => e.Status == EventStatus.Published);
    }

    // ===== Guest Queries =====

    /// <summary>
    /// Get all guests for the current tenant
    /// </summary>
    [Authorize]
    [UseFiltering]
    [UseSorting]
    [UsePaging]
    public IQueryable<Guest> GetGuests(ApplicationDbContext context)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        return context.Guests.Where(g => g.TenantId == tenantId);
    }

    /// <summary>
    /// Get a specific guest by ID
    /// </summary>
    [Authorize]
    public async Task<Guest?> GetGuestById(
        Guid id,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        return await context.Guests
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId, cancellationToken);
    }

    /// <summary>
    /// Search guests by email or name
    /// </summary>
    [Authorize]
    [UsePaging]
    public IQueryable<Guest> SearchGuests(
        string searchTerm,
        ApplicationDbContext context)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        var lowerSearchTerm = searchTerm.ToLower();

        return context.Guests
            .Where(g => g.TenantId == tenantId &&
                (g.Email.ToLower().Contains(lowerSearchTerm) ||
                 (g.FirstName != null && g.FirstName.ToLower().Contains(lowerSearchTerm)) ||
                 (g.LastName != null && g.LastName.ToLower().Contains(lowerSearchTerm)) ||
                 (g.Company != null && g.Company.ToLower().Contains(lowerSearchTerm))));
    }

    // ===== Registration Queries =====

    /// <summary>
    /// Get all registrations for the current tenant
    /// </summary>
    [Authorize]
    [UseFiltering]
    [UseSorting]
    [UsePaging]
    public IQueryable<EventRegistration> GetRegistrations(ApplicationDbContext context)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        return context.EventRegistrations.Where(r => r.TenantId == tenantId);
    }

    /// <summary>
    /// Get a specific registration by ID
    /// </summary>
    [Authorize]
    public async Task<EventRegistration?> GetRegistrationById(
        Guid id,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        return await context.EventRegistrations
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, cancellationToken);
    }

    /// <summary>
    /// Get registrations for a specific event
    /// </summary>
    [Authorize]
    [UseFiltering]
    [UseSorting]
    [UsePaging]
    public IQueryable<EventRegistration> GetRegistrationsByEvent(
        Guid eventId,
        ApplicationDbContext context)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        return context.EventRegistrations
            .Where(r => r.EventId == eventId && r.TenantId == tenantId);
    }

    /// <summary>
    /// Get registrations for the current user
    /// </summary>
    [Authorize]
    [UseFiltering]
    [UseSorting]
    [UsePaging]
    public IQueryable<EventRegistration> GetMyRegistrations(ApplicationDbContext context)
    {
        var userId = _currentUserService.UserId!.Value;
        return context.EventRegistrations.Where(r => r.UserId == userId);
    }

    // ===== User Queries =====

    /// <summary>
    /// Get all users in the current tenant
    /// </summary>
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [UseFiltering]
    [UseSorting]
    [UsePaging]
    public IQueryable<User> GetUsers(ApplicationDbContext context)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        return context.Users.Where(u => u.TenantId == tenantId);
    }

    /// <summary>
    /// Get a specific user by ID
    /// </summary>
    [Authorize]
    public async Task<User?> GetUserById(
        Guid id,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        return await context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId, cancellationToken);
    }

    /// <summary>
    /// Get current authenticated user
    /// </summary>
    [Authorize]
    public async Task<User?> GetMe(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!.Value;
        return await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    // ===== Tenant Queries =====

    /// <summary>
    /// Get the current tenant
    /// </summary>
    [Authorize]
    public async Task<Tenant?> GetMyTenant(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        return await context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
    }

    // ===== Statistics & Analytics =====

    /// <summary>
    /// Get event statistics
    /// </summary>
    [Authorize]
    public async Task<EventStats> GetEventStats(
        Guid eventId,
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        var evt = await context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId && e.TenantId == tenantId, cancellationToken);

        if (evt == null)
            throw new GraphQLException("Event not found");

        var registrations = await context.EventRegistrations
            .Where(r => r.EventId == eventId)
            .ToListAsync(cancellationToken);

        return new EventStats
        {
            EventId = eventId,
            TotalRegistrations = registrations.Count,
            ConfirmedCount = registrations.Count(r => r.Status == RegistrationStatus.Confirmed),
            PendingCount = registrations.Count(r => r.Status == RegistrationStatus.Pending),
            CancelledCount = registrations.Count(r => r.Status == RegistrationStatus.Cancelled),
            CheckedInCount = registrations.Count(r => r.Status == RegistrationStatus.CheckedIn),
            WaitlistedCount = registrations.Count(r => r.Status == RegistrationStatus.Waitlisted),
            TotalRevenue = registrations.Where(r => r.Status != RegistrationStatus.Cancelled).Sum(r => r.TotalAmount),
            AvailableSpots = evt.MaxAttendees.HasValue
                ? Math.Max(0, evt.MaxAttendees.Value - registrations.Count(r => r.Status == RegistrationStatus.Confirmed))
                : null
        };
    }

    /// <summary>
    /// Get tenant statistics
    /// </summary>
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    public async Task<TenantStats> GetTenantStats(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId!.Value;

        var totalEvents = await context.Events.CountAsync(e => e.TenantId == tenantId, cancellationToken);
        var totalGuests = await context.Guests.CountAsync(g => g.TenantId == tenantId, cancellationToken);
        var totalRegistrations = await context.EventRegistrations.CountAsync(r => r.TenantId == tenantId, cancellationToken);
        var totalUsers = await context.Users.CountAsync(u => u.TenantId == tenantId, cancellationToken);

        var upcomingEvents = await context.Events
            .CountAsync(e => e.TenantId == tenantId && e.StartDate > DateTime.UtcNow, cancellationToken);

        return new TenantStats
        {
            TotalEvents = totalEvents,
            TotalGuests = totalGuests,
            TotalRegistrations = totalRegistrations,
            TotalUsers = totalUsers,
            UpcomingEvents = upcomingEvents
        };
    }
}

/// <summary>
/// Event statistics
/// </summary>
public class EventStats
{
    public Guid EventId { get; set; }
    public int TotalRegistrations { get; set; }
    public int ConfirmedCount { get; set; }
    public int PendingCount { get; set; }
    public int CancelledCount { get; set; }
    public int CheckedInCount { get; set; }
    public int WaitlistedCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public int? AvailableSpots { get; set; }
}

/// <summary>
/// Tenant statistics
/// </summary>
public class TenantStats
{
    public int TotalEvents { get; set; }
    public int TotalGuests { get; set; }
    public int TotalRegistrations { get; set; }
    public int TotalUsers { get; set; }
    public int UpcomingEvents { get; set; }
}
