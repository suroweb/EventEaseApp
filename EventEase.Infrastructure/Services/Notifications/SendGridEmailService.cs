using EventEase.Application.Interfaces;
using EventEase.Domain.Entities;
using EventEase.Domain.Enums;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace EventEase.Infrastructure.Services.Notifications;

/// <summary>
/// Email service implementation using SendGrid
/// Production implementation should use SendGrid.NET SDK
/// </summary>
public class SendGridEmailService : IEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailService> _logger;
    private readonly string _sendGridApiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<SendGridEmailService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;

        _sendGridApiKey = configuration["SendGrid:ApiKey"] ?? throw new InvalidOperationException("SendGrid API key not configured");
        _fromEmail = configuration["SendGrid:FromEmail"] ?? "noreply@eventease.com";
        _fromName = configuration["SendGrid:FromName"] ?? "EventEase";
    }

    public async Task<EmailResult> SendEmailAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlContent,
        string? plainTextContent = null,
        List<EmailAttachment>? attachments = null)
    {
        try
        {
            _logger.LogInformation("Sending email to {Email} with subject: {Subject}", toEmail, subject);

            /*
             * Production Implementation with SendGrid.NET SDK:
             *
             * using SendGrid;
             * using SendGrid.Helpers.Mail;
             *
             * var client = new SendGridClient(_sendGridApiKey);
             * var from = new EmailAddress(_fromEmail, _fromName);
             * var to = new EmailAddress(toEmail, toName);
             * var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent ?? "", htmlContent);
             *
             * if (attachments != null)
             * {
             *     foreach (var attachment in attachments)
             *     {
             *         msg.AddAttachment(attachment.FileName,
             *                          Convert.ToBase64String(attachment.Content),
             *                          attachment.ContentType);
             *     }
             * }
             *
             * var response = await client.SendEmailAsync(msg);
             *
             * if (response.IsSuccessStatusCode)
             * {
             *     return new EmailResult
             *     {
             *         Success = true,
             *         MessageId = response.Headers.GetValues("X-Message-Id").FirstOrDefault(),
             *         SentAt = DateTime.UtcNow
             *     };
             * }
             */

            // Simulated implementation for development
            var simulatedMessageId = $"msg_{Guid.NewGuid():N}";

            _logger.LogInformation("Email sent successfully. MessageId: {MessageId}", simulatedMessageId);

            return new EmailResult
            {
                Success = true,
                MessageId = simulatedMessageId,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", toEmail);
            return new EmailResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }

    public async Task<EmailResult> SendBulkEmailAsync(
        List<EmailRecipient> recipients,
        string subject,
        string htmlContent,
        string? plainTextContent = null)
    {
        try
        {
            _logger.LogInformation("Sending bulk email to {Count} recipients", recipients.Count);

            /*
             * Production Implementation with SendGrid.NET SDK:
             *
             * var client = new SendGridClient(_sendGridApiKey);
             * var from = new EmailAddress(_fromEmail, _fromName);
             *
             * var tos = recipients.Select(r => new EmailAddress(r.Email, r.Name)).ToList();
             * var msg = MailHelper.CreateSingleEmailToMultipleRecipients(
             *     from, tos, subject, plainTextContent ?? "", htmlContent);
             *
             * var response = await client.SendEmailAsync(msg);
             */

            // Simulated implementation
            var simulatedMessageId = $"bulk_{Guid.NewGuid():N}";

            return new EmailResult
            {
                Success = true,
                MessageId = simulatedMessageId,
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk email");
            return new EmailResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }

    public async Task<EmailResult> SendTemplateEmailAsync(
        string toEmail,
        string toName,
        EmailTemplate template,
        Dictionary<string, string> templateData)
    {
        // Get template content
        var (subject, htmlContent, plainTextContent) = GetTemplateContent(template, templateData);

        return await SendEmailAsync(toEmail, toName, subject, htmlContent, plainTextContent);
    }

    public async Task<EmailResult> SendEventInvitationAsync(Guid eventId, Guid guestId)
    {
        var guest = await _context.Guests
            .Include(g => g.Event)
            .ThenInclude(e => e.Tenant)
            .FirstOrDefaultAsync(g => g.Id == guestId && g.EventId == eventId);

        if (guest == null || guest.Event == null)
        {
            return new EmailResult
            {
                Success = false,
                ErrorMessage = "Guest or event not found",
                SentAt = DateTime.UtcNow
            };
        }

        var templateData = new Dictionary<string, string>
        {
            { "guest_name", guest.Name },
            { "event_name", guest.Event.Title },
            { "event_date", guest.Event.StartDate.ToString("dddd, MMMM d, yyyy") },
            { "event_time", guest.Event.StartDate.ToString("h:mm tt") },
            { "event_location", guest.Event.Location ?? "To be announced" },
            { "event_description", guest.Event.Description ?? "" },
            { "tenant_name", guest.Event.Tenant.Name }
        };

        return await SendTemplateEmailAsync(
            guest.Email,
            guest.Name,
            EmailTemplate.EventInvitation,
            templateData);
    }

    public async Task<EmailResult> SendEventReminderAsync(
        Guid eventId,
        Guid registrationId,
        int hoursBeforeEvent)
    {
        var registration = await _context.Registrations
            .Include(r => r.Event)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == registrationId && r.EventId == eventId);

        if (registration == null || registration.Event == null || registration.User == null)
        {
            return new EmailResult
            {
                Success = false,
                ErrorMessage = "Registration not found",
                SentAt = DateTime.UtcNow
            };
        }

        var template = hoursBeforeEvent switch
        {
            24 => EmailTemplate.EventReminder24Hours,
            1 => EmailTemplate.EventReminder1Hour,
            _ => EmailTemplate.EventReminder24Hours
        };

        var templateData = new Dictionary<string, string>
        {
            { "user_name", registration.User.FullName ?? registration.User.Email },
            { "event_name", registration.Event.Title },
            { "event_date", registration.Event.StartDate.ToString("dddd, MMMM d, yyyy") },
            { "event_time", registration.Event.StartDate.ToString("h:mm tt") },
            { "event_location", registration.Event.Location ?? "To be announced" },
            { "hours_before", hoursBeforeEvent.ToString() }
        };

        return await SendTemplateEmailAsync(
            registration.User.Email,
            registration.User.FullName ?? registration.User.Email,
            template,
            templateData);
    }

    public async Task<EmailResult> SendRegistrationConfirmationAsync(Guid registrationId)
    {
        var registration = await _context.Registrations
            .Include(r => r.Event)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == registrationId);

        if (registration == null || registration.Event == null || registration.User == null)
        {
            return new EmailResult
            {
                Success = false,
                ErrorMessage = "Registration not found",
                SentAt = DateTime.UtcNow
            };
        }

        var templateData = new Dictionary<string, string>
        {
            { "user_name", registration.User.FullName ?? registration.User.Email },
            { "event_name", registration.Event.Title },
            { "event_date", registration.Event.StartDate.ToString("dddd, MMMM d, yyyy") },
            { "event_time", registration.Event.StartDate.ToString("h:mm tt") },
            { "event_location", registration.Event.Location ?? "To be announced" },
            { "registration_id", registration.Id.ToString() }
        };

        return await SendTemplateEmailAsync(
            registration.User.Email,
            registration.User.FullName ?? registration.User.Email,
            EmailTemplate.RegistrationConfirmation,
            templateData);
    }

    public async Task<EmailResult> SendPaymentReceiptAsync(Guid purchaseId)
    {
        var purchase = await _context.CreditPurchases
            .Include(p => p.Package)
            .Include(p => p.Tenant)
            .FirstOrDefaultAsync(p => p.Id == purchaseId);

        if (purchase == null || purchase.Package == null || purchase.Tenant == null)
        {
            return new EmailResult
            {
                Success = false,
                ErrorMessage = "Purchase not found",
                SentAt = DateTime.UtcNow
            };
        }

        var templateData = new Dictionary<string, string>
        {
            { "tenant_name", purchase.Tenant.Name },
            { "package_name", purchase.Package.Name },
            { "amount", $"{purchase.Amount:N2} {purchase.Currency}" },
            { "credits_granted", purchase.CreditsGranted.ToString("N0") },
            { "purchase_date", purchase.PaidAt?.ToString("MMMM d, yyyy") ?? DateTime.UtcNow.ToString("MMMM d, yyyy") },
            { "invoice_number", $"INV-{purchaseId.ToString()[..8].ToUpper()}" }
        };

        // Get tenant owner email
        var tenantOwner = await _context.Users
            .FirstOrDefaultAsync(u => u.TenantId == purchase.TenantId && u.Role == UserRole.TenantOwner);

        if (tenantOwner == null)
        {
            return new EmailResult
            {
                Success = false,
                ErrorMessage = "Tenant owner not found",
                SentAt = DateTime.UtcNow
            };
        }

        return await SendTemplateEmailAsync(
            tenantOwner.Email,
            tenantOwner.FullName ?? tenantOwner.Email,
            EmailTemplate.PaymentReceipt,
            templateData);
    }

    public async Task<EmailResult> SendPasswordResetEmailAsync(string email, string resetToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            // Don't reveal that user doesn't exist
            return new EmailResult
            {
                Success = true,
                SentAt = DateTime.UtcNow
            };
        }

        var resetUrl = $"{_configuration["AppUrl"]}/reset-password?token={resetToken}";

        var templateData = new Dictionary<string, string>
        {
            { "user_name", user.FullName ?? user.Email },
            { "reset_url", resetUrl },
            { "expiration_hours", "24" }
        };

        return await SendTemplateEmailAsync(
            user.Email,
            user.FullName ?? user.Email,
            EmailTemplate.PasswordReset,
            templateData);
    }

    public async Task<EmailResult> SendWelcomeEmailAsync(Guid tenantId)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Users.Where(u => u.Role == UserRole.TenantOwner))
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant == null)
        {
            return new EmailResult
            {
                Success = false,
                ErrorMessage = "Tenant not found",
                SentAt = DateTime.UtcNow
            };
        }

        var owner = tenant.Users.FirstOrDefault();
        if (owner == null)
        {
            return new EmailResult
            {
                Success = false,
                ErrorMessage = "Tenant owner not found",
                SentAt = DateTime.UtcNow
            };
        }

        var templateData = new Dictionary<string, string>
        {
            { "tenant_name", tenant.Name },
            { "owner_name", owner.FullName ?? owner.Email },
            { "dashboard_url", $"{_configuration["AppUrl"]}/dashboard" },
            { "available_credits", tenant.AvailableCredits.ToString("N0") }
        };

        return await SendTemplateEmailAsync(
            owner.Email,
            owner.FullName ?? owner.Email,
            EmailTemplate.WelcomeTenant,
            templateData);
    }

    private (string subject, string htmlContent, string plainTextContent) GetTemplateContent(
        EmailTemplate template,
        Dictionary<string, string> data)
    {
        // In production, load templates from database or file system
        // For now, generate basic templates

        return template switch
        {
            EmailTemplate.WelcomeTenant => (
                $"Welcome to EventEase, {data.GetValueOrDefault("tenant_name")}!",
                GenerateWelcomeHtml(data),
                GenerateWelcomePlainText(data)
            ),
            EmailTemplate.EventInvitation => (
                $"You're invited to {data.GetValueOrDefault("event_name")}",
                GenerateEventInvitationHtml(data),
                GenerateEventInvitationPlainText(data)
            ),
            EmailTemplate.EventReminder24Hours => (
                $"Reminder: {data.GetValueOrDefault("event_name")} is tomorrow!",
                GenerateEventReminderHtml(data),
                GenerateEventReminderPlainText(data)
            ),
            EmailTemplate.RegistrationConfirmation => (
                $"Registration confirmed for {data.GetValueOrDefault("event_name")}",
                GenerateRegistrationConfirmationHtml(data),
                GenerateRegistrationConfirmationPlainText(data)
            ),
            EmailTemplate.PaymentReceipt => (
                $"Payment receipt for {data.GetValueOrDefault("package_name")}",
                GeneratePaymentReceiptHtml(data),
                GeneratePaymentReceiptPlainText(data)
            ),
            EmailTemplate.PasswordReset => (
                "Reset your EventEase password",
                GeneratePasswordResetHtml(data),
                GeneratePasswordResetPlainText(data)
            ),
            _ => (
                "Notification from EventEase",
                "<html><body><p>You have a new notification from EventEase.</p></body></html>",
                "You have a new notification from EventEase."
            )
        };
    }

    private string GenerateWelcomeHtml(Dictionary<string, string> data) =>
        $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h1 style='color: #4F46E5;'>Welcome to EventEase! 🎉</h1>
                <p>Hi {data.GetValueOrDefault("owner_name")},</p>
                <p>Welcome to EventEase! We're excited to have {data.GetValueOrDefault("tenant_name")} on board.</p>
                <p>You currently have <strong>{data.GetValueOrDefault("available_credits")} AI credits</strong> to get started.</p>
                <p>
                    <a href='{data.GetValueOrDefault("dashboard_url")}'
                       style='display: inline-block; padding: 12px 24px; background-color: #4F46E5; color: white; text-decoration: none; border-radius: 6px;'>
                        Go to Dashboard
                    </a>
                </p>
                <p>Best regards,<br>The EventEase Team</p>
            </div>
        </body>
        </html>";

    private string GenerateWelcomePlainText(Dictionary<string, string> data) =>
        $@"Welcome to EventEase!

