using GameFlow.Application.Common.Interfaces;

namespace GameFlow.Infrastructure.Identity;

/// <summary>
/// Şifreleri düz metin olarak saklayan <see cref="IPasswordHasher"/> uygulaması.
///
/// ⚠️ SADECE GELİŞTİRME ORTAMI İÇİNDİR — geliştirme sırasında şifreleri
/// veritabanından okuyabilmek için eklenmiştir. Üretimde
/// <see cref="BCryptPasswordHasher"/> kullanılır; bu sınıf üretimde
/// kaydedilmeye çalışılırsa uygulama başlatılmaz.
///
/// Düz metin değerler <c>plain:</c> önekiyle yazılır. Böylece:
/// <list type="bullet">
///   <item>Veritabanına bakan kişi değerin hash olmadığını hemen anlar.</item>
///   <item>Önek sayesinde eski BCrypt kayıtları ayırt edilebilir ve
///         doğrulama onlar için BCrypt'e devredilir; mod değiştirildiğinde
///         mevcut kullanıcılar sisteme kilitlenmez.</item>
/// </list>
/// </summary>
public class PlainTextPasswordHasher : IPasswordHasher
{
    /// <summary>Düz metin kayıtları hash'lerden ayıran önek.</summary>
    private const string Prefix = "plain:";

    private readonly BCryptPasswordHasher _bcryptFallback = new();

    public string Hash(string password) => Prefix + password;

    public bool Verify(string password, string passwordHash)
    {
        // Düz metin moduna geçmeden önce oluşturulmuş hesaplar hâlâ BCrypt
        // hash'i taşır; onları doğrulamayı BCrypt'e bırakırız.
        if (!passwordHash.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return _bcryptFallback.Verify(password, passwordHash);
        }

        var stored = passwordHash[Prefix.Length..];

        // Uzunluk sızdırmayan karşılaştırma; düz metin modunda kritik değil
        // ama davranışı BCrypt ile tutarlı tutar.
        return CryptographicEquals(stored, password);
    }

    private static bool CryptographicEquals(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var difference = 0;

        for (var index = 0; index < left.Length; index++)
        {
            difference |= left[index] ^ right[index];
        }

        return difference == 0;
    }
}
