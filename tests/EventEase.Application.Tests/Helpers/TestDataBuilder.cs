using EventEase.Domain.Entities;
using EventEase.Domain.Enums;

namespace EventEase.Application.Tests.Helpers;

/// <summary>
/// Fluent builder for creating test data entities
/// </summary>
public class TestDataBuilder
{
    /// <summary>
    /// Creates a tenant builder
    /// </summary>
    public static TenantBuilder CreateTenant() => new();

    /// <summary>
    /// Creates a user builder
    /// </summary>
    public static UserBuilder CreateUser() => new();

    /// <summary>
    /// Creates an event builder
    /// </summary>
    public static EventBuilder CreateEvent() => new();
}

public class TenantBuilder
{
    private readonly Tenant _tenant;

    public TenantBuilder()
    {
        _tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Company",
            PrimaryContactEmail = "test@example.com",
            Status = TenantStatus.Trial,
            TrialEndsAt = DateTime.UtcNow.AddDays(14),
            AvailableCredits = 100,
            TotalCreditsPurchased = 0,
            TotalCreditsUsed = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public TenantBuilder WithId(Guid id)
    {
        _tenant.Id = id;
        return this;
    }

    public TenantBuilder WithName(string name)
    {
        _tenant.Name = name;
        return this;
    }

    public TenantBuilder WithEmail(string email)
    {
        _tenant.PrimaryContactEmail = email;
        return this;
    }

    public TenantBuilder WithStatus(TenantStatus status)
    {
        _tenant.Status = status;
        return this;
    }

    public TenantBuilder WithCredits(int credits)
    {
        _tenant.AvailableCredits = credits;
        return this;
    }

    public TenantBuilder AsInactive()
    {
        _tenant.IsActive = false;
        return this;
    }

    public TenantBuilder WithTrialEnding(DateTime trialEndsAt)
    {
        _tenant.TrialEndsAt = trialEndsAt;
        return this;
    }

    public Tenant Build() => _tenant;
}

public class UserBuilder
{
    private readonly User _user;

    public UserBuilder()
    {
        _user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Email = "testuser@example.com",
            PasswordHash = "hashed_password",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.EventManager,
            IsActive = true,
            EmailConfirmed = true,
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public UserBuilder WithId(Guid id)
    {
        _user.Id = id;
        return this;
    }

    public UserBuilder WithTenantId(Guid tenantId)
    {
        _user.TenantId = tenantId;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _user.Email = email;
        return this;
    }

    public UserBuilder WithPasswordHash(string passwordHash)
    {
        _user.PasswordHash = passwordHash;
        return this;
    }

    public UserBuilder WithName(string firstName, string lastName)
    {
        _user.FirstName = firstName;
        _user.LastName = lastName;
        return this;
    }

    public UserBuilder WithRole(UserRole role)
    {
        _user.Role = role;
        return this;
    }

    public UserBuilder AsInactive()
    {
        _user.IsActive = false;
        return this;
    }

    public UserBuilder WithRefreshToken(string token, DateTime expiresAt)
    {
        _user.RefreshToken = token;
        _user.RefreshTokenExpiresAt = expiresAt;
        return this;
    }

    public UserBuilder WithFailedLoginAttempts(int attempts)
    {
        _user.FailedLoginAttempts = attempts;
        return this;
    }

    public UserBuilder AsLocked(DateTime? lockedUntil = null)
    {
        _user.LockedUntil = lockedUntil ?? DateTime.UtcNow.AddMinutes(30);
        return this;
    }

    public UserBuilder WithPasswordResetToken(string token, DateTime? expiresAt = null)
    {
        _user.PasswordResetToken = token;
        _user.PasswordResetTokenExpiresAt = expiresAt ?? DateTime.UtcNow.AddHours(1);
        return this;
    }

    public User Build() => _user;
}

public class EventBuilder
{
    private readonly Event _event;

    public EventBuilder()
    {
        _event = new Event
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Test Event",
            Description = "Test event description",
            Location = "Test Location",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(2),
            Status = EventStatus.Draft,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public EventBuilder WithId(Guid id)
    {
        _event.Id = id;
        return this;
    }

    public EventBuilder WithTenantId(Guid tenantId)
    {
        _event.TenantId = tenantId;
        return this;
    }

    public EventBuilder WithName(string name)
    {
        _event.Name = name;
        return this;
    }

    public EventBuilder WithDates(DateTime startDate, DateTime endDate)
    {
        _event.StartDate = startDate;
        _event.EndDate = endDate;
        return this;
    }

    public EventBuilder WithStatus(EventStatus status)
    {
        _event.Status = status;
        return this;
    }

    public Event Build() => _event;
}