Hi {data.GetValueOrDefault("owner_name")},

Welcome to EventEase! We're excited to have {data.GetValueOrDefault("tenant_name")} on board.

You currently have {data.GetValueOrDefault("available_credits")} AI credits to get started.

Visit your dashboard: {data.GetValueOrDefault("dashboard_url")}

Best regards,
The EventEase Team";

    private string GenerateEventInvitationHtml(Dictionary<string, string> data) =>
        $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h1 style='color: #4F46E5;'>You're Invited!</h1>
                <h2>{data.GetValueOrDefault("event_name")}</h2>
                <p>Dear {data.GetValueOrDefault("guest_name")},</p>
                <p>You're invited to join us for {data.GetValueOrDefault("event_name")}.</p>
                <div style='background-color: #F3F4F6; padding: 15px; border-radius: 6px; margin: 20px 0;'>
                    <p><strong>📅 Date:</strong> {data.GetValueOrDefault("event_date")}</p>
                    <p><strong>🕒 Time:</strong> {data.GetValueOrDefault("event_time")}</p>
                    <p><strong>📍 Location:</strong> {data.GetValueOrDefault("event_location")}</p>
                </div>
                <p>{data.GetValueOrDefault("event_description")}</p>
                <p>We look forward to seeing you there!</p>
                <p>Best regards,<br>{data.GetValueOrDefault("tenant_name")}</p>
            </div>
        </body>
        </html>";

    private string GenerateEventInvitationPlainText(Dictionary<string, string> data) =>
        $@"You're Invited to {data.GetValueOrDefault("event_name")}!

