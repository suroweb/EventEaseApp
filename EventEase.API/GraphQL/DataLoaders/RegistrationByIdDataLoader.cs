using EventEase.Domain.Entities;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventEase.API.GraphQL.DataLoaders;

/// <summary>
/// DataLoader for batching EventRegistration queries by ID to prevent N+1 queries
/// </summary>
public class RegistrationByIdDataLoader : BatchDataLoader<Guid, EventRegistration>
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public RegistrationByIdDataLoader(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<IReadOnlyDictionary<Guid, EventRegistration>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.EventRegistrations
            .Where(r => keys.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);
    }
}
