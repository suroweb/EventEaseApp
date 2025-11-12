namespace EventEase.API.DTOs;

/// <summary>
/// Check-in request with geolocation verification
/// </summary>
public class GeolocationCheckInRequest
{
    public Guid RegistrationId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? Accuracy { get; set; } // meters
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Geolocation check-in response
/// </summary>
public class GeolocationCheckInResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsWithinGeofence { get; set; }
    public decimal? DistanceFromVenue { get; set; } // meters
    public decimal? MaxAllowedDistance { get; set; } // meters
    public DateTime? CheckedInAt { get; set; }
    public Guid? RegistrationId { get; set; }
}

/// <summary>
/// Geofence settings for an event
/// </summary>
public class GeofenceSettings
{
    public Guid EventId { get; set; }
    public decimal? VenueLatitude { get; set; }
    public decimal? VenueLongitude { get; set; }
    public decimal GeofenceRadius { get; set; } = 100; // meters (default 100m)
    public bool IsGeofenceEnabled { get; set; } = false;
}