Dear {data.GetValueOrDefault("guest_name")},

You're invited to join us for {data.GetValueOrDefault("event_name")}.

Date: {data.GetValueOrDefault("event_date")}
Time: {data.GetValueOrDefault("event_time")}
Location: {data.GetValueOrDefault("event_location")}

{data.GetValueOrDefault("event_description")}

We look forward to seeing you there!

Best regards,
{data.GetValueOrDefault("tenant_name")}";

    private string GenerateEventReminderHtml(Dictionary<string, string> data) =>
        $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h1 style='color: #4F46E5;'>Event Reminder ⏰</h1>
                <p>Hi {data.GetValueOrDefault("user_name")},</p>
                <p>This is a friendly reminder that <strong>{data.GetValueOrDefault("event_name")}</strong> is happening in {data.GetValueOrDefault("hours_before")} hours!</p>
                <div style='background-color: #FEF3C7; padding: 15px; border-radius: 6px; margin: 20px 0;'>
                    <p><strong>📅 Date:</strong> {data.GetValueOrDefault("event_date")}</p>
                    <p><strong>🕒 Time:</strong> {data.GetValueOrDefault("event_time")}</p>
                    <p><strong>📍 Location:</strong> {data.GetValueOrDefault("event_location")}</p>
                </div>
                <p>We can't wait to see you there!</p>
            </div>
        </body>
        </html>";

    private string GenerateEventReminderPlainText(Dictionary<string, string> data) =>
        $@"Event Reminder

