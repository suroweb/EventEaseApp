namespace EventEase.Application.Interfaces;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates an access token (JWT) for the user
    /// </summary>
    string GenerateAccessToken(Guid userId, Guid tenantId, string email, string role);

    /// <summary>
    /// Generates a refresh token
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT token and returns the user ID if valid
    /// </summary>
    Guid? ValidateToken(string token);
}
