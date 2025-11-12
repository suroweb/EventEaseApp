using EventEase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventEase.Infrastructure.Data.Configurations;

public class CreditPackageConfiguration : IEntityTypeConfiguration<CreditPackage>
{
    public void Configure(EntityTypeBuilder<CreditPackage> builder)
    {
        builder.ToTable("credit_packages");

        builder.HasKey(cp => cp.Id);

        builder.Property(cp => cp.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cp => cp.Price)
            .HasPrecision(18, 2);

        builder.Property(cp => cp.Currency)
            .HasMaxLength(3);

        // Indexes
        builder.HasIndex(cp => cp.Type).IsUnique();
        builder.HasIndex(cp => cp.IsActive);
        builder.HasIndex(cp => cp.IsVisible);
        builder.HasIndex(cp => cp.DisplayOrder);

        // Seed data for default credit packages
        builder.HasData(
            new CreditPackage
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Starter",
                Description = "Perfect for small events and getting started",
                Type = Domain.Enums.CreditPackageType.Starter,
                Price = 99m,
                Currency = "EUR",
                BaseCredits = 1000,
                BonusCredits = 0,
                ValidityDays = 180, // 6 months
                IsActive = true,
                IsVisible = true,
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow
            },
            new CreditPackage
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Pro",
                Description = "Best value for regular event organizers",
                Type = Domain.Enums.CreditPackageType.Pro,
                Price = 399m,
                Currency = "EUR",
                BaseCredits = 5000,
                BonusCredits = 1000,
                ValidityDays = 365, // 12 months
                IsActive = true,
                IsVisible = true,
                IsFeatured = true,
                BadgeText = "Most Popular",
                DisplayOrder = 2,
                CreatedAt = DateTime.UtcNow
            },
            new CreditPackage
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Enterprise",
                Description = "Custom solution for large organizations",
                Type = Domain.Enums.CreditPackageType.Enterprise,
                Price = 0m, // Custom pricing
                Currency = "EUR",
                BaseCredits = 20000,
                BonusCredits = 0,
                ValidityDays = 365,
                IsActive = true,
                IsVisible = true,
                DisplayOrder = 3,
                CreatedAt = DateTime.UtcNow
            },
            new CreditPackage
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Pay As You Go",
                Description = "Flexible credit purchasing",
                Type = Domain.Enums.CreditPackageType.PayAsYouGo,
                Price = 0.15m, // Per credit
                Currency = "EUR",
                BaseCredits = 1, // Represents per-credit pricing
                BonusCredits = 0,
                ValidityDays = 0, // No expiration for PAYG
                IsActive = true,
                IsVisible = true,
                DisplayOrder = 4,
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}
