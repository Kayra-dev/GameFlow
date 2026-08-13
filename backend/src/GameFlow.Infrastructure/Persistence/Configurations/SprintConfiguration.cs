using GameFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameFlow.Infrastructure.Persistence.Configurations;

public class SprintConfiguration : IEntityTypeConfiguration<Sprint>
{
    public void Configure(EntityTypeBuilder<Sprint> builder)
    {
        builder.ToTable("Sprints");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(128).IsRequired();
        builder.Property(s => s.Goal).HasMaxLength(1024);
        builder.Property(s => s.RetrospectiveNotes).HasMaxLength(4000);

        builder.HasIndex(s => new { s.ProjectId, s.Status });
        builder.HasIndex(s => new { s.StartDate, s.EndDate });

        builder.HasOne(s => s.Project)
            .WithMany(p => p.Sprints)
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Team)
            .WithMany(t => t.Sprints)
            .HasForeignKey(s => s.TeamId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
