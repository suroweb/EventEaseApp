using EventEase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventEase.Infrastructure.Data.Configurations;

public class AIAgentUsageConfiguration : IEntityTypeConfiguration<AIAgentUsage>
{
    public void Configure(EntityTypeBuilder<AIAgentUsage> builder)
    {
        builder.ToTable("ai_agent_usages");

        builder.HasKey(au => au.Id);

        builder.Property(au => au.CreditsCost)
            .HasPrecision(18, 2);

        builder.Property(au => au.ProviderCost)
            .HasPrecision(18, 6);

        builder.Property(au => au.ModelUsed)
            .HasMaxLength(100);

        // Store large text fields efficiently
        builder.Property(au => au.PromptInput)
            .HasColumnType("text");

        builder.Property(au => au.SystemPrompt)
            .HasColumnType("text");

        builder.Property(au => au.AgentResponse)
            .HasColumnType("text");

        // Indexes
        builder.HasIndex(au => au.TenantId);
        builder.HasIndex(au => au.UserId);
        builder.HasIndex(au => au.AgentType);
        builder.HasIndex(au => au.Provider);
        builder.HasIndex(au => au.CreatedAt);
        builder.HasIndex(au => new { au.TenantId, au.AgentType, au.CreatedAt });
        builder.HasIndex(au => au.IsSuccess);

        // Relationships
        builder.HasOne(au => au.Event)
            .WithMany()
            .HasForeignKey(au => au.EventId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
