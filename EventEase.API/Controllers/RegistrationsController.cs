using EventEase.API.DTOs;
using EventEase.Application.Interfaces;
using EventEase.Domain.Entities;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EventEase.API.Controllers;

/// <summary>
/// Event registration management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class RegistrationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ILogger<RegistrationsController> _logger;

    public RegistrationsController(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICurrentTenantService currentTenantService,
        ILogger<RegistrationsController> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _currentTenantService = currentTenantService;
        _logger = logger;
    }

    /// <summary>
    /// Register for an event
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <response code="201">Registration successful</response>
    /// <response code="400">Event is full or registration closed</response>
    /// <response code="404">Event not found</response>
    [HttpPost]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegistrationResponse>> RegisterForEvent([FromBody] CreateRegistrationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Get event
        var eventEntity = await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == request.EventId);

        if (eventEntity == null)
        {
            return NotFound(new { Error = "Event not found" });
        }

        // Check if event is published
        if (eventEntity.Status != EventStatus.Published)
        {
            return BadRequest(new { Error = "Event is not open for registration" });
        }

        // Check registration window
        if (eventEntity.RegistrationOpensAt.HasValue && eventEntity.RegistrationOpensAt.Value > DateTime.UtcNow)
        {
            return BadRequest(new { Error = "Registration is not yet open" });
        }

        if (eventEntity.RegistrationClosesAt.HasValue && eventEntity.RegistrationClosesAt.Value < DateTime.UtcNow)
        {
            return BadRequest(new { Error = "Registration is closed" });
        }

        // Check for duplicate registration
        var existingRegistration = await _context.EventRegistrations
            .FirstOrDefaultAsync(r =>
                r.EventId == request.EventId &&
                r.Email == request.Email &&
                r.Status != RegistrationStatus.Cancelled);

        if (existingRegistration != null)
        {
            return BadRequest(new { Error = "You are already registered for this event" });
        }

        // Check capacity
        var confirmedCount = eventEntity.Registrations
            .Count(r => r.Status == RegistrationStatus.Confirmed || r.Status == RegistrationStatus.CheckedIn);

        var isWaitlisted = false;

        if (eventEntity.MaxAttendees.HasValue)
        {
            var availableSeats = eventEntity.MaxAttendees.Value - confirmedCount;

            if (availableSeats < request.NumberOfTickets)
            {
                if (!eventEntity.AllowWaitlist)
                {
                    return BadRequest(new { Error = "Event is full and waitlist is not available" });
                }

                isWaitlisted = true;
            }
        }

        // Calculate total amount
        var totalAmount = eventEntity.IsFree ? 0 : (eventEntity.Price ?? 0) * request.NumberOfTickets;

        // Create registration
        var registration = new EventRegistration
        {
            TenantId = _currentTenantService.TenantId,
            EventId = request.EventId,
            UserId = _currentUserService.IsAuthenticated ? _currentUserService.UserId : null,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Company = request.Company,
            JobTitle = request.JobTitle,
            Status = isWaitlisted ? RegistrationStatus.Waitlisted :
                    (eventEntity.RequiresApproval ? RegistrationStatus.Pending : RegistrationStatus.Confirmed),
            NumberOfTickets = request.NumberOfTickets,
            TotalAmount = totalAmount,
            SpecialRequests = request.SpecialRequests,
            DietaryRestrictions = request.DietaryRestrictions,
            RegisteredAt = DateTime.UtcNow,
            ConfirmedAt = (!eventEntity.RequiresApproval && !isWaitlisted) ? DateTime.UtcNow : null,
            RegistrationSource = "web",
            CustomData = request.CustomData != null ? JsonSerializer.Serialize(request.CustomData) : null
        };

        _context.EventRegistrations.Add(registration);
        await _context.SaveChangesAsync();

        // Load event
        await _context.Entry(registration).Reference(r => r.Event).LoadAsync();

        _logger.LogInformation("New registration: {Email} registered for event {EventId} (Status: {Status})",
            registration.Email, request.EventId, registration.Status);

        // TODO: Send confirmation email

        var response = MapToRegistrationResponse(registration);

        return CreatedAtAction(nameof(GetRegistration), new { id = registration.Id }, response);
    }

    /// <summary>
    /// Get registration by ID
    /// </summary>
    /// <param name="id">Registration ID</param>
    /// <response code="200">Returns registration details</response>
    /// <response code="404">Registration not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegistrationResponse>> GetRegistration(Guid id)
    {
        var registration = await _context.EventRegistrations
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (registration == null)
        {
            return NotFound(new { Error = "Registration not found" });
        }

        return Ok(MapToRegistrationResponse(registration));
    }

    /// <summary>
    /// Get current user's registrations
    /// </summary>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <response code="200">Returns user's registrations</response>
    [HttpGet("my-registrations")]
    [ProducesResponseType(typeof(PagedResult<RegistrationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RegistrationResponse>>> GetMyRegistrations(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.EventRegistrations
            .Include(r => r.Event)
            .Where(r => r.Email == _currentUserService.Email)
            .OrderByDescending(r => r.RegisteredAt);

        var totalCount = await query.CountAsync();

        var registrations = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => MapToRegistrationResponse(r))
            .ToListAsync();

        return Ok(new PagedResult<RegistrationResponse>
        {
            Items = registrations,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Get registrations for a specific event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="status">Filter by status</param>
    /// <response code="200">Returns event registrations</response>
    /// <response code="404">Event not found</response>
    [HttpGet("event/{eventId}")]
    [Authorize(Policy = "EventManagerOrAbove")]
    [ProducesResponseType(typeof(PagedResult<RegistrationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<RegistrationResponse>>> GetEventRegistrations(
        Guid eventId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] RegistrationStatus? status = null)
    {
        var eventEntity = await _context.Events.FindAsync(eventId);

        if (eventEntity == null)
        {
            return NotFound(new { Error = "Event not found" });
        }

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.EventRegistrations
            .Include(r => r.Event)
            .Where(r => r.EventId == eventId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        query = query.OrderBy(r => r.RegisteredAt);

        var totalCount = await query.CountAsync();

        var registrations = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => MapToRegistrationResponse(r))
            .ToListAsync();

        return Ok(new PagedResult<RegistrationResponse>
        {
            Items = registrations,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Cancel a registration
    /// </summary>
    /// <param name="id">Registration ID</param>
    /// <response code="200">Registration cancelled</response>
    /// <response code="404">Registration not found</response>
    /// <response code="400">Registration cannot be cancelled</response>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegistrationResponse>> CancelRegistration(Guid id)
    {
        var registration = await _context.EventRegistrations
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (registration == null)
        {
            return NotFound(new { Error = "Registration not found" });
        }

        // Only allow user to cancel their own registration or EventManager+
        var isEventManager = User.IsInRole("EventManager") ||
                            User.IsInRole("TenantAdmin") ||
                            User.IsInRole("TenantOwner") ||
                            User.IsInRole("SystemAdmin");

        if (registration.Email != _currentUserService.Email && !isEventManager)
        {
            return Forbid();
        }

        if (registration.Status == RegistrationStatus.Cancelled)
        {
            return BadRequest(new { Error = "Registration is already cancelled" });
        }

        if (registration.Status == RegistrationStatus.CheckedIn)
        {
            return BadRequest(new { Error = "Cannot cancel registration after check-in" });
        }

        registration.Status = RegistrationStatus.Cancelled;
        registration.CancelledAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Registration cancelled: {RegistrationId} for event {EventId}",
            id, registration.EventId);

        // TODO: Send cancellation confirmation email

        return Ok(MapToRegistrationResponse(registration));
    }

    /// <summary>
    /// Check-in an attendee at the event
    /// </summary>
    /// <param name="id">Registration ID</param>
    /// <response code="200">Check-in successful</response>
    /// <response code="404">Registration not found</response>
    /// <response code="400">Registration cannot be checked in</response>
    [HttpPost("{id}/check-in")]
    [Authorize(Policy = "EventManagerOrAbove")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegistrationResponse>> CheckIn(Guid id)
    {
        var registration = await _context.EventRegistrations
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (registration == null)
        {
            return NotFound(new { Error = "Registration not found" });
        }

        if (registration.Status == RegistrationStatus.Cancelled)
        {
            return BadRequest(new { Error = "Cannot check-in cancelled registration" });
        }

        if (registration.Status == RegistrationStatus.CheckedIn)
        {
            return BadRequest(new { Error = "Attendee is already checked in" });
        }

        registration.Status = RegistrationStatus.CheckedIn;
        registration.CheckedInAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Attendee checked in: {Email} for event {EventId}",
            registration.Email, registration.EventId);

        return Ok(MapToRegistrationResponse(registration));
    }

    private static RegistrationResponse MapToRegistrationResponse(EventRegistration registration)
    {
        return new RegistrationResponse
        {
            Id = registration.Id,
            TenantId = registration.TenantId,
            EventId = registration.EventId,
            EventName = registration.Event?.Name ?? string.Empty,
            UserId = registration.UserId,
            FirstName = registration.FirstName,
            LastName = registration.LastName,
            Email = registration.Email,
            PhoneNumber = registration.PhoneNumber,
            Company = registration.Company,
            JobTitle = registration.JobTitle,
            Status = registration.Status.ToString(),
            NumberOfTickets = registration.NumberOfTickets,
            TotalAmount = registration.TotalAmount,
            SpecialRequests = registration.SpecialRequests,
            DietaryRestrictions = registration.DietaryRestrictions,
            RegisteredAt = registration.RegisteredAt,
            ConfirmedAt = registration.ConfirmedAt,
            CancelledAt = registration.CancelledAt,
            CheckedInAt = registration.CheckedInAt,
            InvitationId = registration.InvitationId,
            RegistrationSource = registration.RegistrationSource,
            CustomData = !string.IsNullOrEmpty(registration.CustomData)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(registration.CustomData)
                : null
        };
    }
}
