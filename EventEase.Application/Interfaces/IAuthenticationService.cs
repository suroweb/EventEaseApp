using EventEase.Application.Common;

namespace EventEase.Application.Interfaces;

/// <summary>
/// Service for authentication operations
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Registers a new tenant and owner user
    /// </summary>
    Task<Result<AuthenticationResult>> RegisterTenantAsync(
        string companyName,
        string email,
        string password,
        string firstName,
        string lastName,
        string? phoneNumber = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user and returns JWT tokens
    /// </summary>
    Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an access token using a refresh token
    /// </summary>
    Task<AuthenticationResult> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out a user by invalidating their refresh token
    /// </summary>
    Task<bool> LogoutAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates password reset process
    /// </summary>
    Task<bool> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets password using reset token
    /// </summary>
    Task<bool> ResetPasswordAsync(
        string email,
        string resetToken,
        string newPassword,
        CancellationToken cancellationToken = default);
}
