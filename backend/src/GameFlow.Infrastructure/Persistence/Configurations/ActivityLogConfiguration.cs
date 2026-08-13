using GameFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameFlow.Infrastructure.Persistence.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Description).HasMaxLength(512).IsRequired();
        builder.Property(l => l.EntityType).HasMaxLength(64);
        builder.Property(l => l.MetadataJson).HasColumnType("jsonb");

        // "Son aktiviteler" akışı tarihe göre okunur.
        builder.HasIndex(l => l.CreatedAt);
        builder.HasIndex(l => new { l.ProjectId, l.CreatedAt });
        builder.HasIndex(l => new { l.ActorId, l.CreatedAt });

        // Denetim kayıtları, ilişkili kayıt silinse bile korunur.
        builder.HasOne(l => l.Actor)
            .WithMany(u => u.Activities)
            .HasForeignKey(l => l.ActorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.Project)
            .WithMany()
            .HasForeignKey(l => l.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.Team)
            .WithMany()
            .HasForeignKey(l => l.TeamId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.WorkItem)
            .WithMany(t => t.Activities)
            .HasForeignKey(l => l.WorkItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
