using EventEase.Application.Interfaces;
using EventEase.Domain.Entities;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EventEase.Infrastructure.Tests.Data;

/// <summary>
/// Tests for ApplicationDbContext multi-tenancy features
/// </summary>
public class ApplicationDbContextTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Guid _tenant1Id = Guid.NewGuid();
    private readonly Guid _tenant2Id = Guid.NewGuid();

    public ApplicationDbContextTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockTenantService = new Mock<ICurrentTenantService>();
        mockTenantService.Setup(x => x.TenantId).Returns(_tenant1Id);

        var mockUserService = new Mock<ICurrentUserService>();
        mockUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());

        _context = new ApplicationDbContext(options, mockTenantService.Object, mockUserService.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create two tenants
        var tenant1 = new Tenant
        {
            Id = _tenant1Id,
            Name = "Tenant 1",
            PrimaryContactEmail = "tenant1@example.com",
            Status = TenantStatus.Active,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var tenant2 = new Tenant
        {
            Id = _tenant2Id,
            Name = "Tenant 2",
            PrimaryContactEmail = "tenant2@example.com",
            Status = TenantStatus.Active,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Tenants.AddRange(tenant1, tenant2);

        // Create events for each tenant
        var event1 = new Event
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant1Id,
            Name = "Tenant 1 Event",
            Description = "Event for tenant 1",
            Location = "Location 1",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(2),
            Status = EventStatus.Published,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var event2 = new Event
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant2Id,
            Name = "Tenant 2 Event",
            Description = "Event for tenant 2",
            Location = "Location 2",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(2),
            Status = EventStatus.Published,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Events.AddRange(event1, event2);
        _context.SaveChanges();
    }

    [Fact]
    public void Context_CanCreateDatabase()
    {
        // Arrange & Act
        var canConnect = _context.Database.CanConnect();

        // Assert
        canConnect.Should().BeTrue();
    }

    [Fact]
    public void Context_HasAllRequiredDbSets()
    {
        // Assert
        _context.Tenants.Should().NotBeNull();
        _context.Users.Should().NotBeNull();
        _context.Events.Should().NotBeNull();
        _context.EventRegistrations.Should().NotBeNull();
        _context.Guests.Should().NotBeNull();
        _context.Invitations.Should().NotBeNull();
        _context.CreditPackages.Should().NotBeNull();
        _context.CreditTransactions.Should().NotBeNull();
        _context.PaymentTransactions.Should().NotBeNull();
        _context.AIAgentUsages.Should().NotBeNull();
        _context.Budgets.Should().NotBeNull();
        _context.BudgetItems.Should().NotBeNull();
    }

    [Fact]
    public async Task MultiTenancy_QueriesFilterByCurrentTenant()
    {
        // Act - Query events (should only return tenant 1's events)
        var events = await _context.Events.ToListAsync();

        // Assert
        events.Should().HaveCount(1);
        events.All(e => e.TenantId == _tenant1Id).Should().BeTrue();
    }

    [Fact]
    public async Task MultiTenancy_CanQueryAllTenantsDirectly()
    {
        // Act - Query tenants (not filtered)
        var tenants = await _context.Tenants.ToListAsync();

        // Assert
        tenants.Should().HaveCount(2);
    }

    [Fact]
    public async Task Context_CanAddAndRetrieveEntities()
    {
        // Arrange
        var newEvent = new Event
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant1Id,
            Name = "New Event",
            Description = "Test event",
            Location = "Test Location",
            StartDate = DateTime.UtcNow.AddDays(45),
            EndDate = DateTime.UtcNow.AddDays(45).AddHours(3),
            Status = EventStatus.Draft,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Events.FindAsync(newEvent.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("New Event");
    }

    [Fact]
    public async Task Context_CanUpdateEntities()
    {
        // Arrange
        var eventToUpdate = await _context.Events.FirstAsync();
        var originalName = eventToUpdate.Name;

        // Act
        eventToUpdate.Name = "Updated Event Name";
        await _context.SaveChangesAsync();

        // Reload from database
        var updated = await _context.Events.FindAsync(eventToUpdate.Id);

        // Assert
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Event Name");
        updated.Name.Should().NotBe(originalName);
    }

    [Fact]
    public async Task Context_CanDeleteEntities()
    {
        // Arrange
        var eventToDelete = await _context.Events.FirstAsync();
        var eventId = eventToDelete.Id;

        // Act
        _context.Events.Remove(eventToDelete);
        await _context.SaveChangesAsync();

        var deleted = await _context.Events.FindAsync(eventId);

        // Assert
        deleted.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
