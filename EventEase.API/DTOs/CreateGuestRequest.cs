using System.ComponentModel.DataAnnotations;

namespace EventEase.API.DTOs;

/// <summary>
/// Request model for creating a guest
/// </summary>
public class CreateGuestRequest
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(200)]
    public string? Company { get; set; }

    [StringLength(100)]
    public string? JobTitle { get; set; }

    [StringLength(100)]
    public string? Industry { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    [StringLength(10)]
    public string PreferredLanguage { get; set; } = "en";

    public List<string>? Tags { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
