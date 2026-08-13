namespace GameFlow.Infrastructure.Identity;

/// <summary>Şifre saklama davranışını belirleyen güvenlik ayarları.</summary>
public class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// Şifreleri veritabanına düz metin olarak yazar.
    ///
    /// SADECE GELİŞTİRME İÇİNDİR. Açıkken veritabanı sızıntısı tüm hesapların
    /// ele geçmesi anlamına gelir. Üretim ortamında açık bırakılırsa uygulama
    /// başlatılmaz (bkz. DependencyInjection içindeki denetim).
    /// </summary>
    public bool StorePasswordsAsPlainText { get; set; }
}
