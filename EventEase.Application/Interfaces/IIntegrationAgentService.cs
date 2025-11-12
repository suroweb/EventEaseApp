using EventEase.Domain.Enums;

namespace EventEase.Application.Interfaces;

/// <summary>
/// Integration Agent - Third-party integrations and automation
/// </summary>
public interface IIntegrationAgentService
{
    /// <summary>
    /// Generate calendar invite content
    /// </summary>
    Task<IntegrationAgentResult> GenerateCalendarInviteAsync(
        Guid tenantId,
        Guid eventId,
        string calendarType, // Google, Outlook, Apple
        AIProvider? preferredProvider = null);

    /// <summary>
    /// Sync event data to CRM
    /// </summary>
    Task<IntegrationAgentResult> PrepareCRMSyncDataAsync(
        Guid tenantId,
        Guid eventId,
        string crmType, // Salesforce, HubSpot, etc.
        AIProvider? preferredProvider = null);

    /// <summary>
    /// Generate notification messages for Slack/Teams
    /// </summary>
    Task<IntegrationAgentResult> GenerateTeamNotificationsAsync(
        Guid tenantId,
        Guid eventId,
        string platform, // Slack, Teams
        string notificationType, // reminder, update, summary
        AIProvider? preferredProvider = null);

    /// <summary>
    /// Parse and extract event data from external sources
    /// </summary>
    Task<IntegrationAgentResult> ParseExternalEventDataAsync(
        Guid tenantId,
        string sourceData,
        string sourceType, // email, webpage, document
        AIProvider? preferredProvider = null);
}

/// <summary>
/// Integration agent operation result
/// </summary>
public class IntegrationAgentResult
{
    public bool Success { get; set; }
    public string Response { get; set; } = string.Empty;
    public decimal CreditsCost { get; set; }
    public AIProvider Provider { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FormattedContent { get; set; }
    public Dictionary<string, object>? IntegrationData { get; set; }
}
