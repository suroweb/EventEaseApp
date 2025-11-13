using EventEase.Application.Common;
using EventEase.Application.Interfaces;
using EventEase.API.Controllers;
using EventEase.API.DTOs;
using EventEase.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventEase.API.Tests.Controllers;

/// <summary>
/// Tests for AuthenticationController API endpoints
/// </summary>
public class AuthenticationControllerTests
{
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ILogger<AuthenticationController>> _mockLogger;
    private readonly ApplicationDbContext _mockContext;
    private readonly AuthenticationController _controller;

    public AuthenticationControllerTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockLogger = new Mock<ILogger<AuthenticationController>>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _mockContext = new ApplicationDbContext(options);

        _controller = new AuthenticationController(
            _mockAuthService.Object,
            _mockCurrentUserService.Object,
            _mockContext,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsOkWithTokens()
    {
        // Arrange
        var authResult = AuthenticationResult.SuccessResult(
            "access_token",
            "refresh_token",
            DateTime.UtcNow.AddHours(1));

        _mockAuthService
            .Setup(x => x.RegisterTenantAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(authResult));

        // Act
        var result = await _controller.Register(new RegisterTenantRequest
        {
            CompanyName = "Acme Corp",
            Email = "owner@acme.com",
            Password = "SecurePass123",
            FirstName = "John",
            LastName = "Doe"
        });

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithTokens()
    {
        // Arrange
        var authResult = AuthenticationResult.SuccessResult(
            "access_token",
            "refresh_token",
            DateTime.UtcNow.AddHours(1));

        _mockAuthService
            .Setup(x => x.LoginAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        // Act
        var result = await _controller.Login(new LoginRequest
        {
            Email = "user@example.com",
            Password = "Password123"
        });

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var authResult = AuthenticationResult.Failure("Invalid email or password");

        _mockAuthService
            .Setup(x => x.LoginAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        // Act
        var result = await _controller.Login(new LoginRequest
        {
            Email = "user@example.com",
            Password = "WrongPassword"
        });

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsOkWithNewTokens()
    {
        // Arrange
        var authResult = AuthenticationResult.SuccessResult(
            "new_access_token",
            "new_refresh_token",
            DateTime.UtcNow.AddHours(1));

        _mockAuthService
            .Setup(x => x.RefreshTokenAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        // Act
        var result = await _controller.RefreshToken(new RefreshTokenRequest
        {
            RefreshToken = "valid_refresh_token"
        });

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Logout_ValidUser_ReturnsOk()
    {
        // Arrange
        _mockAuthService
            .Setup(x => x.LogoutAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Logout();

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task ForgotPassword_ValidEmail_ReturnsOk()
    {
        // Arrange
        _mockAuthService
            .Setup(x => x.ForgotPasswordAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ForgotPassword(new ForgotPasswordRequest
        {
            Email = "user@example.com"
        });

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task ResetPassword_ValidToken_ReturnsOk()
    {
        // Arrange
        _mockAuthService
            .Setup(x => x.ResetPasswordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ResetPassword(new ResetPasswordRequest
        {
            Email = "user@example.com",
            Token = "valid_token",
            NewPassword = "NewPassword123"
        });

        // Assert
        result.Should().BeOfType<OkResult>();
    }
}
