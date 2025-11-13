using EventEase.Domain.Entities;
using EventEase.Domain.Enums;

namespace EventEase.Domain.Tests.Entities;

/// <summary>
/// Tests for Event entity domain logic
/// </summary>
public class EventTests
{
    [Fact]
    public void Event_Creation_SetsRequiredProperties()
    {
        // Arrange & Act
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Annual Gala",
            Description = "Company annual gala event",
            Location = "Grand Hotel",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(4),
            Status = EventStatus.Draft
        };

        // Assert
        eventEntity.Id.Should().NotBeEmpty();
        eventEntity.Name.Should().Be("Annual Gala");
        eventEntity.Location.Should().Be("Grand Hotel");
        eventEntity.Status.Should().Be(EventStatus.Draft);
    }

    [Theory]
    [InlineData(EventStatus.Draft)]
    [InlineData(EventStatus.Published)]
    [InlineData(EventStatus.InProgress)]
    [InlineData(EventStatus.Completed)]
    [InlineData(EventStatus.Cancelled)]
    public void Event_CanHaveDifferentStatuses(EventStatus status)
    {
        // Arrange & Act
        var eventEntity = new Event
        {
            Status = status
        };

        // Assert
        eventEntity.Status.Should().Be(status);
    }

    [Fact]
    public void Event_Dates_EndDateShouldBeAfterStartDate()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(30);
        var endDate = startDate.AddHours(4);

        // Act
        var eventEntity = new Event
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Assert
        eventEntity.EndDate.Should().BeAfter(eventEntity.StartDate);
    }

    [Fact]
    public void Event_BelongsToTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var eventEntity = new Event
        {
            TenantId = tenantId
        };

        // Assert
        eventEntity.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Event_CanHaveMaxCapacity()
    {
        // Arrange & Act
        var eventEntity = new Event
        {
            MaxCapacity = 100
        };

        // Assert
        eventEntity.MaxCapacity.Should().Be(100);
    }
}
