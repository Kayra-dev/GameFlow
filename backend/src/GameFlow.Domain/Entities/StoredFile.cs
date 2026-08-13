using GameFlow.Domain.Common;

namespace GameFlow.Domain.Entities;

/// <summary>
/// Yüklenen dosyanın veritabanındaki karşılığı.
///
/// Dosyalar normalde diskte tutulur; bu varlık, kalıcı diski olmayan barındırma
/// ortamları (örn. ücretsiz PaaS planları) için vardır. Orada konteynerin dosya
/// sistemi her yeniden başlatmada sıfırlandığından ekler kaybolur; veritabanı ise
/// kalıcıdır.
/// </summary>
public class StoredFile : BaseEntity
{
    /// <summary>Kullanıcının yüklediği özgün ad. Yalnızca gösterim için.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Erişim anahtarı: GUID v7 + uzantı. Tahmin edilemez ve çakışmaz.</summary>
    public string StoredFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    /// <summary>Mantıksal klasör (örn. "gorevler", "sohbet"). Ayıklamayı kolaylaştırır.</summary>
    public string Folder { get; set; } = string.Empty;

    /// <summary>Dosyanın kendisi. PostgreSQL'de <c>bytea</c> olarak saklanır.</summary>
    public byte[] Content { get; set; } = [];
}
