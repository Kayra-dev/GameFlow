using GameFlow.Domain.Enums;
using GameFlow.Domain.Exceptions;

namespace GameFlow.Infrastructure.Services;

/// <summary>
/// Dosya türü kuralları. Depolama uygulamalarının (disk, veritabanı) ortak
/// kullandığı doğrulama ve sınıflandırma mantığı burada tek yerde durur;
/// aksi halde iki uygulamanın izin listeleri zamanla birbirinden ayrılırdı.
/// </summary>
public static class FileTypeRules
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

    /// <summary>Uzantıyı doğrular ve küçük harfe indirgenmiş hâlini döner.</summary>
    public static string EnsureAllowedExtension(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new DomainException($"'{extension}' uzantılı dosyalara izin verilmiyor.");
        }

        return extension.ToLowerInvariant();
    }

    public static void EnsureWithinSizeLimit(long sizeBytes, long maxSizeBytes)
    {
        if (sizeBytes > maxSizeBytes)
        {
            var limitMb = maxSizeBytes / 1_048_576;
            throw new DomainException($"Dosya boyutu {limitMb} MB sınırını aşıyor.");
        }
    }

    /// <summary>Klasör adından dizin geçişine yol açabilecek karakterleri temizler.</summary>
    public static string SanitizeFolder(string folder)
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
