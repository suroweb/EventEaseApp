using EventEase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventEase.Infrastructure.Data.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("invitations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.MessageBody)
            .HasColumnType("text");

        builder.Property(i => i.TrackingToken)
            .IsRequired()
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(i => i.TenantId);
        builder.HasIndex(i => i.EventId);
        builder.HasIndex(i => i.GuestId);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.TrackingToken).IsUnique();
        builder.HasIndex(i => i.ScheduledSendAt);
        builder.HasIndex(i => i.SentAt);
        builder.HasIndex(i => new { i.EventId, i.Status });

        // Relationships
        builder.HasOne(i => i.SentByUser)
            .WithMany()
            .HasForeignKey(i => i.SentByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.AIAgentUsage)
            .WithMany()
            .HasForeignKey(i => i.AIAgentUsageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Registration)
            .WithOne(r => r.Invitation)
            .HasForeignKey<EventRegistration>(r => r.InvitationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
