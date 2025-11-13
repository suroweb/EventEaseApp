# EventEase Database Migrations Guide

## Overview

EventEase uses Entity Framework Core 9.0 with PostgreSQL for database management. This guide covers how to create, apply, and manage database migrations.

## Prerequisites

- .NET 9.0 SDK installed
- PostgreSQL 15+ server running
- EF Core CLI tools installed:
  ```bash
  dotnet tool install --global dotnet-ef
  dotnet tool update --global dotnet-ef
  ```

## Quick Start

### 1. Generate Initial Migration

From the EventEase.API project directory:

```bash
cd EventEase.API
dotnet ef migrations add InitialCreate \
    --project ../EventEase.Infrastructure \
    --output-dir Data/Migrations \
    --context ApplicationDbContext
```

### 2. Apply Migration to Database

```bash
# Apply all pending migrations
dotnet ef database update \
    --project ../EventEase.Infrastructure \
    --context ApplicationDbContext

# Apply to specific migration
dotnet ef database update InitialCreate \
    --project ../EventEase.Infrastructure
```

### 3. Generate SQL Script (for production)

```bash
# Generate SQL for all migrations
dotnet ef migrations script \
    --project ../EventEase.Infrastructure \
    --output migrations.sql \
    --idempotent

# Generate SQL from specific migration to latest
dotnet ef migrations script InitialCreate \
    --project ../EventEase.Infrastructure \
    --output update.sql \
    --idempotent
```

## Migration Workflow

### Creating a New Migration

1. **Make changes to domain entities** in EventEase.Domain/Entities/
2. **Update entity configurations** (if needed) in EventEase.Infrastructure/Data/Configurations/
3. **Generate migration**:
   ```bash
   dotnet ef migrations add <MigrationName> --project ../EventEase.Infrastructure
   ```
4. **Review generated migration** in EventEase.Infrastructure/Data/Migrations/
5. **Test migration**:
   ```bash
   # Apply to development database
   dotnet ef database update

   # Test rollback
   dotnet ef database update <PreviousMigration>
   ```

### Migration Naming Convention

Use descriptive names with PascalCase:
- `InitialCreate` - Initial database schema
- `AddNotificationsTable` - Add new table
- `AddEventCategoryIndex` - Add index
- `UpdateUserEmailConstraint` - Modify constraint
- `RemoveObsoleteColumns` - Remove columns

### Rollback Migration

```bash
# List all migrations
dotnet ef migrations list --project ../EventEase.Infrastructure

# Rollback to specific migration
dotnet ef database update <MigrationName> --project ../EventEase.Infrastructure

# Rollback all migrations
dotnet ef database update 0 --project ../EventEase.Infrastructure
```

### Remove Migration

```bash
# Remove last migration (if not applied to database)
dotnet ef migrations remove --project ../EventEase.Infrastructure

# Force remove (dangerous!)
dotnet ef migrations remove --project ../EventEase.Infrastructure --force
```

## Production Deployment

### Recommended Approach: SQL Scripts

**DO NOT run `dotnet ef database update` in production!**

1. Generate idempotent SQL script:
   ```bash
   dotnet ef migrations script \
       --project ../EventEase.Infrastructure \
       --output migrations.sql \
       --idempotent
   ```

2. Review SQL script manually
3. Apply script using PostgreSQL client:
   ```bash
   psql -h production-host -U postgres -d eventease_saas -f migrations.sql
   ```

4. **OR** use a deployment pipeline:
   ```yaml
   # Azure DevOps / GitHub Actions example
   - name: Apply Database Migrations
     run: |
       dotnet ef migrations script \
           --project EventEase.Infrastructure \
           --idempotent \
           --output $(Build.ArtifactStagingDirectory)/migrations.sql
       psql -f $(Build.ArtifactStagingDirectory)/migrations.sql
   ```

### Alternative: Runtime Migrations

For development/staging only:

```csharp
// Program.cs
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        dbContext.Database.Migrate(); // Apply pending migrations
    }
}
```

## Database Schema

### Current Entities (Phase 2.0)

EventEase includes these domain entities:

1. **Core Entities**
   - `Tenant` - Multi-tenant organizations
   - `User` - Authenticated users with roles
   - `Event` - Event management
   - `EventRegistration` - Attendee tracking
   - `Guest` - Contact database with ML features
   - `Invitation` - AI-personalized invitations

2. **Financial Entities**
   - `CreditPackage` - Pricing tiers
   - `CreditTransaction` - Credit usage tracking
   - `PaymentTransaction` - Stripe payment records

3. **Budgeting**
   - `Budget` - Event budgets
   - `BudgetItem` - Budget line items

4. **AI & Notifications**
   - `AIAgentUsage` - AI usage tracking
   - `Notification` - Multi-channel notifications
   - `MobileDevice` - Push notification devices

### Key Indexes

Required indexes for performance:

