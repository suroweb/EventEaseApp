namespace EventEase.API.DTOs;

/// <summary>
/// App version compatibility check request
/// </summary>
public class AppVersionCheckRequest
{
    public string Platform { get; set; } = string.Empty; // "ios", "android"
    public string CurrentVersion { get; set; } = string.Empty; // e.g., "1.2.3"
    public string? BuildNumber { get; set; }
}

/// <summary>
/// App version compatibility check response
/// </summary>
public class AppVersionCheckResponse
{
    public bool IsSupported { get; set; }
    public bool IsLatestVersion { get; set; }
    public bool RequiresUpdate { get; set; }
    public bool ForceUpdate { get; set; }
    public string LatestVersion { get; set; } = string.Empty;
    public string? MinimumSupportedVersion { get; set; }
    public string? UpdateUrl { get; set; } // App Store or Play Store URL
    public string? ReleaseNotes { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public List<string>? NewFeatures { get; set; }
}

/// <summary>
/// Mobile app configuration response
/// </summary>
public class MobileAppConfigResponse
{
    public string ApiVersion { get; set; } = string.Empty;
    public bool GraphQLEnabled { get; set; } = true;
    public string GraphQLEndpoint { get; set; } = string.Empty;
    public bool PushNotificationsEnabled { get; set; } = true;
    public bool OfflineModeEnabled { get; set; } = true;
    public bool QRCodeScanningEnabled { get; set; } = true;
    public bool GeolocationCheckInEnabled { get; set; } = true;
    public int MaxOfflineEventsCount { get; set; } = 50;
    public int SyncIntervalMinutes { get; set; } = 15;
    public Dictionary<string, object> FeatureFlags { get; set; } = new();
}
