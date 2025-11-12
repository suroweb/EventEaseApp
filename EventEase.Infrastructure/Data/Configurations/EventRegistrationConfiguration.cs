using EventEase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventEase.Infrastructure.Data.Configurations;

public class EventRegistrationConfiguration : IEntityTypeConfiguration<EventRegistration>
{
    public void Configure(EntityTypeBuilder<EventRegistration> builder)
    {
        builder.ToTable("event_registrations");

        builder.HasKey(er => er.Id);

        builder.Property(er => er.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(er => er.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(er => er.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(er => er.TotalAmount)
            .HasPrecision(18, 2);

        // Indexes
        builder.HasIndex(er => er.TenantId);
        builder.HasIndex(er => er.EventId);
        builder.HasIndex(er => er.UserId);
        builder.HasIndex(er => er.Email);
        builder.HasIndex(er => er.Status);
        builder.HasIndex(er => er.RegisteredAt);
        builder.HasIndex(er => new { er.EventId, er.Status });
        builder.HasIndex(er => new { er.EventId, er.Email }); // Prevent duplicate registrations
    }
}
