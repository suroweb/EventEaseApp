using EventEase.Application.Interfaces;
using EventEase.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventEase.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get notification history for current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<NotificationHistoryResponse>> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { Error = "Invalid pagination parameters" });
            }

            var notifications = await _notificationService.GetNotificationHistoryAsync(
                _currentUserService.UserId,
                page,
                pageSize);

            var unreadCount = await _notificationService.GetUnreadCountAsync(_currentUserService.UserId);

            return Ok(new NotificationHistoryResponse
            {
                Notifications = notifications,
                Page = page,
                PageSize = pageSize,
                UnreadCount = unreadCount,
                TotalCount = notifications.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications");
            return StatusCode(500, new { Error = "An error occurred while retrieving notifications" });
        }
    }

    /// <summary>
    /// Get unread notification count
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadCountResponse>> GetUnreadCount()
    {
        try
        {
            var count = await _notificationService.GetUnreadCountAsync(_currentUserService.UserId);

            return Ok(new UnreadCountResponse
            {
                Count = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread count");
            return StatusCode(500, new { Error = "An error occurred while retrieving unread count" });
        }
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    [HttpPost("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId)
    {
        try
        {
            var success = await _notificationService.MarkAsReadAsync(notificationId);

            if (!success)
            {
                return NotFound(new { Error = "Notification not found" });
            }

            return Ok(new { Message = "Notification marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return StatusCode(500, new { Error = "An error occurred while marking notification as read" });
        }
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var success = await _notificationService.MarkAllAsReadAsync(_currentUserService.UserId);

            if (!success)
            {
                return StatusCode(500, new { Error = "Failed to mark all notifications as read" });
            }

            return Ok(new { Message = "All notifications marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, new { Error = "An error occurred while marking all notifications as read" });
        }
    }

    /// <summary>
    /// Delete notification
    /// </summary>
    [HttpDelete("{notificationId}")]
    public async Task<IActionResult> DeleteNotification(Guid notificationId)
    {
        try
        {
            var success = await _notificationService.DeleteNotificationAsync(notificationId);

            if (!success)
            {
                return NotFound(new { Error = "Notification not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification");
            return StatusCode(500, new { Error = "An error occurred while deleting notification" });
        }
    }

    /// <summary>
    /// Get notification preferences
    /// </summary>
    [HttpGet("preferences")]
    public async Task<ActionResult<NotificationPreferences>> GetPreferences()
    {
        try
        {
            var preferences = await _notificationService.GetUserPreferencesAsync(_currentUserService.UserId);
            return Ok(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification preferences");
            return StatusCode(500, new { Error = "An error occurred while retrieving preferences" });
        }
    }

    /// <summary>
    /// Update notification preferences
    /// </summary>
    [HttpPut("preferences")]
    public async Task<ActionResult<NotificationPreferences>> UpdatePreferences(
        [FromBody] NotificationPreferences preferences)
    {
        try
        {
            var success = await _notificationService.UpdateUserPreferencesAsync(
                _currentUserService.UserId,
                preferences);

            if (!success)
            {
                return StatusCode(500, new { Error = "Failed to update preferences" });
            }

            return Ok(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences");
            return StatusCode(500, new { Error = "An error occurred while updating preferences" });
        }
    }

    /// <summary>
    /// Send test notification (for testing purposes)
    /// </summary>
    [HttpPost("test")]
    public async Task<ActionResult<NotificationResult>> SendTestNotification()
    {
        try
        {
            var result = await _notificationService.SendNotificationAsync(
                _currentUserService.UserId,
                "Test Notification",
                "This is a test notification from EventEase. If you're seeing this, notifications are working correctly!",
                NotificationType.Info);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test notification");
            return StatusCode(500, new { Error = "An error occurred while sending test notification" });
        }
    }
}

public class NotificationHistoryResponse
{
    public List<NotificationHistory> Notifications { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int UnreadCount { get; set; }
    public int TotalCount { get; set; }
}

public class UnreadCountResponse
{
    public int Count { get; set; }
}
