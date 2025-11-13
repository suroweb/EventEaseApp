using EventEase.Application.Interfaces;

namespace EventEase.Infrastructure.Tests.Data;

/// <summary>
/// Mock implementation of ICurrentTenantService for testing multi-tenancy
/// </summary>
public class MockCurrentTenantService : ICurrentTenantService
{
    public Guid TenantId { get; set; }
    public string? TenantName { get; set; }
    public bool IsSystemAdmin { get; set; }

    public MockCurrentTenantService(Guid tenantId, string? tenantName = null, bool isSystemAdmin = false)
    {
        TenantId = tenantId;
        TenantName = tenantName;
        IsSystemAdmin = isSystemAdmin;
    }
}
