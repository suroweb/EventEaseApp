namespace EventEase.API.DTOs;

/// <summary>
/// Response model for current user information
/// </summary>
public class CurrentUserResponse
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public decimal AvailableCredits { get; set; }
}
