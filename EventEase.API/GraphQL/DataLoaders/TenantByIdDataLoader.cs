using EventEase.Domain.Entities;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventEase.API.GraphQL.DataLoaders;

/// <summary>
/// DataLoader for batching Tenant queries by ID to prevent N+1 queries
/// </summary>
public class TenantByIdDataLoader : BatchDataLoader<Guid, Tenant>
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public TenantByIdDataLoader(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<IReadOnlyDictionary<Guid, Tenant>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Tenants
            .Where(t => keys.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);
    }
}
