using EventEase.Domain.Enums;

namespace EventEase.Application.Interfaces;

/// <summary>
/// Service for sending emails using SendGrid
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send email to a single recipient
    /// </summary>
    Task<EmailResult> SendEmailAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlContent,
        string? plainTextContent = null,
        List<EmailAttachment>? attachments = null);

    /// <summary>
    /// Send email to multiple recipients
    /// </summary>
    Task<EmailResult> SendBulkEmailAsync(
        List<EmailRecipient> recipients,
        string subject,
        string htmlContent,
        string? plainTextContent = null);

    /// <summary>
    /// Send email using a template
    /// </summary>
    Task<EmailResult> SendTemplateEmailAsync(
        string toEmail,
        string toName,
        EmailTemplate template,
        Dictionary<string, string> templateData);

    /// <summary>
    /// Send event invitation email
    /// </summary>
    Task<EmailResult> SendEventInvitationAsync(
        Guid eventId,
        Guid guestId);

    /// <summary>
    /// Send event reminder email
    /// </summary>
    Task<EmailResult> SendEventReminderAsync(
        Guid eventId,
        Guid registrationId,
        int hoursBeforeEvent);

    /// <summary>
    /// Send registration confirmation email
    /// </summary>
    Task<EmailResult> SendRegistrationConfirmationAsync(
        Guid registrationId);

    /// <summary>
    /// Send payment receipt email
    /// </summary>
    Task<EmailResult> SendPaymentReceiptAsync(
        Guid purchaseId);

    /// <summary>
    /// Send password reset email
    /// </summary>
    Task<EmailResult> SendPasswordResetEmailAsync(
        string email,
        string resetToken);

    /// <summary>
    /// Send welcome email to new tenant
    /// </summary>
    Task<EmailResult> SendWelcomeEmailAsync(
        Guid tenantId);
}

public class EmailResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; }
}

public class EmailRecipient
{
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
}

public class EmailAttachment
{
    public string FileName { get; set; } = null!;
    public byte[] Content { get; set; } = null!;
    public string ContentType { get; set; } = "application/octet-stream";
}
