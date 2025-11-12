using EventEase.API.DTOs;
using EventEase.Application.Interfaces;
using EventEase.Domain.Entities;
using EventEase.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EventEase.API.Controllers;

/// <summary>
/// Guest management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "EventManagerOrAbove")]
[Produces("application/json")]
public class GuestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ILogger<GuestsController> _logger;

    public GuestsController(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ICurrentTenantService currentTenantService,
        ILogger<GuestsController> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _currentTenantService = currentTenantService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of guests
    /// </summary>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="search">Search by name, email, or company</param>
    /// <param name="industry">Filter by industry</param>
    /// <param name="tag">Filter by tag</param>
    /// <param name="minEngagementScore">Minimum engagement score</param>
    /// <param name="isActive">Filter by active status</param>
    /// <response code="200">Returns paginated guest list</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<GuestResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<GuestResponse>>> GetGuests(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? industry = null,
        [FromQuery] string? tag = null,
        [FromQuery] decimal? minEngagementScore = null,
        [FromQuery] bool? isActive = null)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Guests.AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(g =>
                g.FirstName.ToLower().Contains(searchLower) ||
                g.LastName.ToLower().Contains(searchLower) ||
                g.Email.ToLower().Contains(searchLower) ||
                (g.Company != null && g.Company.ToLower().Contains(searchLower)));
        }

        // Industry filter
        if (!string.IsNullOrWhiteSpace(industry))
        {
            query = query.Where(g => g.Industry == industry);
        }

        // Tag filter
        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.Where(g => g.Tags != null && g.Tags.Contains($"\"{tag}\""));
        }

        // Engagement score filter
        if (minEngagementScore.HasValue)
        {
            query = query.Where(g => g.EngagementScore >= minEngagementScore.Value);
        }

        // Active status filter
        if (isActive.HasValue)
        {
            query = query.Where(g => g.IsActive == isActive.Value);
        }

        query = query.OrderBy(g => g.LastName).ThenBy(g => g.FirstName);

        var totalCount = await query.CountAsync();

        var guests = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var guestResponses = new List<GuestResponse>();

        foreach (var guest in guests)
        {
            var invitations = await _context.EventInvitations
                .Where(i => i.GuestId == guest.Id)
                .ToListAsync();

            var totalInvitations = invitations.Count;
            var totalAttended = invitations.Count(i => i.AttendedEvent);

            guestResponses.Add(MapToGuestResponse(guest, totalInvitations, totalAttended));
        }

        return Ok(new PagedResult<GuestResponse>
        {
            Items = guestResponses,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Get guest by ID
    /// </summary>
    /// <param name="id">Guest ID</param>
    /// <response code="200">Returns guest details</response>
    /// <response code="404">Guest not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GuestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GuestResponse>> GetGuest(Guid id)
    {
        var guest = await _context.Guests.FindAsync(id);

        if (guest == null)
        {
            return NotFound(new { Error = "Guest not found" });
        }

        var invitations = await _context.EventInvitations
            .Where(i => i.GuestId == id)
            .ToListAsync();

        var totalInvitations = invitations.Count;
        var totalAttended = invitations.Count(i => i.AttendedEvent);

        return Ok(MapToGuestResponse(guest, totalInvitations, totalAttended));
    }

    /// <summary>
    /// Create a new guest
    /// </summary>
    /// <param name="request">Guest details</param>
    /// <response code="201">Guest created successfully</response>
    /// <response code="400">Invalid request or duplicate email</response>
    [HttpPost]
    [ProducesResponseType(typeof(GuestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GuestResponse>> CreateGuest([FromBody] CreateGuestRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check for duplicate email within tenant
        var existingGuest = await _context.Guests
            .FirstOrDefaultAsync(g => g.Email == request.Email && g.TenantId == _currentTenantService.TenantId);

        if (existingGuest != null)
        {
            return BadRequest(new { Error = "A guest with this email already exists" });
        }

        var guest = new Guest
        {
            TenantId = _currentTenantService.TenantId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Company = request.Company,
            JobTitle = request.JobTitle,
            Industry = request.Industry,
            Location = request.Location,
            PreferredLanguage = request.PreferredLanguage,
            Tags = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : null,
            Notes = request.Notes,
            EngagementScore = 50, // Default starting score
            IsActive = true
        };

        _context.Guests.Add(guest);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New guest created: {Email} for tenant {TenantId}",
            guest.Email, _currentTenantService.TenantId);

        return CreatedAtAction(nameof(GetGuest), new { id = guest.Id }, MapToGuestResponse(guest, 0, 0));
    }

    /// <summary>
    /// Update a guest
    /// </summary>
    /// <param name="id">Guest ID</param>
    /// <param name="request">Updated guest details</param>
    /// <response code="200">Guest updated successfully</response>
    /// <response code="404">Guest not found</response>
    /// <response code="400">Invalid request or duplicate email</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(GuestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GuestResponse>> UpdateGuest(Guid id, [FromBody] UpdateGuestRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var guest = await _context.Guests.FindAsync(id);

        if (guest == null)
        {
            return NotFound(new { Error = "Guest not found" });
        }

        // Check for duplicate email (excluding current guest)
        var duplicateGuest = await _context.Guests
            .FirstOrDefaultAsync(g =>
                g.Email == request.Email &&
                g.TenantId == _currentTenantService.TenantId &&
                g.Id != id);

        if (duplicateGuest != null)
        {
            return BadRequest(new { Error = "A guest with this email already exists" });
        }

        guest.FirstName = request.FirstName;
        guest.LastName = request.LastName;
        guest.Email = request.Email;
        guest.PhoneNumber = request.PhoneNumber;
        guest.Company = request.Company;
        guest.JobTitle = request.JobTitle;
        guest.Industry = request.Industry;
        guest.Location = request.Location;
        guest.PreferredLanguage = request.PreferredLanguage;
        guest.Tags = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : null;
        guest.Notes = request.Notes;
        guest.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Guest updated: {GuestId} ({Email})", id, guest.Email);

        var invitations = await _context.EventInvitations
            .Where(i => i.GuestId == id)
            .ToListAsync();

        var totalInvitations = invitations.Count;
        var totalAttended = invitations.Count(i => i.AttendedEvent);

        return Ok(MapToGuestResponse(guest, totalInvitations, totalAttended));
    }

    /// <summary>
    /// Delete a guest (soft delete)
    /// </summary>
    /// <param name="id">Guest ID</param>
    /// <response code="204">Guest deleted successfully</response>
    /// <response code="404">Guest not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGuest(Guid id)
    {
        var guest = await _context.Guests.FindAsync(id);

        if (guest == null)
        {
            return NotFound(new { Error = "Guest not found" });
        }

        // Soft delete
        guest.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Guest soft deleted: {GuestId} ({Email})", id, guest.Email);

        return NoContent();
    }

    /// <summary>
    /// Bulk import guests
    /// </summary>
    /// <param name="request">Bulk import request with guest list</param>
    /// <response code="200">Returns import results</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("import")]
    [ProducesResponseType(typeof(BulkImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkImportResponse>> BulkImportGuests([FromBody] BulkImportGuestsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = new BulkImportResponse
        {
            TotalProcessed = request.Guests.Count
        };

        var tenantId = _currentTenantService.TenantId;
        var rowNumber = 0;

        foreach (var item in request.Guests)
        {
            rowNumber++;

            try
            {
                // Check for existing guest
                var existingGuest = await _context.Guests
                    .FirstOrDefaultAsync(g => g.Email == item.Email && g.TenantId == tenantId);

                if (existingGuest != null)
                {
                    if (request.SkipDuplicates)
                    {
                        response.Skipped++;
                        continue;
                    }

                    // Update existing guest
                    existingGuest.FirstName = item.FirstName;
                    existingGuest.LastName = item.LastName;
                    existingGuest.PhoneNumber = item.PhoneNumber;
                    existingGuest.Company = item.Company;
                    existingGuest.JobTitle = item.JobTitle;
                    existingGuest.Industry = item.Industry;
                    existingGuest.Location = item.Location;
                    existingGuest.PreferredLanguage = item.PreferredLanguage ?? "en";
                    existingGuest.Notes = item.Notes;

                    // Merge tags
                    var existingTags = !string.IsNullOrEmpty(existingGuest.Tags)
                        ? JsonSerializer.Deserialize<List<string>>(existingGuest.Tags) ?? new List<string>()
                        : new List<string>();

                    if (item.Tags != null)
                    {
                        existingTags.AddRange(item.Tags);
                    }

                    if (request.BulkTags != null)
                    {
                        existingTags.AddRange(request.BulkTags);
                    }

                    existingGuest.Tags = existingTags.Distinct().Any()
                        ? JsonSerializer.Serialize(existingTags.Distinct().ToList())
                        : null;

                    response.Updated++;
                }
                else
                {
                    // Create new guest
                    var tags = new List<string>();
                    if (item.Tags != null)
                    {
                        tags.AddRange(item.Tags);
                    }
                    if (request.BulkTags != null)
                    {
                        tags.AddRange(request.BulkTags);
                    }

                    var guest = new Guest
                    {
                        TenantId = tenantId,
                        FirstName = item.FirstName,
                        LastName = item.LastName,
                        Email = item.Email,
                        PhoneNumber = item.PhoneNumber,
                        Company = item.Company,
                        JobTitle = item.JobTitle,
                        Industry = item.Industry,
                        Location = item.Location,
                        PreferredLanguage = item.PreferredLanguage ?? "en",
                        Tags = tags.Any() ? JsonSerializer.Serialize(tags.Distinct().ToList()) : null,
                        Notes = item.Notes,
                        EngagementScore = 50,
                        IsActive = true
                    };

                    _context.Guests.Add(guest);
                    response.Created++;
                }
            }
            catch (Exception ex)
            {
                response.Failed++;
                response.Errors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Email = item.Email,
                    ErrorMessage = ex.Message
                });

                _logger.LogError(ex, "Error importing guest at row {RowNumber}: {Email}",
                    rowNumber, item.Email);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk import completed: {Created} created, {Updated} updated, {Skipped} skipped, {Failed} failed",
            response.Created, response.Updated, response.Skipped, response.Failed);

        return Ok(response);
    }

    /// <summary>
    /// Get guest statistics
    /// </summary>
    /// <param name="id">Guest ID</param>
    /// <response code="200">Returns guest statistics</response>
    /// <response code="404">Guest not found</response>
    [HttpGet("{id}/statistics")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetGuestStatistics(Guid id)
    {
        var guest = await _context.Guests.FindAsync(id);

        if (guest == null)
        {
            return NotFound(new { Error = "Guest not found" });
        }

        var invitations = await _context.EventInvitations
            .Include(i => i.Event)
            .Where(i => i.GuestId == id)
            .ToListAsync();

        var totalInvitations = invitations.Count;
        var totalAttended = invitations.Count(i => i.AttendedEvent);
        var totalDeclined = invitations.Count(i => i.RsvpStatus == Domain.Enums.RsvpStatus.Declined);
        var totalPending = invitations.Count(i => i.RsvpStatus == Domain.Enums.RsvpStatus.Pending);

        var eventsByCategory = invitations
            .Where(i => i.Event != null)
            .GroupBy(i => i.Event!.Category ?? "Uncategorized")
            .Select(g => new
            {
                Category = g.Key,
                Count = g.Count(),
                AttendanceRate = g.Count() > 0 ? (decimal)g.Count(i => i.AttendedEvent) / g.Count() * 100 : 0
            })
            .ToList();

        return Ok(new
        {
            TotalInvitations = totalInvitations,
            TotalAttended = totalAttended,
            TotalDeclined = totalDeclined,
            TotalPending = totalPending,
            AttendanceRate = totalInvitations > 0 ? (decimal)totalAttended / totalInvitations * 100 : 0,
            EngagementScore = guest.EngagementScore,
            PredictedAttendanceRate = guest.PredictedAttendanceRate,
            LastContactDate = guest.LastContactDate,
            OptimalInviteTime = guest.OptimalInviteTime,
            EventsByCategory = eventsByCategory,
            RecentInvitations = invitations
                .OrderByDescending(i => i.InvitedAt)
                .Take(5)
                .Select(i => new
                {
                    EventId = i.EventId,
                    EventName = i.Event?.Name ?? "Unknown",
                    InvitedAt = i.InvitedAt,
                    RsvpStatus = i.RsvpStatus.ToString(),
                    AttendedEvent = i.AttendedEvent
                })
                .ToList()
        });
    }

    /// <summary>
    /// Calculate ML engagement score for guest
    /// </summary>
    /// <param name="id">Guest ID</param>
    /// <response code="200">Returns updated engagement score</response>
    /// <response code="404">Guest not found</response>
    [HttpPost("{id}/calculate-engagement")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CalculateEngagementScore(Guid id)
    {
        var guest = await _context.Guests.FindAsync(id);

        if (guest == null)
        {
            return NotFound(new { Error = "Guest not found" });
        }

        var invitations = await _context.EventInvitations
            .Where(i => i.GuestId == id)
            .ToListAsync();

        // Simple engagement scoring algorithm
        // In Phase 1.4, this will be replaced with actual ML model prediction
        decimal engagementScore = 50; // Base score

        var totalInvitations = invitations.Count;
        if (totalInvitations > 0)
        {
            var attendanceRate = (decimal)invitations.Count(i => i.AttendedEvent) / totalInvitations;
            var responseRate = (decimal)invitations.Count(i => i.RsvpStatus != Domain.Enums.RsvpStatus.Pending) / totalInvitations;

            // Weighted scoring
            engagementScore = (attendanceRate * 50) + (responseRate * 30) + 20;

            // Recent activity bonus (last 3 months)
            var recentInvitations = invitations.Count(i => i.InvitedAt > DateTime.UtcNow.AddMonths(-3));
            if (recentInvitations > 0)
            {
                engagementScore += Math.Min(10, recentInvitations * 2);
            }

            engagementScore = Math.Clamp(engagementScore, 0, 100);
        }

        guest.EngagementScore = engagementScore;
        guest.PredictedAttendanceRate = totalInvitations > 0
            ? (decimal)invitations.Count(i => i.AttendedEvent) / totalInvitations * 100
            : null;

        // Calculate optimal invite time based on historical data
        var acceptedInvitations = invitations
            .Where(i => i.RsvpStatus == Domain.Enums.RsvpStatus.Confirmed || i.AttendedEvent)
            .ToList();

        if (acceptedInvitations.Any())
        {
            var avgHour = acceptedInvitations
                .Average(i => i.InvitedAt.Hour);

            guest.OptimalInviteTime = DateTime.UtcNow.Date.AddHours(avgHour);
        }

        guest.LastContactDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Engagement score calculated for guest {GuestId}: {Score}",
            id, engagementScore);

        return Ok(new
        {
            GuestId = id,
            EngagementScore = guest.EngagementScore,
            PredictedAttendanceRate = guest.PredictedAttendanceRate,
            OptimalInviteTime = guest.OptimalInviteTime,
            TotalInvitations = totalInvitations,
            Message = "Engagement score calculated successfully. This will use ML models in Phase 1.4."
        });
    }

    private static GuestResponse MapToGuestResponse(Guest guest, int totalInvitations, int totalAttended)
    {
        return new GuestResponse
        {
            Id = guest.Id,
            TenantId = guest.TenantId,
            FirstName = guest.FirstName,
            LastName = guest.LastName,
            Email = guest.Email,
            PhoneNumber = guest.PhoneNumber,
            Company = guest.Company,
            JobTitle = guest.JobTitle,
            Industry = guest.Industry,
            Location = guest.Location,
            PreferredLanguage = guest.PreferredLanguage,
            Tags = !string.IsNullOrEmpty(guest.Tags)
                ? JsonSerializer.Deserialize<List<string>>(guest.Tags)
                : null,
            EngagementScore = guest.EngagementScore,
            LastContactDate = guest.LastContactDate,
            PredictedAttendanceRate = guest.PredictedAttendanceRate,
            OptimalInviteTime = guest.OptimalInviteTime,
            Notes = guest.Notes,
            IsActive = guest.IsActive,
            CreatedAt = guest.CreatedAt,
            UpdatedAt = guest.UpdatedAt,
            TotalInvitations = totalInvitations,
            TotalAttended = totalAttended
        };
    }
}
