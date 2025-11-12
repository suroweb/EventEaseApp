namespace EventEase.Application.Interfaces;

/// <summary>
/// Service to provide the current authenticated user context
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    string? Email { get; }
    string? FullName { get; }
    bool IsAuthenticated { get; }
}
