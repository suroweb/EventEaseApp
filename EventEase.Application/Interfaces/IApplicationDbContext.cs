using Microsoft.EntityFrameworkCore;

namespace EventEase.Application.Interfaces;

/// <summary>
/// Application database context interface for dependency inversion
/// </summary>
public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
