using GameFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameFlow.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName).HasMaxLength(128).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(u => u.JobTitle).HasMaxLength(128);
        builder.Property(u => u.AvatarUrl).HasMaxLength(512);
        builder.Property(u => u.Bio).HasMaxLength(1024);

        // Giriş sorgusu her istekte çalıştığı için e-posta üzerinde tekil index zorunlu.
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.RoleId);
        builder.HasIndex(u => u.IsDeleted);

        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
