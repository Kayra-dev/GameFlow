namespace GameFlow.Infrastructure.Services;

/// <summary>Dosyaların nerede saklanacağı.</summary>
public enum FileStorageProvider
{
    /// <summary>Sunucunun yerel diski. Kalıcı disk gerektirir.</summary>
    Local = 0,

    /// <summary>Veritabanı. Kalıcı diski olmayan ortamlar için.</summary>
    Database = 1
}

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Depolama sağlayıcısı. Kalıcı disk bağlanamayan ortamlarda (örn. ücretsiz
    /// PaaS planları) <see cref="FileStorageProvider.Database"/> seçilmelidir;
    /// aksi halde ekler her yeniden başlatmada kaybolur.
    /// </summary>
    public FileStorageProvider Provider { get; set; } = FileStorageProvider.Local;

    /// <summary>Dosyaların yazılacağı kök dizin. Yalnızca Local sağlayıcıda kullanılır.</summary>
    public string RootPath { get; set; } = "uploads";

    /// <summary>
    /// İstemcinin dosyalara eriştiği taban yol. Local'de statik dosya kökü
    /// (<c>/uploads</c>), Database'de dosya uç noktası (<c>/api/files</c>) olur.
    /// </summary>
    public string PublicBasePath { get; set; } = "/uploads";

    /// <summary>İzin verilen tek dosya boyutu (bayt).</summary>
    public long MaxFileSizeBytes { get; set; } = 52_428_800; // 50 MB
}
