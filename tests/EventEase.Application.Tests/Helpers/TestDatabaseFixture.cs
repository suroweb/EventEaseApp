using EventEase.Application.Interfaces;
using EventEase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EventEase.Application.Tests.Helpers;

/// <summary>
/// Test fixture for creating in-memory database instances for unit tests
/// </summary>
public class TestDatabaseFixture : IDisposable
{
    private bool _disposed = false;

    public ApplicationDbContext CreateContext(Guid? tenantId = null, Guid? userId = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        var mockTenantService = new Mock<ICurrentTenantService>();
        mockTenantService.Setup(x => x.TenantId).Returns(tenantId ?? Guid.NewGuid());

        var mockUserService = new Mock<ICurrentUserService>();
        mockUserService.Setup(x => x.UserId).Returns(userId ?? Guid.NewGuid());

        return new ApplicationDbContext(options, mockTenantService.Object, mockUserService.Object);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources if needed
            }
            _disposed = true;
        }
    }
}
