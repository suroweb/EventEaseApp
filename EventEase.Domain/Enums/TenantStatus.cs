namespace EventEase.Domain.Enums;

/// <summary>
/// Status of a tenant account
/// </summary>
public enum TenantStatus
{
    /// <summary>
    /// Trial period
    /// </summary>
    Trial = 0,

    /// <summary>
    /// Active subscription
    /// </summary>
    Active = 1,

    /// <summary>
    /// Suspended due to payment or policy violation
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// Cancelled by tenant
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Expired trial or subscription
    /// </summary>
    Expired = 4
}
