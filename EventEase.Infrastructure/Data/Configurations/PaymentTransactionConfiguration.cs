using EventEase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventEase.Infrastructure.Data.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("payment_transactions");

        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.StripePaymentIntentId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(pt => pt.Amount)
            .HasPrecision(18, 2);

        builder.Property(pt => pt.RefundedAmount)
            .HasPrecision(18, 2);

        builder.Property(pt => pt.Currency)
            .HasMaxLength(3);

        builder.Property(pt => pt.RawWebhookData)
            .HasColumnType("jsonb"); // PostgreSQL JSONB for efficient querying

        // Indexes
        builder.HasIndex(pt => pt.TenantId);
        builder.HasIndex(pt => pt.StripePaymentIntentId).IsUnique();
        builder.HasIndex(pt => pt.Status);
        builder.HasIndex(pt => pt.CreatedAt);
        builder.HasIndex(pt => new { pt.TenantId, pt.Status });
    }
}
