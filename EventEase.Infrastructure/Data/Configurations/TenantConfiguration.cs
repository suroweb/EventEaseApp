using EventEase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventEase.Infrastructure.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.PrimaryContactEmail)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.AvailableCredits)
            .HasPrecision(18, 2);

        builder.Property(t => t.TotalCreditsPurchased)
            .HasPrecision(18, 2);

        builder.Property(t => t.TotalCreditsUsed)
            .HasPrecision(18, 2);

        // Indexes
        builder.HasIndex(t => t.PrimaryContactEmail);
        builder.HasIndex(t => t.StripeCustomerId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.IsActive);
        builder.HasIndex(t => t.CreatedAt);

        // Relationships
        builder.HasMany(t => t.Users)
            .WithOne(u => u.Tenant)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Events)
            .WithOne(e => e.Tenant)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
