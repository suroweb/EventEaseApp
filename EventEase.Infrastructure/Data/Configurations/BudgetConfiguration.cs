using EventEase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventEase.Infrastructure.Data.Configurations;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("budgets");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.TotalBudget)
            .HasPrecision(18, 2);

        builder.Property(b => b.AllocatedAmount)
            .HasPrecision(18, 2);

        builder.Property(b => b.SpentAmount)
            .HasPrecision(18, 2);

        builder.Property(b => b.Currency)
            .HasMaxLength(3);

        // Indexes
        builder.HasIndex(b => b.TenantId);
        builder.HasIndex(b => b.EventId).IsUnique();
        builder.HasIndex(b => b.IsApproved);

        // Relationships
        builder.HasOne(b => b.ApprovedByUser)
            .WithMany()
            .HasForeignKey(b => b.ApprovedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(b => b.AIAgentUsage)
            .WithMany()
            .HasForeignKey(b => b.AIAgentUsageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(b => b.BudgetItems)
            .WithOne(bi => bi.Budget)
            .HasForeignKey(bi => bi.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
