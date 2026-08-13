using GameFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameFlow.Infrastructure.Persistence.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(96).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(1024);
        builder.Property(t => t.ColorHex).HasMaxLength(9).IsRequired();
        builder.Property(t => t.IconKey).HasMaxLength(64);

        builder.HasIndex(t => t.Name).IsUnique();
        builder.HasIndex(t => t.Category);

        // Lider silinirse takım kaydı korunur, yalnızca lider alanı boşalır.
        builder.HasOne(t => t.Leader)
            .WithMany()
            .HasForeignKey(t => t.LeaderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
