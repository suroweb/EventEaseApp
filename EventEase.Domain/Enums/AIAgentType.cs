namespace EventEase.Domain.Enums;

/// <summary>
/// Types of AI agents available in the system
/// </summary>
public enum AIAgentType
{
    /// <summary>
    /// Planning Agent - Natural language event creation, venue recommendations, budget optimization, timeline generation
    /// </summary>
    Planning = 0,

    /// <summary>
    /// Invitation Agent - Smart guest selection (ML-based), personalized email generation, optimal send time prediction
    /// </summary>
    Invitation = 1,

    /// <summary>
    /// Analytics Agent - Attendance prediction, sentiment analysis, ROI calculation, actionable insights
    /// </summary>
    Analytics = 2,

    /// <summary>
    /// Budget Agent - Cost estimation, vendor comparison, expense tracking
    /// </summary>
    Budget = 3,

    /// <summary>
    /// Integration Agent - Calendar sync (Google/Outlook), CRM integration, Slack/Teams notifications
    /// </summary>
    Integration = 4
}
