using EventEase.Application.Common;
using EventEase.Application.Interfaces;
using EventEase.Domain.Entities;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventEase.Infrastructure.Services;

/// <summary>
/// Authentication service handling registration, login, and token management
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthenticationService> _logger;

    // Trial credits configuration
    private const int TrialCredits = 100;
    private const int TrialDays = 14;

    public AuthenticationService(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<AuthenticationService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new tenant with an owner user (tenant onboarding)
    /// </summary>
    public async Task<Result<AuthenticationResult>> RegisterTenantAsync(
        string companyName,
        string email,
        string password,
        string firstName,
        string lastName,
        string? phoneNumber = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(companyName))
                return Result.Failure<AuthenticationResult>("Company name is required");

            if (string.IsNullOrWhiteSpace(email))
                return Result.Failure<AuthenticationResult>("Email is required");

            if (string.IsNullOrWhiteSpace(password))
                return Result.Failure<AuthenticationResult>("Password is required");

            if (password.Length < 8)
                return Result.Failure<AuthenticationResult>("Password must be at least 8 characters long");

            // Check if email is already registered
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (existingUser != null)
                return Result.Failure<AuthenticationResult>("Email is already registered");

            // Start transaction
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Create tenant
                var tenant = new Tenant
                {
                    Name = companyName,
                    PrimaryContactEmail = email,
                    PrimaryContactPhone = phoneNumber,
                    Status = TenantStatus.Trial,
                    TrialEndsAt = DateTime.UtcNow.AddDays(TrialDays),
                    AvailableCredits = TrialCredits,
                    TotalCreditsPurchased = 0,
                    TotalCreditsUsed = 0,
                    IsActive = true
                };

                _context.Tenants.Add(tenant);
                await _context.SaveChangesAsync(cancellationToken);

                // Create owner user
                var user = new User
                {
                    TenantId = tenant.Id,
                    Email = email,
                    PasswordHash = _passwordHasher.HashPassword(password),
                    FirstName = firstName,
                    LastName = lastName,
                    PhoneNumber = phoneNumber,
                    Role = UserRole.TenantOwner,
                    IsActive = true,
                    EmailConfirmed = false // TODO: Send confirmation email
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync(cancellationToken);

                // Create welcome bonus credit transaction
                var creditTransaction = new CreditTransaction
                {
                    TenantId = tenant.Id,
                    UserId = user.Id,
                    Type = CreditTransactionType.Bonus,
                    Amount = TrialCredits,
                    BalanceBefore = 0,
                    BalanceAfter = TrialCredits,
                    Description = $"Welcome bonus: {TrialDays}-day trial with {TrialCredits} free credits",
                    ExpiresAt = tenant.TrialEndsAt
                };

                _context.CreditTransactions.Add(creditTransaction);
                await _context.SaveChangesAsync(cancellationToken);

                // Generate tokens
                var accessToken = _tokenService.GenerateAccessToken(
                    user.Id,
                    tenant.Id,
                    user.Email,
                    user.Role.ToString()
                );

                var refreshToken = _tokenService.GenerateRefreshToken();

                // Save refresh token
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("New tenant registered: {TenantName} ({TenantId}) with owner {Email}",
                    tenant.Name, tenant.Id, user.Email);

                var authResult = AuthenticationResult.SuccessResult(
                    accessToken,
                    refreshToken,
                    DateTime.UtcNow.AddMinutes(60) // TODO: Get from config
                );

                return Result.Success(authResult);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during tenant registration for email {Email}", email);
            return Result.Failure<AuthenticationResult>("An error occurred during registration. Please try again.");
        }
    }

    /// <summary>
    /// Authenticates a user and returns JWT tokens
    /// </summary>
    public async Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Find user by email
            var user = await _context.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", email);
                return AuthenticationResult.Failure("Invalid email or password");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive user: {Email}", email);
                return AuthenticationResult.Failure("Account is deactivated. Please contact support.");
            }

            // Check if tenant is active
            if (!user.Tenant.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive tenant: {TenantId}", user.TenantId);
                return AuthenticationResult.Failure("Your organization's account is inactive. Please contact support.");
            }

            // Check account lockout
            if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
            {
                var remainingMinutes = (user.LockedUntil.Value - DateTime.UtcNow).TotalMinutes;
                _logger.LogWarning("Login attempt for locked account: {Email}", email);
                return AuthenticationResult.Failure($"Account is locked. Try again in {Math.Ceiling(remainingMinutes)} minutes.");
            }

            // Verify password
            if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
            {
                // Increment failed login attempts
                user.FailedLoginAttempts++;

                // Lock account after 5 failed attempts
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                    _logger.LogWarning("Account locked due to failed login attempts: {Email}", email);
                }

                await _context.SaveChangesAsync(cancellationToken);

                return AuthenticationResult.Failure("Invalid email or password");
            }

            // Reset failed login attempts on successful login
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            user.LastLoginAt = DateTime.UtcNow;

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(
                user.Id,
                user.TenantId,
                user.Email,
                user.Role.ToString()
            );

            var refreshToken = _tokenService.GenerateRefreshToken();

            // Save refresh token
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User logged in successfully: {Email} ({UserId})", email, user.Id);

            return AuthenticationResult.SuccessResult(
                accessToken,
                refreshToken,
                DateTime.UtcNow.AddMinutes(60) // TODO: Get from config
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email {Email}", email);
            return AuthenticationResult.Failure("An error occurred during login. Please try again.");
        }
    }

    /// <summary>
    /// Refreshes an access token using a valid refresh token
    /// </summary>
    public async Task<AuthenticationResult> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return AuthenticationResult.Failure("Refresh token is required");

            // Find user by refresh token
            var user = await _context.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Refresh token attempt with invalid token");
                return AuthenticationResult.Failure("Invalid refresh token");
            }

            // Check if refresh token is expired
            if (user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token expired for user: {UserId}", user.Id);
                return AuthenticationResult.Failure("Refresh token has expired. Please login again.");
            }

            // Check if user is active
            if (!user.IsActive || !user.Tenant.IsActive)
            {
                return AuthenticationResult.Failure("Account is inactive");
            }

            // Generate new tokens
            var accessToken = _tokenService.GenerateAccessToken(
                user.Id,
                user.TenantId,
                user.Email,
                user.Role.ToString()
            );

            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // Update refresh token
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Token refreshed for user: {UserId}", user.Id);

            return AuthenticationResult.SuccessResult(
                accessToken,
                newRefreshToken,
                DateTime.UtcNow.AddMinutes(60)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return AuthenticationResult.Failure("An error occurred during token refresh");
        }
    }

    /// <summary>
    /// Logs out a user by invalidating their refresh token
    /// </summary>
    public async Task<bool> LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);

            if (user == null)
                return false;

            // Invalidate refresh token
            user.RefreshToken = null;
            user.RefreshTokenExpiresAt = null;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User logged out: {UserId}", userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Initiates password reset process by generating a reset token
    /// </summary>
    public async Task<bool> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (user == null)
            {
                // Don't reveal that user doesn't exist
                _logger.LogWarning("Password reset attempt for non-existent email: {Email}", email);
                return true; // Return true to prevent email enumeration
            }

            // Generate reset token
            user.PasswordResetToken = _tokenService.GenerateRefreshToken();
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync(cancellationToken);

            // TODO: Send password reset email via SendGrid
            _logger.LogInformation("Password reset requested for user: {Email}. Reset token: {Token}",
                email, user.PasswordResetToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset request for email {Email}", email);
            return false;
        }
    }

    /// <summary>
    /// Resets password using a valid reset token
    /// </summary>
    public async Task<bool> ResetPasswordAsync(
        string email,
        string resetToken,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                _logger.LogWarning("Password reset attempt with invalid password for email: {Email}", email);
                return false;
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Password reset attempt for non-existent email: {Email}", email);
                return false;
            }

            // Validate reset token
            if (user.PasswordResetToken != resetToken)
            {
                _logger.LogWarning("Password reset attempt with invalid token for user: {UserId}", user.Id);
                return false;
            }

            // Check if token is expired
            if (user.PasswordResetTokenExpiresAt == null || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Password reset attempt with expired token for user: {UserId}", user.Id);
                return false;
            }

            // Update password
            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiresAt = null;
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;

            // Invalidate refresh token for security
            user.RefreshToken = null;
            user.RefreshTokenExpiresAt = null;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password reset successfully for user: {UserId}", user.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset for email {Email}", email);
            return false;
        }
    }
}
