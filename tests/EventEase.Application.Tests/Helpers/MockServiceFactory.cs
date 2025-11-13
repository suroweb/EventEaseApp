using EventEase.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventEase.Application.Tests.Helpers;

/// <summary>
/// Factory for creating mock services for testing
/// </summary>
public static class MockServiceFactory
{
    /// <summary>
    /// Creates a mock IPasswordHasher that uses simple hashing for tests
    /// </summary>
    public static Mock<IPasswordHasher> CreatePasswordHasher()
    {
        var mock = new Mock<IPasswordHasher>();

        // Simple test implementation: just prepend "hashed_"
        mock.Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns<string>(password => $"hashed_{password}");

        mock.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((password, hash) => hash == $"hashed_{password}");

        return mock;
    }

    /// <summary>
    /// Creates a mock ITokenService that returns predictable tokens
    /// </summary>
    public static Mock<ITokenService> CreateTokenService()
    {
        var mock = new Mock<ITokenService>();

        mock.Setup(x => x.GenerateAccessToken(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns<Guid, Guid, string, string>((userId, tenantId, email, role) =>
                $"access_token_{userId}_{tenantId}");

        mock.Setup(x => x.GenerateRefreshToken())
            .Returns(() => $"refresh_token_{Guid.NewGuid()}");

        return mock;
    }

    /// <summary>
    /// Creates a mock IEmailService for testing email operations
    /// </summary>
    public static Mock<IEmailService> CreateEmailService()
    {
        var mock = new Mock<IEmailService>();

        mock.Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return mock;
    }

    /// <summary>
    /// Creates a mock IStripePaymentService for testing payment operations
    /// </summary>
    public static Mock<IStripePaymentService> CreateStripePaymentService()
    {
        var mock = new Mock<IStripePaymentService>();

        // Add payment-related mocks as needed

        return mock;
    }

    /// <summary>
    /// Creates a mock ILogger for any type
    /// </summary>
    public static Mock<ILogger<T>> CreateLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    /// <summary>
    /// Creates a mock ICurrentTenantService
    /// </summary>
    public static Mock<ICurrentTenantService> CreateCurrentTenantService(Guid? tenantId = null)
    {
        var mock = new Mock<ICurrentTenantService>();
        mock.Setup(x => x.TenantId).Returns(tenantId ?? Guid.NewGuid());
        return mock;
    }

    /// <summary>
    /// Creates a mock ICurrentUserService
    /// </summary>
    public static Mock<ICurrentUserService> CreateCurrentUserService(Guid? userId = null)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.UserId).Returns(userId ?? Guid.NewGuid());
        return mock;
    }
}
