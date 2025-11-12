using EventEase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventEase.Infrastructure.Data.Configurations;

public class CreditTransactionConfiguration : IEntityTypeConfiguration<CreditTransaction>
{
    public void Configure(EntityTypeBuilder<CreditTransaction> builder)
    {
        builder.ToTable("credit_transactions");

        builder.HasKey(ct => ct.Id);

        builder.Property(ct => ct.Amount)
            .HasPrecision(18, 2);

        builder.Property(ct => ct.BalanceBefore)
            .HasPrecision(18, 2);

        builder.Property(ct => ct.BalanceAfter)
            .HasPrecision(18, 2);

        builder.Property(ct => ct.Description)
            .IsRequired()
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(ct => ct.TenantId);
        builder.HasIndex(ct => ct.UserId);
        builder.HasIndex(ct => ct.Type);
        builder.HasIndex(ct => ct.CreatedAt);
        builder.HasIndex(ct => new { ct.TenantId, ct.CreatedAt });
        builder.HasIndex(ct => ct.ExpiresAt);
        builder.HasIndex(ct => ct.IsExpired);

        // Relationships
        builder.HasOne(ct => ct.AIAgentUsage)
            .WithOne(au => au.CreditTransaction)
            .HasForeignKey<CreditTransaction>(ct => ct.AIAgentUsageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(ct => ct.PaymentTransaction)
            .WithOne(pt => pt.CreditTransaction)
            .HasForeignKey<CreditTransaction>(ct => ct.PaymentTransactionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
