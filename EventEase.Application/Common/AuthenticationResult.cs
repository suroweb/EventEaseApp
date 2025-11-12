namespace EventEase.Application.Common;

/// <summary>
/// Represents an authentication result with tokens
/// </summary>
public class AuthenticationResult
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Error { get; set; }

    public static AuthenticationResult Failure(string error) => new()
    {
        Success = false,
        Error = error
    };

    public static AuthenticationResult SuccessResult(string accessToken, string refreshToken, DateTime expiresAt) => new()
    {
        Success = true,
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        ExpiresAt = expiresAt
    };
}