Hi {data.GetValueOrDefault("user_name")},

This is a friendly reminder that {data.GetValueOrDefault("event_name")} is happening in {data.GetValueOrDefault("hours_before")} hours!

Date: {data.GetValueOrDefault("event_date")}
Time: {data.GetValueOrDefault("event_time")}
Location: {data.GetValueOrDefault("event_location")}

We can't wait to see you there!";

    private string GenerateRegistrationConfirmationHtml(Dictionary<string, string> data) =>
        $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h1 style='color: #10B981;'>Registration Confirmed! ✓</h1>
                <p>Hi {data.GetValueOrDefault("user_name")},</p>
                <p>Your registration for <strong>{data.GetValueOrDefault("event_name")}</strong> has been confirmed!</p>
                <div style='background-color: #D1FAE5; padding: 15px; border-radius: 6px; margin: 20px 0;'>
                    <p><strong>📅 Date:</strong> {data.GetValueOrDefault("event_date")}</p>
                    <p><strong>🕒 Time:</strong> {data.GetValueOrDefault("event_time")}</p>
                    <p><strong>📍 Location:</strong> {data.GetValueOrDefault("event_location")}</p>
                    <p><strong>🎫 Registration ID:</strong> {data.GetValueOrDefault("registration_id")}</p>
                </div>
                <p>You'll receive a reminder before the event. See you there!</p>
            </div>
        </body>
        </html>";

    private string GenerateRegistrationConfirmationPlainText(Dictionary<string, string> data) =>
        $@"Registration Confirmed!

