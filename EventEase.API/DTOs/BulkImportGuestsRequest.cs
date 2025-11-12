using System.ComponentModel.DataAnnotations;

namespace EventEase.API.DTOs;

/// <summary>
/// Request model for bulk importing guests
/// </summary>
public class BulkImportGuestsRequest
{
    [Required(ErrorMessage = "Guests list is required")]
    [MinLength(1, ErrorMessage = "At least one guest is required")]
    public List<GuestImportItem> Guests { get; set; } = new();

    /// <summary>
    /// Skip guests with duplicate emails (default: false = update existing)
    /// </summary>
    public bool SkipDuplicates { get; set; } = false;

    /// <summary>
    /// Apply tags to all imported guests
    /// </summary>
    public List<string>? BulkTags { get; set; }
}

/// <summary>
/// Individual guest data for bulk import
/// </summary>
public class GuestImportItem
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

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
    public string? PreferredLanguage { get; set; }

    public List<string>? Tags { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

/// <summary>
/// Response model for bulk import operation
/// </summary>
public class BulkImportResponse
{
    public int TotalProcessed { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<ImportError> Errors { get; set; } = new();
}

/// <summary>
/// Individual import error details
/// </summary>
public class ImportError
{
    public int RowNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
