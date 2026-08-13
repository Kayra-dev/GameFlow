using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Common.Models;
using GameFlow.Domain.Entities;
using GameFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GameFlow.Infrastructure.Services;

/// <summary>
/// Dosyaları veritabanında (<c>bytea</c>) saklar.
///
/// Kalıcı diski olmayan barındırma ortamları içindir: ücretsiz PaaS planlarında
/// konteynerin dosya sistemi her yeniden başlatmada sıfırlanır ve diske yazılan
/// ekler kaybolur. Veritabanı kalıcı olduğundan ekler de kalıcı olur.
///
/// Dosya içeriği belleğe alındığı için boyut sınırı disk uygulamasına göre daha
/// önemlidir; <c>FileStorage:MaxFileSizeBytes</c> buna göre ayarlanmalıdır.
/// </summary>
public class DatabaseFileStorageService(
    ApplicationDbContext context,
    IOptions<FileStorageOptions> options) : IFileStorageService
{
    private readonly FileStorageOptions _options = options.Value;

    public async Task<StoredFileResult> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var extension = FileTypeRules.EnsureAllowedExtension(originalFileName);

        // Akış uzunluğu biliniyorsa belleğe almadan önce reddedilir.
        if (content.CanSeek)
        {
            FileTypeRules.EnsureWithinSizeLimit(content.Length, _options.MaxFileSizeBytes);
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        // Uzunluk baştan bilinmiyorsa (chunked yükleme) kopyalama sonrası denetlenir.
        FileTypeRules.EnsureWithinSizeLimit(buffer.Length, _options.MaxFileSizeBytes);

        var safeFolder = FileTypeRules.SanitizeFolder(folder);
        var storedFileName = $"{Guid.CreateVersion7():N}{extension}";

        var stored = new StoredFile
        {
            FileName = Path.GetFileName(originalFileName),
            StoredFileName = storedFileName,
            ContentType = contentType,
            SizeBytes = buffer.Length,
            Folder = safeFolder,
            Content = buffer.ToArray()
        };

        context.StoredFiles.Add(stored);
        await context.SaveChangesAsync(cancellationToken);

        return new StoredFileResult(
            stored.FileName,
            storedFileName,
            contentType,
            stored.SizeBytes,
            FileTypeRules.ResolveCategory(extension),
            $"{_options.PublicBasePath}/{storedFileName}");
    }

    public async Task DeleteAsync(
        string storedFileName,
        string folder,
        CancellationToken cancellationToken = default)
    {
        await context.StoredFiles
            .Where(f => f.StoredFileName == storedFileName)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
