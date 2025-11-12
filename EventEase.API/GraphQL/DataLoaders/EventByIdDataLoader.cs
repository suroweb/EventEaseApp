using EventEase.Domain.Entities;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventEase.API.GraphQL.DataLoaders;

/// <summary>
/// DataLoader for batching Event queries by ID to prevent N+1 queries
/// </summary>
public class EventByIdDataLoader : BatchDataLoader<Guid, Event>
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public EventByIdDataLoader(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<IReadOnlyDictionary<Guid, Event>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Events
            .Where(e => keys.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken);
    }
}
