namespace EventEase.API.DTOs;

/// <summary>
/// Response model for authentication operations
/// </summary>
public class AuthenticationResponse
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? TokenType { get; set; } = "Bearer";
    public int? ExpiresIn { get; set; } // Seconds until expiration
    public string? Error { get; set; }
}
