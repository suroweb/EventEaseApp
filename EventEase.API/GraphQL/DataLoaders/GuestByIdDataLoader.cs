using EventEase.Domain.Entities;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventEase.API.GraphQL.DataLoaders;

/// <summary>
/// DataLoader for batching Guest queries by ID to prevent N+1 queries
/// </summary>
public class GuestByIdDataLoader : BatchDataLoader<Guid, Guest>
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public GuestByIdDataLoader(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<IReadOnlyDictionary<Guid, Guest>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Guests
            .Where(g => keys.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, cancellationToken);
    }
}
