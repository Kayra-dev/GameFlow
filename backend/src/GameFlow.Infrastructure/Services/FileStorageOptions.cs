namespace GameFlow.Infrastructure.Services;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Dosyaların yazılacağı kök dizin.</summary>
    public string RootPath { get; set; } = "uploads";

    /// <summary>İstemcinin dosyalara eriştiği taban yol.</summary>
    public string PublicBasePath { get; set; } = "/uploads";

    /// <summary>İzin verilen tek dosya boyutu (bayt).</summary>
    public long MaxFileSizeBytes { get; set; } = 52_428_800; // 50 MB
}
