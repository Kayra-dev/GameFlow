using Microsoft.Extensions.Configuration;
using Npgsql;

namespace GameFlow.Infrastructure.Persistence;

/// <summary>
/// Bağlantı dizesini çözer. Render/Railway/Heroku gibi platformlar bağlantıyı
/// <c>postgres://kullanici:sifre@host:port/veritabani</c> biçiminde bir URI olarak
/// <c>DATABASE_URL</c> ortam değişkeninde verir; Npgsql ise anahtar/değer biçimi bekler.
/// Bu sınıf iki biçimi de kabul eder.
/// </summary>
public static class ConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var databaseUrl = configuration["DATABASE_URL"];

        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return FromUri(databaseUrl);
        }

        var connectionString = configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Veritabanı bağlantı bilgisi bulunamadı. 'ConnectionStrings:Default' veya " +
                "'DATABASE_URL' değerini tanımlayın.");
        }

        return connectionString.StartsWith("postgres", StringComparison.OrdinalIgnoreCase)
               && connectionString.Contains("://", StringComparison.Ordinal)
            ? FromUri(connectionString)
            : connectionString;
    }

    private static string FromUri(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var credentials = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty,
            // Yönetilen PostgreSQL servisleri TLS zorunlu tutar; sertifika zinciri
            // container içinde doğrulanamadığı için Require kullanılır.
            SslMode = SslMode.Require,
            Pooling = true,
            MaxPoolSize = 20
        };

        return builder.ConnectionString;
    }
}
