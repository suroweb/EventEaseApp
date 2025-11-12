using System.Security.Claims;
using EventEase.Application.Interfaces;
using EventEase.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace EventEase.Infrastructure.Services;

/// <summary>
/// Service to provide current tenant context from HTTP context
/// </summary>
public class CurrentTenantService : ICurrentTenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId
    {
        get
        {
            var tenantIdClaim = _httpContextAccessor.HttpContext?.User
                ?.FindFirst("tenant_id")?.Value;

            if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                return tenantId;
            }

            return Guid.Empty;
        }
    }

    public string? TenantName => _httpContextAccessor.HttpContext?.User
        ?.FindFirst("tenant_name")?.Value;

    public bool IsSystemAdmin
    {
        get
        {
            var role = _httpContextAccessor.HttpContext?.User
                ?.FindFirst(ClaimTypes.Role)?.Value;

            return role == UserRole.SystemAdmin.ToString();
        }
    }
}
