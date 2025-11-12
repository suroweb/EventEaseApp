namespace EventEase.Domain.Common;

/// <summary>
/// Interface for entities that belong to a tenant (for multi-tenancy support)
/// </summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
