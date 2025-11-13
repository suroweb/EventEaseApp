using EventEase.Application.Tests.Helpers;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Application.Tests.Services;

/// <summary>
/// Comprehensive tests for AuthenticationService covering all security scenarios
/// </summary>
public class AuthenticationServiceTests : IDisposable
{
    private readonly TestDatabaseFixture _fixture;

    public AuthenticationServiceTests()
    {
        _fixture = new TestDatabaseFixture();
    }

    #region Registration Tests

    [Fact]
    public async Task RegisterTenant_ValidData_CreatesTenantAndOwner()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Act
        var result = await service.RegisterTenantAsync(
            "Acme Corp",
            "owner@acme.com",
            "SecurePass123",
            "John",
            "Doe",
            "+1234567890");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Success.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();

        // Verify tenant was created
        var tenant = await context.Tenants.FirstOrDefaultAsync();
        tenant.Should().NotBeNull();
        tenant!.Name.Should().Be("Acme Corp");
        tenant.PrimaryContactEmail.Should().Be("owner@acme.com");
        tenant.Status.Should().Be(TenantStatus.Trial);
        tenant.IsActive.Should().BeTrue();

        // Verify owner user was created
        var user = await context.Users.FirstOrDefaultAsync();
        user.Should().NotBeNull();
        user!.Email.Should().Be("owner@acme.com");
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.Role.Should().Be(UserRole.TenantOwner);
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterTenant_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Create existing user
        var existingTenant = TestDataBuilder.CreateTenant()
            .WithEmail("owner@acme.com")
            .Build();
        var existingUser = TestDataBuilder.CreateUser()
            .WithEmail("owner@acme.com")
            .WithTenantId(existingTenant.Id)
            .Build();

