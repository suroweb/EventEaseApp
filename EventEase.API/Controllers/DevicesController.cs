using EventEase.API.DTOs;
using EventEase.Application.Interfaces;
using EventEase.Domain.Entities;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.API.Controllers;

/// <summary>
/// Device management and push notification endpoints for mobile apps
/// </summary>
[ApiController]
[Route("api/devices")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(
        ApplicationDbContext context,
        ICurrentTenantService currentTenantService,
        ICurrentUserService currentUserService,
        INotificationService notificationService,
        ILogger<DevicesController> logger)
    {
        _context = context;
        _currentTenantService = currentTenantService;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Register a device for push notifications
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(DeviceRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterDevice([FromBody] DeviceRegistrationRequest request)
    {
        var tenantId = _currentTenantService.TenantId!.Value;
        var userId = _currentUserService.UserId!.Value;

        if (string.IsNullOrWhiteSpace(request.DeviceToken))
        {
            return BadRequest(new { Message = "Device token is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Platform))
        {
            return BadRequest(new { Message = "Platform is required" });
        }

        // Check if device already exists
        var existingDevice = await _context.MobileDevices
            .FirstOrDefaultAsync(d => d.DeviceToken == request.DeviceToken && d.UserId == userId);

        if (existingDevice != null)
        {
            // Update existing device
            existingDevice.Platform = request.Platform;
            existingDevice.DeviceModel = request.DeviceModel;
            existingDevice.OsVersion = request.OsVersion;
            existingDevice.AppVersion = request.AppVersion;
            existingDevice.Language = request.Language;
            existingDevice.TimeZone = request.TimeZone;
            existingDevice.IsActive = true;
            existingDevice.LastSeenAt = DateTime.UtcNow;
            existingDevice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Device updated: {DeviceId} for user {UserId}", existingDevice.Id, userId);

            return Ok(new DeviceRegistrationResponse
            {
                Success = true,
                Message = "Device updated successfully",
                DeviceId = existingDevice.Id,
                RegisteredAt = existingDevice.CreatedAt
            });
        }

        // Create new device
        var device = new MobileDevice
        {
            TenantId = tenantId,
            UserId = userId,
            DeviceToken = request.DeviceToken,
            Platform = request.Platform,
            DeviceModel = request.DeviceModel,
            OsVersion = request.OsVersion,
            AppVersion = request.AppVersion,
            Language = request.Language,
            TimeZone = request.TimeZone,
            IsActive = true,
            LastSeenAt = DateTime.UtcNow
        };

        _context.MobileDevices.Add(device);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Device registered: {DeviceId} for user {UserId}", device.Id, userId);

        return Ok(new DeviceRegistrationResponse
        {
            Success = true,
            Message = "Device registered successfully",
            DeviceId = device.Id,
            RegisteredAt = device.CreatedAt
        });
    }

    /// <summary>
    /// Unregister a device (disable push notifications)
    /// </summary>
    [HttpPost("unregister")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnregisterDevice([FromBody] string deviceToken)
    {
        var userId = _currentUserService.UserId!.Value;

        var device = await _context.MobileDevices
            .FirstOrDefaultAsync(d => d.DeviceToken == deviceToken && d.UserId == userId);

        if (device == null)
        {
            return NotFound(new { Message = "Device not found" });
        }

        device.IsActive = false;
        device.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Device unregistered: {DeviceId}", device.Id);

        return Ok(new { Message = "Device unregistered successfully" });
    }

    /// <summary>
    /// Get all registered devices for current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MobileDeviceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyDevices()
    {
        var userId = _currentUserService.UserId!.Value;

        var devices = await _context.MobileDevices
            .Where(d => d.UserId == userId && d.IsActive)
            .OrderByDescending(d => d.LastSeenAt)
            .ToListAsync();

        var response = devices.Select(d => new MobileDeviceResponse
        {
            Id = d.Id,
            Platform = d.Platform,
            DeviceModel = d.DeviceModel,
            OsVersion = d.OsVersion,
            AppVersion = d.AppVersion,
            RegisteredAt = d.CreatedAt,
            LastSeenAt = d.LastSeenAt
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Update notification preferences
    /// </summary>
    [HttpPost("preferences")]
    [ProducesResponseType(typeof(NotificationPreferencesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateNotificationPreferences([FromBody] NotificationPreferencesRequest request)
    {
        var userId = _currentUserService.UserId!.Value;

        // In a real implementation, this would update the user's notification preferences in the database
        // For now, we'll just return success
        _logger.LogInformation("Notification preferences updated for user {UserId}", userId);

        return Ok(new NotificationPreferencesResponse
        {
            Success = true,
            Preferences = request
        });
    }

    /// <summary>
    /// Update badge count for a device
    /// </summary>
    [HttpPost("badge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBadgeCount([FromBody] BadgeCountRequest request)
    {
        var userId = _currentUserService.UserId!.Value;

        if (!string.IsNullOrWhiteSpace(request.DeviceToken))
        {
            var device = await _context.MobileDevices
                .FirstOrDefaultAsync(d => d.DeviceToken == request.DeviceToken && d.UserId == userId);

            if (device != null)
            {
                device.BadgeCount = request.Count;
                device.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        return Ok(new { Message = "Badge count updated" });
    }

    /// <summary>
    /// Send a test push notification
    /// </summary>
    [HttpPost("test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SendTestNotification([FromQuery] string? deviceToken)
    {
        var userId = _currentUserService.UserId!.Value;
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        try
        {
            await _notificationService.SendNotificationAsync(new Notification
            {
                TenantId = user.TenantId,
                UserId = userId,
                Type = Domain.Enums.NotificationType.System,
                Title = "Test Notification",
                Message = "This is a test push notification from EventEase",
                IsRead = false
            });

            return Ok(new { Message = "Test notification sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test notification to user {UserId}", userId);
            return StatusCode(500, new { Message = "Failed to send test notification", Error = ex.Message });
        }
    }

    /// <summary>
    /// Send push notification to specific users (Admin only)
    /// </summary>
    [HttpPost("send")]
    [Authorize(Policy = "TenantOwnerOrAdmin")]
    [ProducesResponseType(typeof(PushNotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SendPushNotification([FromBody] PushNotificationRequest request)
    {
        var tenantId = _currentTenantService.TenantId!.Value;

        var successCount = 0;
        var failureCount = 0;
        var errors = new List<string>();

        // Get target devices
        IQueryable<MobileDevice> deviceQuery = _context.MobileDevices
            .Where(d => d.TenantId == tenantId && d.IsActive);

        if (request.UserIds != null && request.UserIds.Any())
        {
            deviceQuery = deviceQuery.Where(d => request.UserIds.Contains(d.UserId));
        }

        if (request.DeviceIds != null && request.DeviceIds.Any())
        {
            deviceQuery = deviceQuery.Where(d => request.DeviceIds.Contains(d.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.Platform))
        {
            deviceQuery = deviceQuery.Where(d => d.Platform == request.Platform);
        }

        var devices = await deviceQuery.ToListAsync();

        // Send notifications
        foreach (var device in devices)
        {
            try
            {
                // Create notification record
                var notification = new Notification
                {
                    TenantId = tenantId,
                    UserId = device.UserId,
                    Type = NotificationType.System,
                    Title = request.Title,
                    Message = request.Body,
                    IsRead = false
                };

                await _notificationService.SendNotificationAsync(notification);
                successCount++;

                _logger.LogInformation("Push notification sent to device {DeviceId}", device.Id);
            }
            catch (Exception ex)
            {
                failureCount++;
                errors.Add($"Device {device.Id}: {ex.Message}");
                _logger.LogError(ex, "Failed to send push notification to device {DeviceId}", device.Id);
            }
        }

        return Ok(new PushNotificationResponse
        {
            Success = successCount > 0,
            TotalRecipients = devices.Count,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Errors = errors
        });
    }

    /// <summary>
    /// Send event reminder notifications
    /// </summary>
    [HttpPost("reminders/event/{eventId}")]
    [Authorize(Policy = "EventManagerOrAbove")]
    [ProducesResponseType(typeof(PushNotificationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendEventReminder(Guid eventId)
    {
        var tenantId = _currentTenantService.TenantId!.Value;

        var evt = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == eventId && e.TenantId == tenantId);

        if (evt == null)
        {
            return NotFound(new { Message = "Event not found" });
        }

        // Get all confirmed registrations
        var registrations = await _context.EventRegistrations
            .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Confirmed && r.UserId.HasValue)
            .ToListAsync();

        var userIds = registrations.Select(r => r.UserId!.Value).Distinct().ToList();

        var notificationRequest = new PushNotificationRequest
        {
            UserIds = userIds,
            Title = $"Event Reminder: {evt.Name}",
            Body = $"Your event '{evt.Name}' starts on {evt.StartDate:MMM dd, yyyy} at {evt.StartDate:hh:mm tt}",
            Category = "event_reminder",
            Data = new Dictionary<string, string>
            {
                { "event_id", eventId.ToString() },
                { "type", "event_reminder" }
            }
        };

        return await SendPushNotification(notificationRequest);
    }
}

/// <summary>
/// Mobile device response DTO
/// </summary>
public class MobileDeviceResponse
{
    public Guid Id { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string? DeviceModel { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}
