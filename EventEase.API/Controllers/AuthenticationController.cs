using EventEase.API.DTOs;
using EventEase.Application.Interfaces;
using EventEase.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.API.Controllers;

/// <summary>
/// Authentication and authorization endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(
        IAuthenticationService authService,
        ICurrentUserService currentUserService,
        ApplicationDbContext context,
        ILogger<AuthenticationController> logger)
    {
        _authService = authService;
        _currentUserService = currentUserService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Register a new tenant (company) with an owner user
    /// </summary>
    /// <param name="request">Tenant registration details</param>
    /// <returns>Authentication tokens if successful</returns>
    /// <response code="200">Tenant registered successfully</response>
    /// <response code="400">Invalid request or email already registered</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthenticationResponse>> Register([FromBody] RegisterTenantRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterTenantAsync(
            request.CompanyName,
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.PhoneNumber
        );

        if (!result.IsSuccess)
        {
            return BadRequest(new { Error = result.Error });
        }

        var authResult = result.Value;

        var response = new AuthenticationResponse
        {
            Success = authResult.Success,
            AccessToken = authResult.AccessToken,
            RefreshToken = authResult.RefreshToken,
            ExpiresAt = authResult.ExpiresAt,
            ExpiresIn = authResult.ExpiresAt.HasValue
                ? (int)(authResult.ExpiresAt.Value - DateTime.UtcNow).TotalSeconds
                : null
        };

        return Ok(response);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication tokens if successful</returns>
    /// <response code="200">Login successful</response>
    /// <response code="401">Invalid credentials or account locked</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var authResult = await _authService.LoginAsync(request.Email, request.Password);

        if (!authResult.Success)
        {
            return Unauthorized(new { Error = authResult.Error });
        }

        var response = new AuthenticationResponse
        {
            Success = authResult.Success,
            AccessToken = authResult.AccessToken,
            RefreshToken = authResult.RefreshToken,
            ExpiresAt = authResult.ExpiresAt,
            ExpiresIn = authResult.ExpiresAt.HasValue
                ? (int)(authResult.ExpiresAt.Value - DateTime.UtcNow).TotalSeconds
                : null
        };

        return Ok(response);
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>New authentication tokens</returns>
    /// <response code="200">Token refreshed successfully</response>
    /// <response code="401">Invalid or expired refresh token</response>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var authResult = await _authService.RefreshTokenAsync(request.RefreshToken);

        if (!authResult.Success)
        {
            return Unauthorized(new { Error = authResult.Error });
        }

        var response = new AuthenticationResponse
        {
            Success = authResult.Success,
            AccessToken = authResult.AccessToken,
            RefreshToken = authResult.RefreshToken,
            ExpiresAt = authResult.ExpiresAt,
            ExpiresIn = authResult.ExpiresAt.HasValue
                ? (int)(authResult.ExpiresAt.Value - DateTime.UtcNow).TotalSeconds
                : null
        };

        return Ok(response);
    }

    /// <summary>
    /// Logout current user (invalidates refresh token)
    /// </summary>
    /// <returns>Success status</returns>
    /// <response code="200">Logged out successfully</response>
    /// <response code="401">Not authenticated</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Logout()
    {
        var userId = _currentUserService.UserId;

        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var success = await _authService.LogoutAsync(userId);

        if (success)
        {
            return Ok(new { Message = "Logged out successfully" });
        }

        return BadRequest(new { Error = "Logout failed" });
    }

    /// <summary>
    /// Initiate password reset (sends email with reset token)
    /// </summary>
    /// <param name="request">Email address</param>
    /// <returns>Success status</returns>
    /// <response code="200">Password reset email sent (if email exists)</response>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Always return success to prevent email enumeration
        await _authService.ForgotPasswordAsync(request.Email);

        return Ok(new { Message = "If your email is registered, you will receive a password reset link shortly." });
    }

    /// <summary>
    /// Reset password using reset token
    /// </summary>
    /// <param name="request">Password reset details</param>
    /// <returns>Success status</returns>
    /// <response code="200">Password reset successful</response>
    /// <response code="400">Invalid or expired reset token</response>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _authService.ResetPasswordAsync(
            request.Email,
            request.ResetToken,
            request.NewPassword
        );

        if (success)
        {
            return Ok(new { Message = "Password reset successfully. You can now login with your new password." });
        }

        return BadRequest(new { Error = "Invalid or expired reset token" });
    }

    /// <summary>
    /// Get current authenticated user information
    /// </summary>
    /// <returns>Current user details</returns>
    /// <response code="200">User information retrieved</response>
    /// <response code="401">Not authenticated</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser()
    {
        var userId = _currentUserService.UserId;

        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new { Error = "User not found" });
        }

        var response = new CurrentUserResponse
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.ToString(),
            TenantName = user.Tenant.Name,
            AvailableCredits = user.Tenant.AvailableCredits
        };

        return Ok(response);
    }
}
