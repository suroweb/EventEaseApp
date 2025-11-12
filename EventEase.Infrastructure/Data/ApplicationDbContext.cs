using EventEase.Application.Interfaces;
using EventEase.Domain.Common;
using EventEase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Infrastructure.Data;

/// <summary>
/// Main application database context with multi-tenancy support
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentTenantService? _currentTenantService;
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentTenantService? currentTenantService = null,
        ICurrentUserService? currentUserService = null) : base(options)
    {
        _currentTenantService = currentTenantService;
        _currentUserService = currentUserService;
    }

    // Core Entities
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();

    // Event Management
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();

    // Guest & Invitation Management
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    // Credits & Payments
    public DbSet<CreditPackage> CreditPackages => Set<CreditPackage>();
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    // AI Agents
    public DbSet<AIAgentUsage> AIAgentUsages => Set<AIAgentUsage>();

    // Budget Management
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetItem> BudgetItems => Set<BudgetItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure multi-tenancy global query filters
        ConfigureGlobalQueryFilters(modelBuilder);

        // Configure PostgreSQL specific features
        ConfigurePostgreSQLFeatures(modelBuilder);
    }

    private void ConfigureGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        // Apply tenant filter to all entities that implement ITenantEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(SetTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?
                    .MakeGenericMethod(entityType.ClrType);

                var filter = method?.Invoke(null, new object[] { this });
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter!);
            }
        }
    }

    private static System.Linq.Expressions.LambdaExpression SetTenantFilter<TEntity>(ApplicationDbContext context)
        where TEntity : class, ITenantEntity
    {
        System.Linq.Expressions.Expression<Func<TEntity, bool>> filter = e =>
            context._currentTenantService == null ||
            e.TenantId == context._currentTenantService.TenantId;
        return filter;
    }

    private void ConfigurePostgreSQLFeatures(ModelBuilder modelBuilder)
    {
        // Enable pgvector extension for AI embeddings (if needed in future)
        // modelBuilder.HasPostgresExtension("vector");

        // Configure database collation for case-insensitive searches
        modelBuilder.UseCollation("en_US.utf8");
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Set audit fields before saving
        SetAuditFields();

        // Set TenantId for new tenant entities
        SetTenantId();

        return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditFields()
    {
        var entries = ChangeTracker.Entries<BaseAuditableEntity>();

        foreach (var entry in entries)
        {
            var currentUser = _currentUserService?.UserId.ToString();

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = currentUser;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = currentUser;
                    break;
            }
        }
    }

    private void SetTenantId()
    {
        if (_currentTenantService == null || _currentTenantService.TenantId == Guid.Empty)
            return;

        var entries = ChangeTracker.Entries<ITenantEntity>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            entry.Entity.TenantId = _currentTenantService.TenantId;
        }
    }
}
