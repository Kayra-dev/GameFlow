using GameFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameFlow.Infrastructure.Persistence.Configurations;

public class WorkItemLabelConfiguration : IEntityTypeConfiguration<WorkItemLabel>
{
    public void Configure(EntityTypeBuilder<WorkItemLabel> builder)
    {
        builder.ToTable("TaskLabels");

        builder.HasKey(x => new { x.WorkItemId, x.LabelId });

        builder.HasIndex(x => x.LabelId);

        builder.HasOne(x => x.WorkItem)
            .WithMany(t => t.Labels)
            .HasForeignKey(x => x.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Label)
            .WithMany(l => l.WorkItems)
            .HasForeignKey(x => x.LabelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
