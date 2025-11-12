using EventEase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventEase.Infrastructure.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(e => e.Description)
            .HasMaxLength(5000);

        builder.Property(e => e.Price)
            .HasPrecision(18, 2);

        builder.Property(e => e.Currency)
            .HasMaxLength(3);

        builder.Property(e => e.VenueLatitude)
            .HasPrecision(10, 7);

        builder.Property(e => e.VenueLongitude)
            .HasPrecision(10, 7);

        // Indexes
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.CreatedByUserId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.StartDate);
        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => new { e.TenantId, e.StartDate });
        builder.HasIndex(e => new { e.TenantId, e.Status });

        // Relationships
        builder.HasMany(e => e.Registrations)
            .WithOne(r => r.Event)
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Invitations)
            .WithOne(i => i.Event)
            .HasForeignKey(i => i.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Budget)
            .WithOne(b => b.Event)
            .HasForeignKey<Budget>(b => b.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
