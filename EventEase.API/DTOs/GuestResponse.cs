namespace EventEase.API.DTOs;

/// <summary>
/// Response model for guest data
/// </summary>
public class GuestResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public string? Industry { get; set; }
    public string? Location { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public List<string>? Tags { get; set; }
    public decimal EngagementScore { get; set; }
    public DateTime? LastContactDate { get; set; }
    public decimal? PredictedAttendanceRate { get; set; }
    public DateTime? OptimalInviteTime { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Statistics
    public int TotalInvitations { get; set; }
    public int TotalAttended { get; set; }
    public decimal AttendanceRate => TotalInvitations > 0
        ? (decimal)TotalAttended / TotalInvitations * 100
        : 0;
}