```sql
-- Multi-tenancy indexes
CREATE INDEX idx_events_tenant_id ON events(tenant_id);
CREATE INDEX idx_users_tenant_id ON users(tenant_id);
CREATE INDEX idx_registrations_tenant_id ON event_registrations(tenant_id);

-- Search indexes
CREATE INDEX idx_events_start_date ON events(start_date);
CREATE INDEX idx_events_category ON events(category);
CREATE INDEX idx_events_status ON events(status);

-- Composite indexes
CREATE INDEX idx_events_tenant_status ON events(tenant_id, status);
CREATE INDEX idx_registrations_event_status ON event_registrations(event_id, status);

-- Unique constraints
CREATE UNIQUE INDEX idx_users_email ON users(email);
CREATE UNIQUE INDEX idx_guests_tenant_email ON guests(tenant_id, email);

-- pgvector index for AI embeddings (if using)
CREATE INDEX idx_guest_embedding ON guests USING ivfflat (embedding vector_cosine_ops);
```

## Multi-Tenancy Considerations

EventEase uses **row-level isolation** with `TenantId` on all tenant-scoped entities.

### Global Query Filters

EF Core automatically applies tenant filtering:

```csharp
// ApplicationDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
        {
            // Automatic: WHERE TenantId = @currentTenantId
            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(/* tenant filter */);
        }
    }
}
```

### Seed Data

Migrations should include seed data for:
- Default credit packages
- System roles
- Default configuration

Example:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Seed credit packages
    migrationBuilder.InsertData(
        table: "credit_packages",
        columns: new[] { "id", "name", "type", "price", "base_credits" },
        values: new object[,]
        {
            { Guid.NewGuid(), "Starter", 0, 99.00m, 1000 },
            { Guid.NewGuid(), "Pro", 1, 399.00m, 5000 },
            { Guid.NewGuid(), "Enterprise", 2, 1999.00m, 20000 }
        });
}
```

## Troubleshooting

### Error: "No migrations configuration found"

```bash
# Ensure you're in the correct directory
cd EventEase.API

# Specify project explicitly
dotnet ef migrations add MigrationName \
    --project ../EventEase.Infrastructure \
    --startup-project .
```

### Error: "Connection string not found"

```bash
# Set connection string via environment variable
export ConnectionStrings__DefaultConnection="Host=localhost;Database=eventease_saas;Username=postgres;Password=pass"

# Or use appsettings.Development.json
dotnet ef database update --environment Development
```

### Error: "Pending model changes"

```bash
# List pending changes
dotnet ef migrations list --project ../EventEase.Infrastructure

# Create new migration for changes
dotnet ef migrations add <MigrationName> --project ../EventEase.Infrastructure
```

### Performance Issues with Migrations

```bash
# Disable automatic transaction (for large data migrations)
dotnet ef migrations script --no-transactions
```

## Best Practices

### ✅ DO

- **Use descriptive migration names** (`AddEventCategoryIndex` not `Update1`)
- **Review generated migrations** before applying
- **Test migrations and rollbacks** in development first
- **Use idempotent SQL scripts** for production
- **Version control migrations** (commit to Git)
- **Document breaking changes** in migration comments
- **Add indexes for foreign keys** and frequently queried columns
- **Seed reference data** in migrations

### ❌ DON'T

- **Don't edit applied migrations** (create new migration instead)
- **Don't run migrations directly in production** (use SQL scripts)
- **Don't skip migrations** (maintain linear history)
- **Don't store sensitive data** in migrations
- **Don't create migrations for temporary changes**
- **Don't forget to test rollbacks**

## Database Backup Before Migrations

```bash
# PostgreSQL backup
pg_dump -h localhost -U postgres eventease_saas > backup_before_migration.sql

# Restore if needed
psql -h localhost -U postgres eventease_saas < backup_before_migration.sql
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Database Migrations

on:
  push:
    branches: [main, develop]
    paths:
      - 'EventEase.Infrastructure/Data/Migrations/**'

jobs:
  migrate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Install EF Core tools
        run: dotnet tool install --global dotnet-ef

      - name: Generate SQL script
        run: |
          cd EventEase.API
          dotnet ef migrations script \
              --project ../EventEase.Infrastructure \
              --idempotent \
              --output migrations.sql

      - name: Apply to staging database
        run: |
          psql -h ${{ secrets.STAGING_DB_HOST }} \
               -U ${{ secrets.DB_USER }} \
               -d eventease_staging \
               -f migrations.sql
        env:
          PGPASSWORD: ${{ secrets.DB_PASSWORD }}
```

## Monitoring Migration History

```sql
-- View migration history
SELECT * FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC;

-- Check last applied migration
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC
LIMIT 1;
```

## Resources

- [EF Core Migrations Documentation](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [EventEase Architecture Review](/docs/ARCHITECTURE_REVIEW.md)
- [EventEase Database Schema](/docs/DATABASE_SCHEMA.md)

---

**Last Updated**: 2025-11-13
**EF Core Version**: 9.0.0
**PostgreSQL Version**: 15+