        context.Tenants.Add(existingTenant);
        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        // Act
        var result = await service.RegisterTenantAsync(
            "Another Corp",
            "owner@acme.com",
            "SecurePass123",
            "Jane",
            "Smith");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already registered");
    }

    [Fact]
    public async Task RegisterTenant_CreatesTrialCredits()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Act
        var result = await service.RegisterTenantAsync(
            "Acme Corp",
            "owner@acme.com",
            "SecurePass123",
            "John",
            "Doe");

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify trial credits
        var tenant = await context.Tenants.FirstOrDefaultAsync();
        tenant.Should().NotBeNull();
        tenant!.AvailableCredits.Should().Be(100);
        tenant.TrialEndsAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(14), TimeSpan.FromSeconds(5));

        // Verify credit transaction
        var creditTransaction = await context.CreditTransactions.FirstOrDefaultAsync();
        creditTransaction.Should().NotBeNull();
        creditTransaction!.Type.Should().Be(CreditTransactionType.Bonus);
        creditTransaction.Amount.Should().Be(100);
        creditTransaction.BalanceBefore.Should().Be(0);
        creditTransaction.BalanceAfter.Should().Be(100);
    }

    [Theory]
    [InlineData("", "owner@acme.com", "SecurePass123", "Company name is required")]
    [InlineData("Acme Corp", "", "SecurePass123", "Email is required")]
    [InlineData("Acme Corp", "owner@acme.com", "", "Password is required")]
    [InlineData("Acme Corp", "owner@acme.com", "short", "at least 8 characters")]
    public async Task RegisterTenant_InvalidInput_ReturnsValidationError(
        string companyName,
        string email,
        string password,
        string expectedError)
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Act
        var result = await service.RegisterTenantAsync(
            companyName,
            email,
            password,
            "John",
            "Doe");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain(expectedError);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Create user
        var tenant = TestDataBuilder.CreateTenant().Build();
        var user = TestDataBuilder.CreateUser()
            .WithTenantId(tenant.Id)
            .WithEmail("user@example.com")
            .WithPasswordHash("hashed_Password123")
            .Build();

        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.LoginAsync("user@example.com", "Password123");

        // Assert
        result.Success.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.ExpiresAt.Should().NotBeNull();

        // Verify user state was updated
        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        updatedUser.FailedLoginAttempts.Should().Be(0);
        updatedUser.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidPassword_IncrementsFailedAttempts()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Create user
        var tenant = TestDataBuilder.CreateTenant().Build();
        var user = TestDataBuilder.CreateUser()
            .WithTenantId(tenant.Id)
            .WithEmail("user@example.com")
            .WithPasswordHash("hashed_CorrectPassword")
            .Build();

        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.LoginAsync("user@example.com", "WrongPassword");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Invalid email or password");

        // Verify failed attempts incremented
        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Login_FiveFailedAttempts_LocksAccount()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Create user with 4 failed attempts
        var tenant = TestDataBuilder.CreateTenant().Build();
        var user = TestDataBuilder.CreateUser()
            .WithTenantId(tenant.Id)
            .WithEmail("user@example.com")
            .WithPasswordHash("hashed_CorrectPassword")
            .WithFailedLoginAttempts(4)
            .Build();

        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act - Fifth failed attempt
        var result = await service.LoginAsync("user@example.com", "WrongPassword");

        // Assert
        result.Success.Should().BeFalse();

        // Verify account is locked
        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.FailedLoginAttempts.Should().Be(5);
        updatedUser.LockedUntil.Should().NotBeNull();
        updatedUser.LockedUntil.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Login_LockedAccount_ReturnsError()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Create locked user
        var tenant = TestDataBuilder.CreateTenant().Build();
        var user = TestDataBuilder.CreateUser()
            .WithTenantId(tenant.Id)
            .WithEmail("user@example.com")
            .WithPasswordHash("hashed_CorrectPassword")
            .AsLocked(DateTime.UtcNow.AddMinutes(30))
            .Build();

        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.LoginAsync("user@example.com", "CorrectPassword");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Account is locked");
    }

    [Fact]
    public async Task Login_SuccessfulAfterFailed_ResetsCounter()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Create user with failed attempts
        var tenant = TestDataBuilder.CreateTenant().Build();
        var user = TestDataBuilder.CreateUser()
            .WithTenantId(tenant.Id)
            .WithEmail("user@example.com")
            .WithPasswordHash("hashed_CorrectPassword")
            .WithFailedLoginAttempts(3)
            .Build();

        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.LoginAsync("user@example.com", "CorrectPassword");

        // Assert
        result.Success.Should().BeTrue();

        // Verify counter was reset
        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.FailedLoginAttempts.Should().Be(0);
        updatedUser.LockedUntil.Should().BeNull();
    }

    [Fact]
    public async Task Login_InactiveUser_ReturnsError()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Create inactive user
        var tenant = TestDataBuilder.CreateTenant().Build();
        var user = TestDataBuilder.CreateUser()
            .WithTenantId(tenant.Id)
            .WithEmail("user@example.com")
            .WithPasswordHash("hashed_Password123")
            .AsInactive()
            .Build();

        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.LoginAsync("user@example.com", "Password123");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("deactivated");
    }

    [Fact]
    public async Task Login_InactiveTenant_ReturnsError()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Create user with inactive tenant
        var tenant = TestDataBuilder.CreateTenant()
            .AsInactive()
            .Build();
        var user = TestDataBuilder.CreateUser()
            .WithTenantId(tenant.Id)
            .WithEmail("user@example.com")
            .WithPasswordHash("hashed_Password123")
            .Build();

        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.LoginAsync("user@example.com", "Password123");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("organization's account is inactive");
    }

    [Fact]
    public async Task Login_NonExistentEmail_ReturnsError()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Act
        var result = await service.LoginAsync("nonexistent@example.com", "Password123");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Invalid email or password");
    }

    #endregion

    #region Refresh Token Tests

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Create user with valid refresh token
        var tenant = TestDataBuilder.CreateTenant().Build();
        var user = TestDataBuilder.CreateUser()
            .WithTenantId(tenant.Id)
            .WithRefreshToken("valid_refresh_token", DateTime.UtcNow.AddDays(7))
            .Build();

        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.RefreshTokenAsync("valid_refresh_token");

        // Assert
        result.Success.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe("valid_refresh_token"); // Should be a new token

        // Verify refresh token was updated
        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.RefreshToken.Should().NotBe("valid_refresh_token");
    }

    [Fact]
    public async Task RefreshToken_ExpiredToken_ReturnsFailure()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Create user with expired refresh token
        var tenant = TestDataBuilder.CreateTenant().Build();
        var user = TestDataBuilder.CreateUser()
            .WithTenantId(tenant.Id)
            .WithRefreshToken("expired_token", DateTime.UtcNow.AddDays(-1))
            .Build();

        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.RefreshTokenAsync("expired_token");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("expired");
    }

    [Fact]
    public async Task RefreshToken_InvalidToken_ReturnsFailure()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Act
        var result = await service.RefreshTokenAsync("invalid_token");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Invalid refresh token");
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_ValidUser_InvalidatesRefreshToken()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Create user with refresh token
        var tenant = TestDataBuilder.CreateTenant().Build();
        var user = TestDataBuilder.CreateUser()
            .WithTenantId(tenant.Id)
            .WithRefreshToken("active_token", DateTime.UtcNow.AddDays(7))
            .Build();

        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.LogoutAsync(user.Id);

        // Assert
        result.Should().BeTrue();

        // Verify refresh token was invalidated
        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.RefreshToken.Should().BeNull();
        updatedUser.RefreshTokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Logout_NonExistentUser_ReturnsFalse()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Act
        var result = await service.LogoutAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Password Reset Tests

    [Fact]
    public async Task ForgotPassword_ValidEmail_GeneratesResetToken()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Create user
        var tenant = TestDataBuilder.CreateTenant().Build();
        var user = TestDataBuilder.CreateUser()
            .WithTenantId(tenant.Id)
            .WithEmail("user@example.com")
            .Build();

        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.ForgotPasswordAsync("user@example.com");

        // Assert
        result.Should().BeTrue();

        // Verify reset token was generated
        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.PasswordResetToken.Should().NotBeNullOrEmpty();
        updatedUser.PasswordResetTokenExpiresAt.Should().NotBeNull();
        updatedUser.PasswordResetTokenExpiresAt.Should().BeCloseTo(
            DateTime.UtcNow.AddHours(1),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ForgotPassword_InvalidEmail_DoesNotRevealExistence()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Act
        var result = await service.ForgotPasswordAsync("nonexistent@example.com");

        // Assert
        // Should return true to prevent email enumeration
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPassword_ValidToken_UpdatesPassword()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Create user with reset token
        var tenant = TestDataBuilder.CreateTenant().Build();
        var user = TestDataBuilder.CreateUser()
            .WithTenantId(tenant.Id)
            .WithEmail("user@example.com")
            .WithPasswordHash("hashed_OldPassword")
            .WithPasswordResetToken("valid_reset_token", DateTime.UtcNow.AddHours(1))
            .Build();

        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.ResetPasswordAsync(
            "user@example.com",
            "valid_reset_token",
            "NewPassword123");

        // Assert
        result.Should().BeTrue();

        // Verify password was updated and token cleared
        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.PasswordHash.Should().Be("hashed_NewPassword123");
        updatedUser.PasswordResetToken.Should().BeNull();
        updatedUser.PasswordResetTokenExpiresAt.Should().BeNull();
        updatedUser.FailedLoginAttempts.Should().Be(0);
        updatedUser.LockedUntil.Should().BeNull();
        updatedUser.RefreshToken.Should().BeNull(); // Should be invalidated for security
    }

    [Fact]
    public async Task ResetPassword_ExpiredToken_ReturnsFailure()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Create user with expired reset token
        var tenant = TestDataBuilder.CreateTenant().Build();
        var user = TestDataBuilder.CreateUser()
            .WithTenantId(tenant.Id)
            .WithEmail("user@example.com")
            .WithPasswordResetToken("expired_token", DateTime.UtcNow.AddHours(-1))
            .Build();

        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.ResetPasswordAsync(
            "user@example.com",
            "expired_token",
            "NewPassword123");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsFailure()
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Create user with reset token
        var tenant = TestDataBuilder.CreateTenant().Build();
        var user = TestDataBuilder.CreateUser()
            .WithTenantId(tenant.Id)
            .WithEmail("user@example.com")
            .WithPasswordResetToken("valid_token", DateTime.UtcNow.AddHours(1))
            .Build();

        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.ResetPasswordAsync(
            "user@example.com",
            "wrong_token",
            "NewPassword123");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public async Task ResetPassword_InvalidNewPassword_ReturnsFailure(string newPassword)
    {
        // Arrange
        var context = _fixture.CreateContext();
        var passwordHasher = MockServiceFactory.CreatePasswordHasher();
        var tokenService = MockServiceFactory.CreateTokenService();
        var logger = MockServiceFactory.CreateLogger<AuthenticationService>();

        var service = new AuthenticationService(
            context,
            passwordHasher.Object,
            tokenService.Object,
            logger.Object);

        // Create user with reset token
        var tenant = TestDataBuilder.CreateTenant().Build();
        var user = TestDataBuilder.CreateUser()
            .WithTenantId(tenant.Id)
            .WithEmail("user@example.com")
            .WithPasswordResetToken("valid_token", DateTime.UtcNow.AddHours(1))
            .Build();

        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.ResetPasswordAsync(
            "user@example.com",
            "valid_token",
            newPassword);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
