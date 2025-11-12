using EventEase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventEase.Infrastructure.Data.Configurations;

public class GuestConfiguration : IEntityTypeConfiguration<Guest>
{
    public void Configure(EntityTypeBuilder<Guest> builder)
    {
        builder.ToTable("guests");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(g => g.FirstName)
            .HasMaxLength(100);

        builder.Property(g => g.LastName)
            .HasMaxLength(100);

        builder.Property(g => g.Company)
            .HasMaxLength(200);

        builder.Property(g => g.EngagementScore)
            .HasPrecision(5, 2);

        // Indexes
        builder.HasIndex(g => g.TenantId);
        builder.HasIndex(g => g.Email);
        builder.HasIndex(g => new { g.TenantId, g.Email }).IsUnique();
        builder.HasIndex(g => g.EngagementScore);
        builder.HasIndex(g => g.LastInvitationSentAt);

        // Relationships
        builder.HasMany(g => g.Invitations)
            .WithOne(i => i.Guest)
            .HasForeignKey(i => i.GuestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
