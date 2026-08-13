using GameFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameFlow.Infrastructure.Persistence.Configurations;

public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ToTable("Announcements");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title).HasMaxLength(192).IsRequired();
        builder.Property(a => a.Content).HasMaxLength(8000).IsRequired();

        builder.HasIndex(a => new { a.IsPinned, a.PublishedAt });
        builder.HasIndex(a => a.ProjectId);

        builder.HasOne(a => a.Author)
            .WithMany(u => u.Announcements)
            .HasForeignKey(a => a.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Project)
            .WithMany(p => p.Announcements)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
