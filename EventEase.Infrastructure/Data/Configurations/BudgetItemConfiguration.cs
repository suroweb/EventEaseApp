using EventEase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventEase.Infrastructure.Data.Configurations;

public class BudgetItemConfiguration : IEntityTypeConfiguration<BudgetItem>
{
    public void Configure(EntityTypeBuilder<BudgetItem> builder)
    {
        builder.ToTable("budget_items");

        builder.HasKey(bi => bi.Id);

        builder.Property(bi => bi.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(bi => bi.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(bi => bi.EstimatedCost)
            .HasPrecision(18, 2);

        builder.Property(bi => bi.ActualCost)
            .HasPrecision(18, 2);

        // Indexes
        builder.HasIndex(bi => bi.TenantId);
        builder.HasIndex(bi => bi.BudgetId);
        builder.HasIndex(bi => bi.Category);
        builder.HasIndex(bi => bi.IsPaid);
    }
}
