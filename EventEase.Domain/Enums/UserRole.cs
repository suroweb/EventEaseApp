namespace EventEase.Domain.Enums;

/// <summary>
/// User roles within the system
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Platform super administrator (across all tenants)
    /// </summary>
    SystemAdmin = 0,

    /// <summary>
    /// Tenant owner - can manage billing and all tenant settings
    /// </summary>
    TenantOwner = 1,

    /// <summary>
    /// Tenant administrator - can manage users and events
    /// </summary>
    TenantAdmin = 2,

    /// <summary>
    /// Event manager - can create and manage events
    /// </summary>
    EventManager = 3,

    /// <summary>
    /// Regular user - can view events and register as attendee
    /// </summary>
    User = 4
}
