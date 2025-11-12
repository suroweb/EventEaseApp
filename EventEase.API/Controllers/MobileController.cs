using EventEase.API.DTOs;
using EventEase.Application.Interfaces;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace EventEase.API.Controllers;

/// <summary>
/// Mobile-optimized API endpoints for EventEase mobile apps
/// </summary>
[ApiController]
[Route("api/mobile")]
[Authorize]
public class MobileController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<MobileController> _logger;

    public MobileController(
        ApplicationDbContext context,
        ICurrentTenantService currentTenantService,
        ICurrentUserService currentUserService,
        ILogger<MobileController> logger)
    {
        _context = context;
        _currentTenantService = currentTenantService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    // ===== Mobile-Optimized Event Endpoints =====

    /// <summary>
    /// Get lightweight events list for mobile app
    /// </summary>
    [HttpGet("events")]
    [ProducesResponseType(typeof(PagedResult<MobileEventListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMobileEvents(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] bool upcomingOnly = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        var userId = _currentUserService.UserId!.Value;

        var query = _context.Events.Where(e => e.TenantId == tenantId);

        // Filters
        if (upcomingOnly)
        {
            query = query.Where(e => e.StartDate > DateTime.UtcNow);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => e.Name.Contains(search) || (e.Description != null && e.Description.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(e => e.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EventStatus>(status, out var eventStatus))
        {
            query = query.Where(e => e.Status == eventStatus);
        }

        var totalCount = await query.CountAsync();

        var events = await query
            .OrderBy(e => e.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                Event = e,
                RegisteredCount = _context.EventRegistrations.Count(r => r.EventId == e.Id && r.Status == RegistrationStatus.Confirmed),
                IsUserRegistered = _context.EventRegistrations.Any(r => r.EventId == e.Id && r.UserId == userId)
            })
            .ToListAsync();

        var items = events.Select(e => new MobileEventListItem
        {
            Id = e.Event.Id,
            Name = e.Event.Name,
            StartDate = e.Event.StartDate,
            Venue = e.Event.Venue,
            ThumbnailUrl = e.Event.ImageUrl,
            RegisteredCount = e.RegisteredCount,
            AvailableSpots = e.Event.MaxAttendees.HasValue ? Math.Max(0, e.Event.MaxAttendees.Value - e.RegisteredCount) : 999,
            IsFree = e.Event.IsFree,
            Price = e.Event.Price,
            Currency = e.Event.Currency,
            IsUserRegistered = e.IsUserRegistered
        }).ToList();

        return Ok(new PagedResult<MobileEventListItem>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    /// <summary>
    /// Get detailed event for mobile app
    /// </summary>
    [HttpGet("events/{id}")]
    [ProducesResponseType(typeof(MobileEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMobileEventDetails(Guid id)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        var userId = _currentUserService.UserId!.Value;

        var evt = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

        if (evt == null)
            return NotFound(new { Message = "Event not found" });

        var registeredCount = await _context.EventRegistrations
            .CountAsync(r => r.EventId == id && r.Status == RegistrationStatus.Confirmed);

        var userRegistration = await _context.EventRegistrations
            .FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userId);

        var now = DateTime.UtcNow;
        var isRegistrationOpen = evt.Status == EventStatus.Published &&
                                 (!evt.RegistrationOpensAt.HasValue || evt.RegistrationOpensAt.Value <= now) &&
                                 (!evt.RegistrationClosesAt.HasValue || evt.RegistrationClosesAt.Value > now);

        var response = new MobileEventResponse
        {
            Id = evt.Id,
            Name = evt.Name,
            Description = evt.Description,
            Category = evt.Category,
            Status = evt.Status.ToString(),
            StartDate = evt.StartDate,
            EndDate = evt.EndDate,
            TimeZone = evt.TimeZone,
            Location = evt.Location,
            Venue = evt.Venue,
            IsVirtual = evt.IsVirtual,
            VirtualMeetingUrl = evt.VirtualMeetingUrl,
            Latitude = evt.VenueLatitude,
            Longitude = evt.VenueLongitude,
            MaxAttendees = evt.MaxAttendees,
            RegisteredCount = registeredCount,
            AvailableSpots = evt.MaxAttendees.HasValue ? Math.Max(0, evt.MaxAttendees.Value - registeredCount) : 999,
            Price = evt.Price,
            Currency = evt.Currency,
            IsFree = evt.IsFree,
            ThumbnailUrl = evt.ImageUrl,
            ImageUrl = evt.BannerUrl ?? evt.ImageUrl,
            RequiresApproval = evt.RequiresApproval,
            IsRegistrationOpen = isRegistrationOpen,
            RegistrationClosesAt = evt.RegistrationClosesAt,
            IsUserRegistered = userRegistration != null,
            UserRegistrationId = userRegistration?.Id
        };

        return Ok(response);
    }

    // ===== QR Code Endpoints =====

    /// <summary>
    /// Generate QR code for event or registration
    /// </summary>
    [HttpPost("qrcode/generate")]
    [ProducesResponseType(typeof(QRCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateQRCode([FromBody] QRCodeRequest request)
    {
        var tenantId = _currentTenantService.TenantId!.Value;

        string qrContent;
        if (request.RegistrationId.HasValue)
        {
            var registration = await _context.EventRegistrations
                .FirstOrDefaultAsync(r => r.Id == request.RegistrationId.Value && r.TenantId == tenantId);

            if (registration == null)
                return NotFound(new { Message = "Registration not found" });

            qrContent = $"EVENTEEASE:REG:{registration.Id}:{registration.EventId}";
        }
        else if (request.EventId.HasValue)
        {
            var evt = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == request.EventId.Value && e.TenantId == tenantId);

            if (evt == null)
                return NotFound(new { Message = "Event not found" });

            qrContent = $"EVENTEASE:EVT:{evt.Id}";
        }
        else
        {
            return BadRequest(new { Message = "Either EventId or RegistrationId must be provided" });
        }

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);

        byte[] qrCodeBytes;
        string contentType;

        if (request.Format.ToLower() == "svg")
        {
            using var qrCode = new SvgQRCode(qrCodeData);
            var svgString = qrCode.GetGraphic(request.Size / 100);
            qrCodeBytes = System.Text.Encoding.UTF8.GetBytes(svgString);
            contentType = "image/svg+xml";
        }
        else
        {
            using var qrCode = new PngByteQRCode(qrCodeData);
            qrCodeBytes = qrCode.GetGraphic(20);
            contentType = "image/png";
        }

        var base64QRCode = Convert.ToBase64String(qrCodeBytes);

        return Ok(new QRCodeResponse
        {
            QRCodeData = base64QRCode,
            Format = request.Format,
            ContentType = contentType,
            EncodedValue = qrContent
        });
    }

    /// <summary>
    /// Scan and validate QR code
    /// </summary>
    [HttpPost("qrcode/scan")]
    [ProducesResponseType(typeof(QRCodeScanResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ScanQRCode([FromBody] QRCodeScanRequest request)
    {
        var tenantId = _currentTenantService.TenantId!.Value;

        // Parse QR code content
        var parts = request.QRCodeData.Split(':');
        if (parts.Length < 3 || parts[0] != "EVENTEASE")
        {
            return Ok(new QRCodeScanResponse
            {
                IsValid = false,
                ErrorMessage = "Invalid QR code format"
            });
        }

        var type = parts[1];

        if (type == "REG" && parts.Length >= 4)
        {
            if (!Guid.TryParse(parts[2], out var registrationId))
            {
                return Ok(new QRCodeScanResponse { IsValid = false, ErrorMessage = "Invalid registration ID" });
            }

            var registration = await _context.EventRegistrations
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.Id == registrationId && r.TenantId == tenantId);

            if (registration == null)
            {
                return Ok(new QRCodeScanResponse { IsValid = false, ErrorMessage = "Registration not found" });
            }

            return Ok(new QRCodeScanResponse
            {
                IsValid = true,
                Type = "registration",
                EventId = registration.EventId,
                RegistrationId = registration.Id,
                EventName = registration.Event.Name,
                AttendeeName = $"{registration.FirstName} {registration.LastName}",
                AttendeeEmail = registration.Email,
                RegistrationStatus = registration.Status.ToString(),
                AlreadyCheckedIn = registration.CheckedInAt.HasValue,
                CheckedInAt = registration.CheckedInAt
            });
        }
        else if (type == "EVT" && parts.Length >= 3)
        {
            if (!Guid.TryParse(parts[2], out var eventId))
            {
                return Ok(new QRCodeScanResponse { IsValid = false, ErrorMessage = "Invalid event ID" });
            }

            var evt = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventId && e.TenantId == tenantId);

            if (evt == null)
            {
                return Ok(new QRCodeScanResponse { IsValid = false, ErrorMessage = "Event not found" });
            }

            return Ok(new QRCodeScanResponse
            {
                IsValid = true,
                Type = "event",
                EventId = evt.Id,
                EventName = evt.Name
            });
        }

        return Ok(new QRCodeScanResponse
        {
            IsValid = false,
            ErrorMessage = "Unknown QR code type"
        });
    }

    /// <summary>
    /// Quick check-in using QR code
    /// </summary>
    [HttpPost("qrcode/checkin")]
    [ProducesResponseType(typeof(GeolocationCheckInResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> QRCodeCheckIn([FromBody] QRCodeScanRequest request)
    {
        var tenantId = _currentTenantService.TenantId!.Value;

        // Parse QR code
        var parts = request.QRCodeData.Split(':');
        if (parts.Length < 4 || parts[0] != "EVENTEASE" || parts[1] != "REG")
        {
            return BadRequest(new { Message = "Invalid QR code for check-in" });
        }

        if (!Guid.TryParse(parts[2], out var registrationId))
        {
            return BadRequest(new { Message = "Invalid registration ID" });
        }

        var registration = await _context.EventRegistrations
            .FirstOrDefaultAsync(r => r.Id == registrationId && r.TenantId == tenantId);

        if (registration == null)
            return NotFound(new { Message = "Registration not found" });

        if (registration.Status != RegistrationStatus.Confirmed)
        {
            return BadRequest(new
            {
                Message = "Only confirmed registrations can be checked in",
                Status = registration.Status.ToString()
            });
        }

        if (registration.CheckedInAt.HasValue)
        {
            return Ok(new GeolocationCheckInResponse
            {
                Success = true,
                Message = "Attendee was already checked in",
                IsWithinGeofence = true,
                CheckedInAt = registration.CheckedInAt,
                RegistrationId = registration.Id
            });
        }

        // Perform check-in
        registration.Status = RegistrationStatus.CheckedIn;
        registration.CheckedInAt = DateTime.UtcNow;
        registration.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("QR code check-in successful for registration {RegistrationId}", registrationId);

        return Ok(new GeolocationCheckInResponse
        {
            Success = true,
            Message = "Check-in successful",
            IsWithinGeofence = true,
            CheckedInAt = registration.CheckedInAt,
            RegistrationId = registration.Id
        });
    }

    // ===== Geolocation Check-In =====

    /// <summary>
    /// Check-in with geolocation verification
    /// </summary>
    [HttpPost("checkin/geolocation")]
    [ProducesResponseType(typeof(GeolocationCheckInResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GeolocationCheckIn([FromBody] GeolocationCheckInRequest request)
    {
        var tenantId = _currentTenantService.TenantId!.Value;

        var registration = await _context.EventRegistrations
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.Id == request.RegistrationId && r.TenantId == tenantId);

        if (registration == null)
            return NotFound(new { Message = "Registration not found" });

        if (registration.Status != RegistrationStatus.Confirmed)
        {
            return BadRequest(new { Message = "Only confirmed registrations can be checked in" });
        }

        // Verify geolocation if event has venue coordinates
        bool isWithinGeofence = true;
        double? distance = null;
        const double maxAllowedDistance = 100; // 100 meters

        if (registration.Event.VenueLatitude.HasValue && registration.Event.VenueLongitude.HasValue)
        {
            distance = CalculateDistance(
                (double)registration.Event.VenueLatitude.Value,
                (double)registration.Event.VenueLongitude.Value,
                (double)request.Latitude,
                (double)request.Longitude
            );

            isWithinGeofence = distance <= maxAllowedDistance;

            if (!isWithinGeofence)
            {
                _logger.LogWarning(
                    "Check-in attempt outside geofence. Registration: {RegistrationId}, Distance: {Distance}m",
                    request.RegistrationId, distance);

                return Ok(new GeolocationCheckInResponse
                {
                    Success = false,
                    Message = $"You are too far from the venue ({distance:F0}m away). Please move closer to check in.",
                    IsWithinGeofence = false,
                    DistanceFromVenue = (decimal)distance,
                    MaxAllowedDistance = maxAllowedDistance
                });
            }
        }

        // Perform check-in
        registration.Status = RegistrationStatus.CheckedIn;
        registration.CheckedInAt = DateTime.UtcNow;
        registration.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Geolocation check-in successful for registration {RegistrationId}", request.RegistrationId);

        return Ok(new GeolocationCheckInResponse
        {
            Success = true,
            Message = "Check-in successful",
            IsWithinGeofence = true,
            DistanceFromVenue = distance.HasValue ? (decimal)distance : null,
            MaxAllowedDistance = maxAllowedDistance,
            CheckedInAt = registration.CheckedInAt,
            RegistrationId = registration.Id
        });
    }

    // ===== Offline Sync =====

    /// <summary>
    /// Sync offline changes with server
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(OfflineSyncResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SyncOfflineData([FromBody] OfflineSyncRequest request)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        var userId = _currentUserService.UserId!.Value;

        var response = new OfflineSyncResponse
        {
            Success = true,
            ServerTimestamp = DateTime.UtcNow,
            Results = new List<SyncResult>()
        };

        foreach (var action in request.PendingActions)
        {
            try
            {
                var result = await ProcessOfflineAction(action, tenantId, userId);
                response.Results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing offline action {ActionId}", action.ActionId);
                response.Results.Add(new SyncResult
                {
                    ActionId = action.ActionId,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        // Get server updates since last sync
        var serverUpdates = await GetServerUpdatesSince(request.LastSyncTimestamp, tenantId, userId);
        response.ServerUpdates = serverUpdates;

        return Ok(response);
    }

    /// <summary>
    /// Get data for offline mode
    /// </summary>
    [HttpPost("offline/data")]
    [ProducesResponseType(typeof(OfflineDataResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOfflineData([FromBody] OfflineDataRequest request)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        var userId = _currentUserService.UserId!.Value;

        var response = new OfflineDataResponse
        {
            Timestamp = DateTime.UtcNow
        };

        // Get events
        var eventsQuery = _context.Events.Where(e => e.TenantId == tenantId);

        if (request.EventIds.Any())
        {
            eventsQuery = eventsQuery.Where(e => request.EventIds.Contains(e.Id));
        }
        else
        {
            // Get upcoming events only for offline mode
            eventsQuery = eventsQuery.Where(e => e.StartDate > DateTime.UtcNow.AddDays(-1));
        }

        if (request.SinceTimestamp.HasValue)
        {
            eventsQuery = eventsQuery.Where(e => e.UpdatedAt > request.SinceTimestamp.Value);
        }

        var events = await eventsQuery.Take(request.EventIds.Any() ? 100 : 50).ToListAsync();
        response.Events = events.Cast<object>().ToList();

        // Get registrations if requested
        if (request.IncludeRegistrations)
        {
            var eventIds = events.Select(e => e.Id).ToList();
            var registrations = await _context.EventRegistrations
                .Where(r => r.TenantId == tenantId && eventIds.Contains(r.EventId))
                .ToListAsync();
            response.Registrations = registrations.Cast<object>().ToList();
        }

        return Ok(response);
    }

    // ===== App Configuration =====

    /// <summary>
    /// Get mobile app configuration
    /// </summary>
    [HttpGet("config")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MobileAppConfigResponse), StatusCodes.Status200OK)]
    public IActionResult GetMobileConfig()
    {
        return Ok(new MobileAppConfigResponse
        {
            ApiVersion = "1.9.0",
            GraphQLEnabled = true,
            GraphQLEndpoint = "/graphql",
            PushNotificationsEnabled = true,
            OfflineModeEnabled = true,
            QRCodeScanningEnabled = true,
            GeolocationCheckInEnabled = true,
            MaxOfflineEventsCount = 50,
            SyncIntervalMinutes = 15,
            FeatureFlags = new Dictionary<string, object>
            {
                { "ai_agents", true },
                { "stripe_payments", true },
                { "realtime_updates", true },
                { "analytics", true }
            }
        });
    }

    /// <summary>
    /// Check app version compatibility
    /// </summary>
    [HttpPost("version/check")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AppVersionCheckResponse), StatusCodes.Status200OK)]
    public IActionResult CheckAppVersion([FromBody] AppVersionCheckRequest request)
    {
        // In a real app, this would check against a database or configuration
        var latestVersion = "1.9.0";
        var minimumSupportedVersion = "1.5.0";

        var isSupported = CompareVersions(request.CurrentVersion, minimumSupportedVersion) >= 0;
        var isLatest = request.CurrentVersion == latestVersion;
        var requiresUpdate = CompareVersions(request.CurrentVersion, latestVersion) < 0;

        return Ok(new AppVersionCheckResponse
        {
            IsSupported = isSupported,
            IsLatestVersion = isLatest,
            RequiresUpdate = requiresUpdate,
            ForceUpdate = !isSupported,
            LatestVersion = latestVersion,
            MinimumSupportedVersion = minimumSupportedVersion,
            UpdateUrl = request.Platform.ToLower() == "ios"
                ? "https://apps.apple.com/app/eventease"
                : "https://play.google.com/store/apps/details?id=com.eventease",
            ReleaseNotes = "New features: Mobile app support with GraphQL, QR code scanning, offline mode, and geolocation check-in.",
            ReleaseDate = new DateTime(2025, 11, 12),
            NewFeatures = new List<string>
            {
                "GraphQL API support",
                "QR code generation and scanning",
                "Offline mode with sync",
                "Geolocation-based check-in",
                "Real-time updates via subscriptions"
            }
        });
    }

    // ===== Helper Methods =====

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula to calculate distance between two points on Earth
        const double R = 6371000; // Earth radius in meters

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    private async Task<SyncResult> ProcessOfflineAction(OfflineAction action, Guid tenantId, Guid userId)
    {
        switch (action.ActionType.ToLower())
        {
            case "check_in":
                if (action.Data.TryGetValue("registrationId", out var regIdObj) &&
                    Guid.TryParse(regIdObj.ToString(), out var registrationId))
                {
                    var registration = await _context.EventRegistrations
                        .FirstOrDefaultAsync(r => r.Id == registrationId && r.TenantId == tenantId);

                    if (registration != null && registration.Status == RegistrationStatus.Confirmed)
                    {
                        registration.Status = RegistrationStatus.CheckedIn;
                        registration.CheckedInAt = action.Timestamp;
                        registration.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        return new SyncResult
                        {
                            ActionId = action.ActionId,
                            Success = true,
                            ServerData = new { RegistrationId = registrationId, CheckedInAt = registration.CheckedInAt }
                        };
                    }
                }
                break;

            // Add more action types as needed
        }

        return new SyncResult
        {
            ActionId = action.ActionId,
            Success = false,
            ErrorMessage = "Unknown or invalid action"
        };
    }

    private async Task<List<ServerUpdate>> GetServerUpdatesSince(DateTime sinceTimestamp, Guid tenantId, Guid userId)
    {
        var updates = new List<ServerUpdate>();

        // Get updated events
        var updatedEvents = await _context.Events
            .Where(e => e.TenantId == tenantId && e.UpdatedAt > sinceTimestamp)
            .Take(50)
            .ToListAsync();

        updates.AddRange(updatedEvents.Select(e => new ServerUpdate
        {
            EntityType = "event",
            UpdateType = "updated",
            EntityId = e.Id,
            Timestamp = e.UpdatedAt ?? DateTime.UtcNow,
            Data = e
        }));

        return updates;
    }

    private int CompareVersions(string version1, string version2)
    {
        var v1Parts = version1.Split('.').Select(int.Parse).ToArray();
        var v2Parts = version2.Split('.').Select(int.Parse).ToArray();

        for (int i = 0; i < Math.Max(v1Parts.Length, v2Parts.Length); i++)
        {
            var v1Part = i < v1Parts.Length ? v1Parts[i] : 0;
            var v2Part = i < v2Parts.Length ? v2Parts[i] : 0;

            if (v1Part > v2Part) return 1;
            if (v1Part < v2Part) return -1;
        }

        return 0;
    }
}
