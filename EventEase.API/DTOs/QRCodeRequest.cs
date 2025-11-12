namespace EventEase.API.DTOs;

/// <summary>
/// Request to generate QR code for event or registration
/// </summary>
public class QRCodeRequest
{
    public Guid? EventId { get; set; }
    public Guid? RegistrationId { get; set; }
    public string Format { get; set; } = "png"; // png, svg
    public int Size { get; set; } = 300; // pixels
}

/// <summary>
/// QR code response
/// </summary>
public class QRCodeResponse
{
    public string QRCodeData { get; set; } = string.Empty; // Base64 encoded image
    public string Format { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string EncodedValue { get; set; } = string.Empty; // The actual text/data encoded in QR
}

/// <summary>
/// QR code scan request
/// </summary>
public class QRCodeScanRequest
{
    public string QRCodeData { get; set; } = string.Empty; // The scanned QR code data
    public Guid? EventId { get; set; } // Optional: for validation
}

/// <summary>
/// QR code scan response
/// </summary>
public class QRCodeScanResponse
{
    public bool IsValid { get; set; }
    public string Type { get; set; } = string.Empty; // "event", "registration"
    public Guid? EventId { get; set; }
    public Guid? RegistrationId { get; set; }
    public string? EventName { get; set; }
    public string? AttendeeName { get; set; }
    public string? AttendeeEmail { get; set; }
    public string? RegistrationStatus { get; set; }
    public bool AlreadyCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public string? ErrorMessage { get; set; }
}
