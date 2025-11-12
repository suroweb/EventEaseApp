using EventEase.Domain.Entities;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventEase.API.GraphQL.DataLoaders;

/// <summary>
/// DataLoader for batching User queries by ID to prevent N+1 queries
/// </summary>
public class UserByIdDataLoader : BatchDataLoader<Guid, User>
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public UserByIdDataLoader(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<IReadOnlyDictionary<Guid, User>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Users
            .Where(u => keys.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);
    }
}
