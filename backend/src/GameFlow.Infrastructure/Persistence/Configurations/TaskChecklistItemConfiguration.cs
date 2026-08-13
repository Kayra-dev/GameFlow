using GameFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameFlow.Infrastructure.Persistence.Configurations;

public class TaskChecklistItemConfiguration : IEntityTypeConfiguration<TaskChecklistItem>
{
    public void Configure(EntityTypeBuilder<TaskChecklistItem> builder)
    {
        builder.ToTable("TaskChecklistItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Text).HasMaxLength(512).IsRequired();

        builder.HasIndex(i => new { i.WorkItemId, i.Order });

        builder.HasOne(i => i.WorkItem)
            .WithMany(t => t.ChecklistItems)
            .HasForeignKey(i => i.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
