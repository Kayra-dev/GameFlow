using GameFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameFlow.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(128).IsRequired();
        builder.Property(p => p.Key).HasMaxLength(10).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(4000);
        builder.Property(p => p.ColorHex).HasMaxLength(9).IsRequired();
        builder.Property(p => p.Genre).HasMaxLength(64);
        builder.Property(p => p.Platforms).HasMaxLength(256);
        builder.Property(p => p.CoverImageUrl).HasMaxLength(512);

        builder.HasIndex(p => p.Key).IsUnique();
        builder.HasIndex(p => p.Status);

        builder.HasOne(p => p.CreatedBy)
            .WithMany()
            .HasForeignKey(p => p.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
