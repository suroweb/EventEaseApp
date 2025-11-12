namespace EventEase.Application.Interfaces;

/// <summary>
/// Service to provide the current tenant context
/// </summary>
public interface ICurrentTenantService
{
    Guid TenantId { get; }
    string? TenantName { get; }
    bool IsSystemAdmin { get; }
}
