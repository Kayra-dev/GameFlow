using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Common.Models;
using GameFlow.Domain.Enums;
using Microsoft.Extensions.Options;

namespace GameFlow.Infrastructure.Services;

/// <summary>
/// Dosyaları sunucunun yerel diskine yazar. Dosya adları GUID ile yeniden üretildiği için
/// path traversal ve ad çakışması riski ortadan kalkar.
///
/// Kalıcı disk gerektirir: konteyner tabanlı ortamlarda disk bağlanmamışsa dosyalar
/// yeniden başlatmada kaybolur. O durumda <see cref="DatabaseFileStorageService"/>
/// kullanılmalıdır.
/// </summary>
public class LocalFileStorageService(IOptions<FileStorageOptions> options) : IFileStorageService
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

        if (content.CanSeek)
        {
            FileTypeRules.EnsureWithinSizeLimit(content.Length, _options.MaxFileSizeBytes);
        }

        var safeFolder = FileTypeRules.SanitizeFolder(folder);
        var directory = Path.Combine(_options.RootPath, safeFolder);
        Directory.CreateDirectory(directory);

        var storedFileName = $"{Guid.CreateVersion7():N}{extension}";
        var fullPath = Path.Combine(directory, storedFileName);

        await using (var target = File.Create(fullPath))
        {
            await content.CopyToAsync(target, cancellationToken);
        }

        var sizeBytes = new FileInfo(fullPath).Length;

        return new StoredFileResult(
            Path.GetFileName(originalFileName),
            storedFileName,
            contentType,
            sizeBytes,
            FileTypeRules.ResolveCategory(extension),
            $"{_options.PublicBasePath}/{safeFolder}/{storedFileName}");
    }

    public Task DeleteAsync(string storedFileName, string folder, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(
            _options.RootPath,
            FileTypeRules.SanitizeFolder(folder),
            Path.GetFileName(storedFileName));

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public static AttachmentCategory ResolveCategory(string extension)
        => FileTypeRules.ResolveCategory(extension);
}