Hi {data.GetValueOrDefault("user_name")},

Your registration for {data.GetValueOrDefault("event_name")} has been confirmed!

Date: {data.GetValueOrDefault("event_date")}
Time: {data.GetValueOrDefault("event_time")}
Location: {data.GetValueOrDefault("event_location")}
Registration ID: {data.GetValueOrDefault("registration_id")}

You'll receive a reminder before the event. See you there!";

    private string GeneratePaymentReceiptHtml(Dictionary<string, string> data) =>
        $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h1 style='color: #4F46E5;'>Payment Receipt</h1>
                <p>Hi {data.GetValueOrDefault("tenant_name")},</p>
                <p>Thank you for your purchase! Your payment has been processed successfully.</p>
                <div style='background-color: #F3F4F6; padding: 15px; border-radius: 6px; margin: 20px 0;'>
                    <p><strong>Package:</strong> {data.GetValueOrDefault("package_name")}</p>
                    <p><strong>Amount:</strong> {data.GetValueOrDefault("amount")}</p>
                    <p><strong>Credits Granted:</strong> {data.GetValueOrDefault("credits_granted")}</p>
                    <p><strong>Date:</strong> {data.GetValueOrDefault("purchase_date")}</p>
                    <p><strong>Invoice Number:</strong> {data.GetValueOrDefault("invoice_number")}</p>
                </div>
                <p>Your credits have been added to your account and are ready to use.</p>
                <p>Thank you for choosing EventEase!</p>
            </div>
        </body>
        </html>";

    private string GeneratePaymentReceiptPlainText(Dictionary<string, string> data) =>
        $@"Payment Receipt

Hi {data.GetValueOrDefault("tenant_name")},

Thank you for your purchase! Your payment has been processed successfully.

Package: {data.GetValueOrDefault("package_name")}
Amount: {data.GetValueOrDefault("amount")}
Credits Granted: {data.GetValueOrDefault("credits_granted")}
Date: {data.GetValueOrDefault("purchase_date")}
Invoice Number: {data.GetValueOrDefault("invoice_number")}

Your credits have been added to your account and are ready to use.

Thank you for choosing EventEase!";

    private string GeneratePasswordResetHtml(Dictionary<string, string> data) =>
        $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h1 style='color: #4F46E5;'>Reset Your Password</h1>
                <p>Hi {data.GetValueOrDefault("user_name")},</p>
                <p>We received a request to reset your password. Click the button below to create a new password:</p>
                <p>
                    <a href='{data.GetValueOrDefault("reset_url")}'
                       style='display: inline-block; padding: 12px 24px; background-color: #4F46E5; color: white; text-decoration: none; border-radius: 6px;'>
                        Reset Password
                    </a>
                </p>
                <p>This link will expire in {data.GetValueOrDefault("expiration_hours")} hours.</p>
                <p>If you didn't request this, please ignore this email.</p>
                <p style='color: #6B7280; font-size: 12px;'>If the button doesn't work, copy and paste this link into your browser:<br>{data.GetValueOrDefault("reset_url")}</p>
            </div>
        </body>
        </html>";

    private string GeneratePasswordResetPlainText(Dictionary<string, string> data) =>
        $@"Reset Your Password

Hi {data.GetValueOrDefault("user_name")},

We received a request to reset your password. Visit this link to create a new password:

{data.GetValueOrDefault("reset_url")}

This link will expire in {data.GetValueOrDefault("expiration_hours")} hours.

If you didn't request this, please ignore this email.";
}
