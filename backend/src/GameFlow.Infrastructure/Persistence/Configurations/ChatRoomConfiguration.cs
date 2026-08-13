using GameFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameFlow.Infrastructure.Persistence.Configurations;

public class ChatRoomConfiguration : IEntityTypeConfiguration<ChatRoom>
{
    public void Configure(EntityTypeBuilder<ChatRoom> builder)
    {
        builder.ToTable("ChatRooms");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(128).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(512);

        builder.HasIndex(r => r.Type);
        // Bir takımın yalnızca bir sohbet odası olur.
        builder.HasIndex(r => r.TeamId).IsUnique().HasFilter("\"TeamId\" IS NOT NULL");

        builder.HasOne(r => r.Team)
            .WithMany(t => t.ChatRooms)
            .HasForeignKey(r => r.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Project)
            .WithMany(p => p.ChatRooms)
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
