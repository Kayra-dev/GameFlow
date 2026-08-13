using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Common.Models;
using GameFlow.Domain.Enums;
using GameFlow.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace GameFlow.Infrastructure.Services;

/// <summary>
/// Dosyaları sunucunun yerel diskine yazar. Dosya adları GUID ile yeniden üretildiği için
/// path traversal ve ad çakışması riski ortadan kalkar.
/// </summary>
public class LocalFileStorageService(IOptions<FileStorageOptions> options) : IFileStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".bmp",
        ".pdf",
        ".zip", ".rar", ".7z",
        ".doc", ".docx", ".txt", ".md", ".rtf",
        ".xls", ".xlsx", ".csv",
        ".mp4", ".webm", ".mov", ".avi", ".mkv",
        ".mp3", ".wav", ".ogg", ".flac",
        ".psd", ".fbx", ".obj", ".blend", ".unitypackage"
    };

    private readonly FileStorageOptions _options = options.Value;

    public async Task<StoredFileResult> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new DomainException($"'{extension}' uzantılı dosyalara izin verilmiyor.");
        }

        if (content.CanSeek && content.Length > _options.MaxFileSizeBytes)
        {
            var limitMb = _options.MaxFileSizeBytes / 1_048_576;
            throw new DomainException($"Dosya boyutu {limitMb} MB sınırını aşıyor.");
        }

        var safeFolder = SanitizeFolder(folder);
        var directory = Path.Combine(_options.RootPath, safeFolder);
        Directory.CreateDirectory(directory);

        var storedFileName = $"{Guid.CreateVersion7():N}{extension.ToLowerInvariant()}";
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
            ResolveCategory(extension),
            $"{_options.PublicBasePath}/{safeFolder}/{storedFileName}");
    }

    public Task DeleteAsync(string storedFileName, string folder, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_options.RootPath, SanitizeFolder(folder), Path.GetFileName(storedFileName));

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    /// <summary>Klasör adından dizin geçişine yol açabilecek karakterleri temizler.</summary>
    private static string SanitizeFolder(string folder)
    {
        var segments = folder
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .Where(segment => segment != "." && segment != "..")
            .Select(segment => string.Concat(segment.Where(char.IsLetterOrDigit)));

        var sanitized = string.Join('/', segments.Where(segment => segment.Length > 0));

        return sanitized.Length == 0 ? "genel" : sanitized;
    }

    public static AttachmentCategory ResolveCategory(string extension) => extension.ToLowerInvariant() switch
    {
        ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".svg" or ".bmp" => AttachmentCategory.Image,
        ".pdf" => AttachmentCategory.Pdf,
        ".zip" or ".rar" or ".7z" => AttachmentCategory.Archive,
        ".doc" or ".docx" or ".txt" or ".md" or ".rtf" => AttachmentCategory.Document,
        ".xls" or ".xlsx" or ".csv" => AttachmentCategory.Spreadsheet,
        ".mp4" or ".webm" or ".mov" or ".avi" or ".mkv" => AttachmentCategory.Video,
        ".mp3" or ".wav" or ".ogg" or ".flac" => AttachmentCategory.Audio,
        _ => AttachmentCategory.Other
    };
}
