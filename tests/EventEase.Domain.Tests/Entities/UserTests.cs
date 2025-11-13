using EventEase.Domain.Entities;
using EventEase.Domain.Enums;

namespace EventEase.Domain.Tests.Entities;

/// <summary>
/// Tests for User entity domain logic
/// </summary>
public class UserTests
{
    [Fact]
    public void User_Creation_SetsDefaultValues()
    {
        // Arrange & Act
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.EventManager
        };

        // Assert
        user.Id.Should().NotBeEmpty();
        user.Email.Should().Be("test@example.com");
        user.Role.Should().Be(UserRole.EventManager);
        user.FailedLoginAttempts.Should().Be(0);
        user.IsActive.Should().BeFalse(); // Default value
    }

    [Fact]
    public void User_FullName_CombinesFirstAndLastName()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act & Assert
        user.FullName.Should().Be("John Doe");
    }

    [Theory]
    [InlineData(UserRole.TenantOwner)]
    [InlineData(UserRole.EventManager)]
    [InlineData(UserRole.Staff)]
    [InlineData(UserRole.Guest)]
    public void User_CanHaveDifferentRoles(UserRole role)
    {
        // Arrange & Act
        var user = new User
        {
            Role = role
        };

        // Assert
        user.Role.Should().Be(role);
    }

    [Fact]
    public void User_AccountLockout_CanBeSetAndCleared()
    {
        // Arrange
        var user = new User();
        var lockoutTime = DateTime.UtcNow.AddMinutes(30);

        // Act
        user.LockedUntil = lockoutTime;

        // Assert
        user.LockedUntil.Should().Be(lockoutTime);

        // Clear lockout
        user.LockedUntil = null;
        user.LockedUntil.Should().BeNull();
    }

    [Fact]
    public void User_FailedLoginAttempts_CanBeIncremented()
    {
        // Arrange
        var user = new User
        {
            FailedLoginAttempts = 0
        };

        // Act
        user.FailedLoginAttempts++;
        user.FailedLoginAttempts++;
        user.FailedLoginAttempts++;

        // Assert
        user.FailedLoginAttempts.Should().Be(3);
    }
}
