using GameFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameFlow.Infrastructure.Persistence.Configurations;

public class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.ToTable("Meetings");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Title).HasMaxLength(192).IsRequired();
        builder.Property(m => m.Description).HasMaxLength(2000);
        builder.Property(m => m.Location).HasMaxLength(192);
        builder.Property(m => m.MeetingUrl).HasMaxLength(512);

        builder.HasIndex(m => m.StartsAt);
        builder.HasIndex(m => new { m.ProjectId, m.StartsAt });
        builder.HasIndex(m => new { m.TeamId, m.StartsAt });

        builder.HasOne(m => m.Project)
            .WithMany(p => p.Meetings)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Team)
            .WithMany(t => t.Meetings)
            .HasForeignKey(m => m.TeamId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.Organizer)
            .WithMany(u => u.OrganizedMeetings)
            .HasForeignKey(m => m.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
