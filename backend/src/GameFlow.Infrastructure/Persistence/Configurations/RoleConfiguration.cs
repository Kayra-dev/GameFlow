using GameFlow.Domain.Entities;
using GameFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameFlow.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Name).HasMaxLength(64).IsRequired();
        builder.Property(r => r.DisplayName).HasMaxLength(64).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(256);

        builder.HasIndex(r => r.Name).IsUnique();

        // Roller sabittir; migration ile birlikte oluşturulur.
        builder.HasData(
            new Role
            {
                Id = (int)SystemRole.Admin,
                Name = nameof(SystemRole.Admin),
                DisplayName = "Yönetici",
                Description = "Tüm sistem üzerinde tam yetkiye sahiptir."
            },
            new Role
            {
                Id = (int)SystemRole.TeamLeader,
                Name = nameof(SystemRole.TeamLeader),
                DisplayName = "Takım Lideri",
                Description = "Kendi takımını, görevlerini ve sprintlerini yönetir."
            },
            new Role
            {
                Id = (int)SystemRole.TeamMember,
                Name = nameof(SystemRole.TeamMember),
                DisplayName = "Takım Üyesi",
                Description = "Kendisine atanan görevler üzerinde çalışır."
            });
    }
}
