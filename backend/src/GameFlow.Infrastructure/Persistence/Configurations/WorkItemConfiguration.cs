using GameFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameFlow.Infrastructure.Persistence.Configurations;

public class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        // Tablo adı, istenen şemaya uygun olarak "Tasks" olarak eşlenir.
        builder.ToTable("Tasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).HasMaxLength(256).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(8000);
        builder.Property(t => t.Key).HasMaxLength(24).IsRequired();
        builder.Property(t => t.EstimatedHours).HasPrecision(8, 2);
        builder.Property(t => t.LoggedHours).HasPrecision(8, 2);

        // Görev anahtarı proje içinde tekildir (ODY-42).
        builder.HasIndex(t => t.Key).IsUnique();
        builder.HasIndex(t => new { t.ProjectId, t.Number }).IsUnique();

        // Kanban ve liste sorgularının çalıştığı asıl erişim yolları.
        builder.HasIndex(t => new { t.ProjectId, t.Status, t.BoardOrder });
        builder.HasIndex(t => new { t.AssigneeId, t.Status });
        builder.HasIndex(t => new { t.TeamId, t.Status });
        builder.HasIndex(t => t.SprintId);
        // Deadline uyarıları ve "geciken görevler" sorgusu için.
        builder.HasIndex(t => t.DueDate);
        builder.HasIndex(t => t.IsDeleted);

        builder.HasOne(t => t.Project)
            .WithMany(p => p.WorkItems)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Takım veya sprint silinse bile görev kaybolmaz, yalnızca ilişkisi boşalır.
        builder.HasOne(t => t.Team)
            .WithMany(tm => tm.WorkItems)
            .HasForeignKey(t => t.TeamId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Sprint)
            .WithMany(s => s.WorkItems)
            .HasForeignKey(t => t.SprintId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Assignee)
            .WithMany(u => u.AssignedWorkItems)
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Reporter)
            .WithMany(u => u.ReportedWorkItems)
            .HasForeignKey(t => t.ReporterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Parent)
            .WithMany(t => t.SubItems)
            .HasForeignKey(t => t.ParentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
