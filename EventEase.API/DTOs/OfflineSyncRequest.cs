namespace EventEase.API.DTOs;

/// <summary>
/// Offline sync request to sync local changes with server
/// </summary>
public class OfflineSyncRequest
{
    public DateTime LastSyncTimestamp { get; set; }
    public List<OfflineAction> PendingActions { get; set; } = new();
    public string DeviceId { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
}

/// <summary>
/// Offline action performed by mobile app
/// </summary>
public class OfflineAction
{
    public string ActionId { get; set; } = string.Empty; // Client-generated unique ID
    public string ActionType { get; set; } = string.Empty; // "check_in", "register", "update_profile"
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Offline sync response
/// </summary>
public class OfflineSyncResponse
{
    public bool Success { get; set; }
    public DateTime ServerTimestamp { get; set; }
    public List<SyncResult> Results { get; set; } = new();
    public List<ServerUpdate> ServerUpdates { get; set; } = new();
    public SyncConflict? Conflict { get; set; }
}

/// <summary>
/// Result of syncing a single offline action
/// </summary>
public class SyncResult
{
    public string ActionId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public object? ServerData { get; set; }
}

/// <summary>
/// Server update to be applied on client
/// </summary>
public class ServerUpdate
{
    public string EntityType { get; set; } = string.Empty; // "event", "registration"
    public string UpdateType { get; set; } = string.Empty; // "created", "updated", "deleted"
    public Guid EntityId { get; set; }
    public DateTime Timestamp { get; set; }
    public object? Data { get; set; }
}

/// <summary>
/// Sync conflict information
/// </summary>
public class SyncConflict
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string ConflictType { get; set; } = string.Empty; // "concurrent_update", "deleted_on_server"
    public object? ClientVersion { get; set; }
    public object? ServerVersion { get; set; }
    public string? Resolution { get; set; } // "server_wins", "client_wins", "manual"
}

/// <summary>
/// Request to get data for offline mode
/// </summary>
public class OfflineDataRequest
{
    public List<Guid> EventIds { get; set; } = new();
    public bool IncludeRegistrations { get; set; } = true;
    public bool IncludeGuests { get; set; } = false;
    public DateTime? SinceTimestamp { get; set; }
}

/// <summary>
/// Response with data for offline mode
/// </summary>
public class OfflineDataResponse
{
    public DateTime Timestamp { get; set; }
    public List<object> Events { get; set; } = new();
    public List<object> Registrations { get; set; } = new();
    public List<object> Guests { get; set; } = new();
    public string? ContinuationToken { get; set; }
}
