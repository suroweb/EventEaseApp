using EventEase.Application.Interfaces;
using EventEase.Domain.Entities;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Infrastructure.Tests.Data;

/// <summary>
/// Test fixture for multi-tenancy isolation tests
/// Sets up two test tenants with sample data for comprehensive testing
/// </summary>
public class MultiTenancyTestFixture : IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    // Test Tenants
    public Tenant TenantA { get; private set; }
    public Tenant TenantB { get; private set; }

    // TenantA Data
    public User TenantA_User1 { get; private set; }
    public User TenantA_User2 { get; private set; }
    public Event TenantA_Event1 { get; private set; }
    public Event TenantA_Event2 { get; private set; }
    public Event TenantA_Event3 { get; private set; }
    public EventRegistration TenantA_Registration1 { get; private set; }
    public EventRegistration TenantA_Registration2 { get; private set; }
    public Guest TenantA_Guest1 { get; private set; }
    public Invitation TenantA_Invitation1 { get; private set; }
    public CreditTransaction TenantA_CreditTx1 { get; private set; }
    public Notification TenantA_Notification1 { get; private set; }

    // TenantB Data
    public User TenantB_User1 { get; private set; }
    public User TenantB_User2 { get; private set; }
    public Event TenantB_Event1 { get; private set; }
    public Event TenantB_Event2 { get; private set; }
    public EventRegistration TenantB_Registration1 { get; private set; }
    public Guest TenantB_Guest1 { get; private set; }
    public Invitation TenantB_Invitation1 { get; private set; }
    public CreditTransaction TenantB_CreditTx1 { get; private set; }
    public Notification TenantB_Notification1 { get; private set; }

    public MultiTenancyTestFixture()
    {
        // Use InMemory database for testing
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"MultiTenancyTestDb_{Guid.NewGuid()}")
            .Options;

        // Seed test data
        SeedTestData();
    }

    /// <summary>
    /// Creates a new DbContext with the specified tenant context
    /// </summary>
    public ApplicationDbContext CreateDbContext(Guid tenantId, bool isSystemAdmin = false)
    {
        var tenantService = new MockCurrentTenantService(tenantId, isSystemAdmin: isSystemAdmin);
        return new ApplicationDbContext(_options, tenantService);
    }

    /// <summary>
    /// Creates a DbContext without tenant context (should cause issues)
    /// </summary>
    public ApplicationDbContext CreateDbContextWithoutTenant()
    {
        return new ApplicationDbContext(_options, currentTenantService: null);
    }

    private void SeedTestData()
    {
        // Create context without tenant service for initial seeding
        using var context = new ApplicationDbContext(_options);

        // Create Tenants
        TenantA = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Tenant A Corporation",
            PrimaryContactEmail = "contact@tenanta.com",
            Status = TenantStatus.Active,
            AvailableCredits = 1000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        TenantB = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Tenant B Enterprises",
            PrimaryContactEmail = "contact@tenantb.com",
            Status = TenantStatus.Active,
            AvailableCredits = 2000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Tenants.AddRange(TenantA, TenantB);
        context.SaveChanges();

        // Seed Tenant A Data
        SeedTenantAData(context);

        // Seed Tenant B Data
        SeedTenantBData(context);

        context.SaveChanges();
    }

    private void SeedTenantAData(ApplicationDbContext context)
    {
        // Users
        TenantA_User1 = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA.Id,
            Email = "user1@tenanta.com",
            FirstName = "Alice",
            LastName = "Anderson",
            PasswordHash = "hash1",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        TenantA_User2 = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA.Id,
            Email = "user2@tenanta.com",
            FirstName = "Bob",
            LastName = "Brown",
            PasswordHash = "hash2",
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(TenantA_User1, TenantA_User2);

        // Events
        TenantA_Event1 = new Event
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA.Id,
            CreatedByUserId = TenantA_User1.Id,
            Name = "TenantA Conference 2025",
            Description = "Annual conference",
            Status = EventStatus.Published,
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(32),
            IsFree = false,
            Price = 299.99m,
            CreatedAt = DateTime.UtcNow
        };

        TenantA_Event2 = new Event
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA.Id,
            CreatedByUserId = TenantA_User1.Id,
            Name = "TenantA Workshop",
            Description = "Technical workshop",
            Status = EventStatus.Published,
            StartDate = DateTime.UtcNow.AddDays(15),
            EndDate = DateTime.UtcNow.AddDays(15),
            IsFree = true,
            CreatedAt = DateTime.UtcNow
        };

        TenantA_Event3 = new Event
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA.Id,
            CreatedByUserId = TenantA_User2.Id,
            Name = "TenantA Meetup",
            Description = "Networking event",
            Status = EventStatus.Draft,
            StartDate = DateTime.UtcNow.AddDays(45),
            EndDate = DateTime.UtcNow.AddDays(45),
            IsFree = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Events.AddRange(TenantA_Event1, TenantA_Event2, TenantA_Event3);

        // Guests
        TenantA_Guest1 = new Guest
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA.Id,
            Email = "guest1@example.com",
            FirstName = "Charlie",
            LastName = "Chen",
            Company = "Tech Corp",
            CreatedAt = DateTime.UtcNow
        };

        context.Guests.Add(TenantA_Guest1);

        // Registrations
        TenantA_Registration1 = new EventRegistration
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA.Id,
            EventId = TenantA_Event1.Id,
            UserId = TenantA_User2.Id,
            FirstName = "Bob",
            LastName = "Brown",
            Email = "user2@tenanta.com",
            Status = RegistrationStatus.Confirmed,
            RegisteredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        TenantA_Registration2 = new EventRegistration
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA.Id,
            EventId = TenantA_Event2.Id,
            FirstName = "External",
            LastName = "Guest",
            Email = "external@example.com",
            Status = RegistrationStatus.Confirmed,
            RegisteredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        context.EventRegistrations.AddRange(TenantA_Registration1, TenantA_Registration2);

        // Invitations
        TenantA_Invitation1 = new Invitation
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA.Id,
            EventId = TenantA_Event1.Id,
            GuestId = TenantA_Guest1.Id,
            SentByUserId = TenantA_User1.Id,
            Subject = "Invitation to TenantA Conference",
            Status = InvitationStatus.Sent,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        context.Invitations.Add(TenantA_Invitation1);

        // Credit Transactions
        TenantA_CreditTx1 = new CreditTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA.Id,
            UserId = TenantA_User1.Id,
            Type = CreditTransactionType.Purchase,
            Amount = 1000,
            BalanceBefore = 0,
            BalanceAfter = 1000,
            Description = "Initial credit purchase",
            CreatedAt = DateTime.UtcNow
        };

        context.CreditTransactions.Add(TenantA_CreditTx1);

        // Notifications
        TenantA_Notification1 = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = TenantA.Id,
            UserId = TenantA_User1.Id,
            Title = "Welcome to TenantA",
            Message = "Welcome message",
            Type = NotificationType.System,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Notifications.Add(TenantA_Notification1);
    }

    private void SeedTenantBData(ApplicationDbContext context)
    {
        // Users
        TenantB_User1 = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TenantB.Id,
            Email = "user1@tenantb.com",
            FirstName = "David",
            LastName = "Davis",
            PasswordHash = "hash3",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        TenantB_User2 = new User
        {
            Id = Guid.NewGuid(),
            TenantId = TenantB.Id,
            Email = "user2@tenantb.com",
            FirstName = "Emma",
            LastName = "Evans",
            PasswordHash = "hash4",
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(TenantB_User1, TenantB_User2);

        // Events
        TenantB_Event1 = new Event
        {
            Id = Guid.NewGuid(),
            TenantId = TenantB.Id,
            CreatedByUserId = TenantB_User1.Id,
            Name = "TenantB Summit 2025",
            Description = "Business summit",
            Status = EventStatus.Published,
            StartDate = DateTime.UtcNow.AddDays(20),
            EndDate = DateTime.UtcNow.AddDays(22),
            IsFree = false,
            Price = 499.99m,
            CreatedAt = DateTime.UtcNow
        };

        TenantB_Event2 = new Event
        {
            Id = Guid.NewGuid(),
            TenantId = TenantB.Id,
            CreatedByUserId = TenantB_User1.Id,
            Name = "TenantB Webinar",
            Description = "Online webinar",
            Status = EventStatus.Published,
            StartDate = DateTime.UtcNow.AddDays(10),
            EndDate = DateTime.UtcNow.AddDays(10),
            IsFree = true,
            IsVirtual = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Events.AddRange(TenantB_Event1, TenantB_Event2);

        // Guests
        TenantB_Guest1 = new Guest
        {
            Id = Guid.NewGuid(),
            TenantId = TenantB.Id,
            Email = "guest1@tenantb.com",
            FirstName = "Frank",
            LastName = "Foster",
            Company = "Business Inc",
            CreatedAt = DateTime.UtcNow
        };

        context.Guests.Add(TenantB_Guest1);

        // Registrations
        TenantB_Registration1 = new EventRegistration
        {
            Id = Guid.NewGuid(),
            TenantId = TenantB.Id,
            EventId = TenantB_Event1.Id,
            UserId = TenantB_User2.Id,
            FirstName = "Emma",
            LastName = "Evans",
            Email = "user2@tenantb.com",
            Status = RegistrationStatus.Confirmed,
            RegisteredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        context.EventRegistrations.Add(TenantB_Registration1);

        // Invitations
        TenantB_Invitation1 = new Invitation
        {
            Id = Guid.NewGuid(),
            TenantId = TenantB.Id,
            EventId = TenantB_Event1.Id,
            GuestId = TenantB_Guest1.Id,
            SentByUserId = TenantB_User1.Id,
            Subject = "Invitation to TenantB Summit",
            Status = InvitationStatus.Sent,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        context.Invitations.Add(TenantB_Invitation1);

        // Credit Transactions
        TenantB_CreditTx1 = new CreditTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = TenantB.Id,
            UserId = TenantB_User1.Id,
            Type = CreditTransactionType.Purchase,
            Amount = 2000,
            BalanceBefore = 0,
            BalanceAfter = 2000,
            Description = "Initial credit purchase",
            CreatedAt = DateTime.UtcNow
        };

        context.CreditTransactions.Add(TenantB_CreditTx1);

        // Notifications
        TenantB_Notification1 = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = TenantB.Id,
            UserId = TenantB_User1.Id,
            Title = "Welcome to TenantB",
            Message = "Welcome message",
            Type = NotificationType.System,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Notifications.Add(TenantB_Notification1);
    }

    public void Dispose()
    {
        // Cleanup if needed
        using var context = new ApplicationDbContext(_options);
        context.Database.EnsureDeleted();
    }
}
