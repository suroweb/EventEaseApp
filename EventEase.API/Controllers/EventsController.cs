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
/// Event management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICurrentTenantService currentTenantService,
        ILogger<EventsController> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _currentTenantService = currentTenantService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of events
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="search">Search term for name or description</param>
    /// <param name="category">Filter by category</param>
    /// <param name="status">Filter by status</param>
    /// <param name="startDate">Filter events starting from this date</param>
    /// <param name="endDate">Filter events ending before this date</param>
    /// <response code="200">Returns paginated events</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventResponse>>> GetEvents(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] EventStatus? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Events
            .Include(e => e.CreatedByUser)
            .Include(e => e.Registrations)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e =>
                e.Name.Contains(search) ||
                (e.Description != null && e.Description.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(e => e.Category == category);
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(e => e.StartDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.EndDate <= endDate.Value);
        }

        // Order by start date (upcoming events first)
        query = query.OrderBy(e => e.StartDate);

        var totalCount = await query.CountAsync();

        var events = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(e => MapToEventResponse(e))
            .ToListAsync();

        return Ok(new PagedResult<EventResponse>
        {
            Items = events,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Get event by ID
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <response code="200">Returns event details</response>
    /// <response code="404">Event not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetEvent(Guid id)
    {
        var eventEntity = await _context.Events
            .Include(e => e.CreatedByUser)
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventEntity == null)
        {
            return NotFound(new { Error = "Event not found" });
        }

        return Ok(MapToEventResponse(eventEntity));
    }

    /// <summary>
    /// Create a new event
    /// </summary>
    /// <param name="request">Event details</param>
    /// <response code="201">Event created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="403">Insufficient permissions</response>
    [HttpPost]
    [Authorize(Policy = "EventManagerOrAbove")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventResponse>> CreateEvent([FromBody] CreateEventRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate dates
        if (request.EndDate <= request.StartDate)
        {
            return BadRequest(new { Error = "End date must be after start date" });
        }

        if (request.StartDate < DateTime.UtcNow)
        {
            return BadRequest(new { Error = "Start date must be in the future" });
        }

        // Create event
        var eventEntity = new Event
        {
            TenantId = _currentTenantService.TenantId,
            CreatedByUserId = _currentUserService.UserId,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Status = EventStatus.Draft,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TimeZone = request.TimeZone,
            Location = request.Location,
            Venue = request.Venue,
            VenueAddress = request.VenueAddress,
            VenueCity = request.VenueCity,
            VenueCountry = request.VenueCountry,
            VenueLatitude = request.VenueLatitude,
            VenueLongitude = request.VenueLongitude,
            IsVirtual = request.IsVirtual,
            VirtualMeetingUrl = request.VirtualMeetingUrl,
            MaxAttendees = request.MaxAttendees,
            Price = request.Price,
            Currency = request.Currency,
            IsFree = request.IsFree,
            ImageUrl = request.ImageUrl,
            BannerUrl = request.BannerUrl,
            RequiresApproval = request.RequiresApproval,
            RegistrationOpensAt = request.RegistrationOpensAt,
            RegistrationClosesAt = request.RegistrationClosesAt,
            AllowWaitlist = request.AllowWaitlist,
            Tags = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : null,
            CustomFields = request.CustomFields != null ? JsonSerializer.Serialize(request.CustomFields) : null
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Load navigation properties
        await _context.Entry(eventEntity).Reference(e => e.CreatedByUser).LoadAsync();

        _logger.LogInformation("Event created: {EventName} ({EventId}) by user {UserId}",
            eventEntity.Name, eventEntity.Id, _currentUserService.UserId);

        var response = MapToEventResponse(eventEntity);

        return CreatedAtAction(nameof(GetEvent), new { id = eventEntity.Id }, response);
    }

    /// <summary>
    /// Update an existing event
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <param name="request">Updated event details</param>
    /// <response code="200">Event updated successfully</response>
    /// <response code="404">Event not found</response>
    /// <response code="403">Insufficient permissions</response>
    [HttpPut("{id}")]
    [Authorize(Policy = "EventManagerOrAbove")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventResponse>> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var eventEntity = await _context.Events
            .Include(e => e.CreatedByUser)
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventEntity == null)
        {
            return NotFound(new { Error = "Event not found" });
        }

        // Validate dates
        if (request.EndDate <= request.StartDate)
        {
            return BadRequest(new { Error = "End date must be after start date" });
        }

        // Update event properties
        eventEntity.Name = request.Name;
        eventEntity.Description = request.Description;
        eventEntity.Category = request.Category;
        eventEntity.StartDate = request.StartDate;
        eventEntity.EndDate = request.EndDate;
        eventEntity.TimeZone = request.TimeZone;
        eventEntity.Location = request.Location;
        eventEntity.Venue = request.Venue;
        eventEntity.VenueAddress = request.VenueAddress;
        eventEntity.VenueCity = request.VenueCity;
        eventEntity.VenueCountry = request.VenueCountry;
        eventEntity.VenueLatitude = request.VenueLatitude;
        eventEntity.VenueLongitude = request.VenueLongitude;
        eventEntity.IsVirtual = request.IsVirtual;
        eventEntity.VirtualMeetingUrl = request.VirtualMeetingUrl;
        eventEntity.MaxAttendees = request.MaxAttendees;
        eventEntity.Price = request.Price;
        eventEntity.Currency = request.Currency;
        eventEntity.IsFree = request.IsFree;
        eventEntity.ImageUrl = request.ImageUrl;
        eventEntity.BannerUrl = request.BannerUrl;
        eventEntity.RequiresApproval = request.RequiresApproval;
        eventEntity.RegistrationOpensAt = request.RegistrationOpensAt;
        eventEntity.RegistrationClosesAt = request.RegistrationClosesAt;
        eventEntity.AllowWaitlist = request.AllowWaitlist;
        eventEntity.Tags = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : null;
        eventEntity.CustomFields = request.CustomFields != null ? JsonSerializer.Serialize(request.CustomFields) : null;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Event updated: {EventName} ({EventId}) by user {UserId}",
            eventEntity.Name, eventEntity.Id, _currentUserService.UserId);

        return Ok(MapToEventResponse(eventEntity));
    }

    /// <summary>
    /// Publish an event (change status from Draft to Published)
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <response code="200">Event published successfully</response>
    /// <response code="404">Event not found</response>
    [HttpPost("{id}/publish")]
    [Authorize(Policy = "EventManagerOrAbove")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> PublishEvent(Guid id)
    {
        var eventEntity = await _context.Events
            .Include(e => e.CreatedByUser)
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventEntity == null)
        {
            return NotFound(new { Error = "Event not found" });
        }

        if (eventEntity.Status != EventStatus.Draft)
        {
            return BadRequest(new { Error = "Only draft events can be published" });
        }

        eventEntity.Status = EventStatus.Published;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Event published: {EventName} ({EventId})", eventEntity.Name, eventEntity.Id);

        return Ok(MapToEventResponse(eventEntity));
    }

    /// <summary>
    /// Cancel an event
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <response code="200">Event cancelled successfully</response>
    /// <response code="404">Event not found</response>
    [HttpPost("{id}/cancel")]
    [Authorize(Policy = "EventManagerOrAbove")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> CancelEvent(Guid id)
    {
        var eventEntity = await _context.Events
            .Include(e => e.CreatedByUser)
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventEntity == null)
        {
            return NotFound(new { Error = "Event not found" });
        }

        eventEntity.Status = EventStatus.Cancelled;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Event cancelled: {EventName} ({EventId})", eventEntity.Name, eventEntity.Id);

        // TODO: Send cancellation emails to all registered attendees

        return Ok(MapToEventResponse(eventEntity));
    }

    /// <summary>
    /// Delete an event (soft delete by changing status)
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <response code="204">Event deleted successfully</response>
    /// <response code="404">Event not found</response>
    /// <response code="400">Cannot delete event with registrations</response>
    [HttpDelete("{id}")]
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        var eventEntity = await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventEntity == null)
        {
            return NotFound(new { Error = "Event not found" });
        }

        // Check if event has registrations
        if (eventEntity.Registrations.Any())
        {
            return BadRequest(new { Error = "Cannot delete event with existing registrations. Cancel the event instead." });
        }

        _context.Events.Remove(eventEntity);
        await _context.SaveChangesAsync();

        _logger.LogWarning("Event deleted: {EventName} ({EventId}) by user {UserId}",
            eventEntity.Name, eventEntity.Id, _currentUserService.UserId);

        return NoContent();
    }

    /// <summary>
    /// Get event categories (distinct list)
    /// </summary>
    /// <response code="200">Returns list of categories</response>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        var categories = await _context.Events
            .Where(e => e.Category != null)
            .Select(e => e.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return Ok(categories);
    }

    private static EventResponse MapToEventResponse(Event eventEntity)
    {
        var currentAttendees = eventEntity.Registrations?
            .Count(r => r.Status == RegistrationStatus.Confirmed || r.Status == RegistrationStatus.CheckedIn)
            ?? 0;

        var isRegistrationOpen = eventEntity.Status == EventStatus.Published &&
                                 (!eventEntity.RegistrationOpensAt.HasValue || eventEntity.RegistrationOpensAt.Value <= DateTime.UtcNow) &&
                                 (!eventEntity.RegistrationClosesAt.HasValue || eventEntity.RegistrationClosesAt.Value >= DateTime.UtcNow);

        return new EventResponse
        {
            Id = eventEntity.Id,
            TenantId = eventEntity.TenantId,
            CreatedByUserId = eventEntity.CreatedByUserId,
            CreatedByName = $"{eventEntity.CreatedByUser?.FirstName} {eventEntity.CreatedByUser?.LastName}".Trim(),
            Name = eventEntity.Name,
            Description = eventEntity.Description,
            Category = eventEntity.Category,
            Status = eventEntity.Status.ToString(),
            StartDate = eventEntity.StartDate,
            EndDate = eventEntity.EndDate,
            TimeZone = eventEntity.TimeZone,
            Location = eventEntity.Location,
            Venue = eventEntity.Venue,
            VenueAddress = eventEntity.VenueAddress,
            VenueCity = eventEntity.VenueCity,
            VenueCountry = eventEntity.VenueCountry,
            VenueLatitude = eventEntity.VenueLatitude,
            VenueLongitude = eventEntity.VenueLongitude,
            IsVirtual = eventEntity.IsVirtual,
            VirtualMeetingUrl = eventEntity.VirtualMeetingUrl,
            MaxAttendees = eventEntity.MaxAttendees,
            CurrentAttendees = currentAttendees,
            Price = eventEntity.Price,
            Currency = eventEntity.Currency,
            IsFree = eventEntity.IsFree,
            ImageUrl = eventEntity.ImageUrl,
            BannerUrl = eventEntity.BannerUrl,
            IsAIGenerated = eventEntity.IsAIGenerated,
            AIGenerationPrompt = eventEntity.AIGenerationPrompt,
            RequiresApproval = eventEntity.RequiresApproval,
            RegistrationOpensAt = eventEntity.RegistrationOpensAt,
            RegistrationClosesAt = eventEntity.RegistrationClosesAt,
            AllowWaitlist = eventEntity.AllowWaitlist,
            IsRegistrationOpen = isRegistrationOpen,
            Tags = !string.IsNullOrEmpty(eventEntity.Tags)
                ? JsonSerializer.Deserialize<List<string>>(eventEntity.Tags)
                : null,
            CustomFields = !string.IsNullOrEmpty(eventEntity.CustomFields)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(eventEntity.CustomFields)
                : null,
            CreatedAt = eventEntity.CreatedAt,
            UpdatedAt = eventEntity.UpdatedAt
        };
    }
}
