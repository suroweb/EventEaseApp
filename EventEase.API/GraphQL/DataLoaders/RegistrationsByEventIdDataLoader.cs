using EventEase.Domain.Entities;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventEase.API.GraphQL.DataLoaders;

/// <summary>
/// DataLoader for batching EventRegistrations by EventId to prevent N+1 queries
/// </summary>
public class RegistrationsByEventIdDataLoader : GroupedDataLoader<Guid, EventRegistration>
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public RegistrationsByEventIdDataLoader(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<ILookup<Guid, EventRegistration>> LoadGroupedBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var registrations = await dbContext.EventRegistrations
            .Where(r => keys.Contains(r.EventId))
            .ToListAsync(cancellationToken);

        return registrations.ToLookup(r => r.EventId);
    }
}
