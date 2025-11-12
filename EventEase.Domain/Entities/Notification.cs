using System.ComponentModel.DataAnnotations;
using EventEase.Domain.Enums;

namespace EventEase.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; }

    // Multi-tenant isolation
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    // Recipient
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // Notification content
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = null!;

    public NotificationType Type { get; set; }

    // Status
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    // Delivery
    public bool EmailSent { get; set; } = false;
    public bool SmsSent { get; set; } = false;
    public bool PushSent { get; set; } = false;

    public DateTime? EmailSentAt { get; set; }
    public DateTime? SmsSentAt { get; set; }
    public DateTime? PushSentAt { get; set; }

    // Optional link/action
    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    [MaxLength(50)]
    public string? ActionText { get; set; }

    // Related entity (optional)
    public Guid? RelatedEntityId { get; set; }
    [MaxLength(50)]
    public string? RelatedEntityType { get; set; } // "Event", "Registration", etc.

    // Additional data (JSON)
    public string? DataJson { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
