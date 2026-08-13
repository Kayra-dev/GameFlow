using GameFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameFlow.Infrastructure.Persistence.Configurations;

public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("StoredFiles");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.FileName).HasMaxLength(256).IsRequired();
        builder.Property(f => f.StoredFileName).HasMaxLength(128).IsRequired();
        builder.Property(f => f.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(f => f.Folder).HasMaxLength(64).IsRequired();

        builder.Property(f => f.Content).IsRequired();

        // Dosyalar her zaman erişim anahtarıyla aranır; benzersizlik ayrıca
        // aynı adın iki kez üretilmesini engeller.
        builder.HasIndex(f => f.StoredFileName).IsUnique();
    }
}
