using GameFlow.Application.Common.Interfaces;
using GameFlow.Infrastructure.Identity;
using GameFlow.Infrastructure.Persistence;
using GameFlow.Infrastructure.Persistence.Seed;
using GameFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameFlow.Infrastructure;

/// <summary>Infrastructure katmanının servis kayıtları.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                ConnectionStringResolver.Resolve(configuration),
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    // Ücretsiz barındırma katmanlarında görülen geçici kopmalar için yeniden dene.
                    npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);

                    // Detay sorguları birden fazla koleksiyon projeksiyonu içeriyor
                    // (üyeler + görev sayıları gibi). Tek sorguda kartezyen çarpım
                    // oluşmaması için EF Core bunları ayrı sorgulara böler.
                    npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });

            // Mantıksal silme filtresi yalnızca ana varlıklarda (User, Team, Project, Task ...)
            // tanımlıdır. Bağımlı kayıtlar (TeamMembers, Sprints, Labels ...) her zaman ana
            // varlığın kimliğine göre sorgulandığı ve ana varlık silindiğinde servis katmanı
            // tarafından temizlendiği için, bağımlılara ayrıca navigasyon üzerinden filtre
            // eklenmez; bu her sorguya gereksiz JOIN maliyeti bindirirdi.
            options.ConfigureWarnings(warnings => warnings.Ignore(
                CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        });

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        // FileStorageOptions kaydı AddFileStorage içinde yapılır; sağlayıcıya göre
        // taban yol da orada düzeltiliyor.

        AddPasswordHasher(services, configuration);

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        AddFileStorage(services, configuration);
        services.AddScoped<DatabaseInitializer>();

        // Anlık iletim varsayılan olarak etkisizdir; API katmanı SignalR hub'ını
        // kaydettiğinde bu kayıt ezilir.
        services.AddScoped<IRealtimeNotifier, NullRealtimeNotifier>();
        services.AddScoped<IChatNotifier, NullChatNotifier>();

        return services;
    }

    /// <summary>
    /// Şifre saklama stratejisini seçer. Varsayılan BCrypt'tir; düz metin yalnızca
    /// geliştirme ortamında ve açıkça istendiğinde devreye girer.
    /// </summary>
    /// <summary>
    /// Dosya depolamayı yapılandırır. Veritabanı sağlayıcısı seçildiğinde istemcinin
    /// dosyaya eriştiği taban yol da değişir: statik dosya kökü yerine dosya uç
    /// noktası kullanılır. Ayar elle verilmemişse burada düzeltilir, aksi halde
    /// üretilen bağlantılar 404 dönerdi.
    /// </summary>
    private static void AddFileStorage(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(FileStorageOptions.SectionName);
        var options = section.Get<FileStorageOptions>() ?? new FileStorageOptions();

        if (options.Provider == FileStorageProvider.Database)
        {
            services.Configure<FileStorageOptions>(section);
            services.PostConfigure<FileStorageOptions>(configured =>
            {
                if (configured.PublicBasePath.TrimEnd('/').EndsWith("/uploads", StringComparison.OrdinalIgnoreCase))
                {
                    configured.PublicBasePath = "/api/files";
                }
            });

            services.AddScoped<IFileStorageService, DatabaseFileStorageService>();
            return;
        }

        services.Configure<FileStorageOptions>(section);
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
    }

    private static void AddPasswordHasher(IServiceCollection services, IConfiguration configuration)
    {
        var security = configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>()
                       ?? new SecurityOptions();

        if (!security.StorePasswordsAsPlainText)
        {
            services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
            return;
        }

        // Ortam adı ASPNETCORE_ENVIRONMENT üzerinden okunur. Development dışında
        // düz metin şifreye kesinlikle izin verilmez; yapılandırma yanlışlıkla
        // üretime taşınırsa uygulama sessizce güvensiz çalışmak yerine hiç açılmaz.
        var environment = configuration["ASPNETCORE_ENVIRONMENT"]
                          ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                          ?? "Production";

        if (!string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"'Security:StorePasswordsAsPlainText' yalnızca Development ortamında " +
                $"kullanılabilir (mevcut ortam: {environment}). Şifrelerin düz metin " +
                "saklanması üretimde tüm hesapların ele geçmesi anlamına gelir. " +
                "Bu ayarı kapatın veya appsettings.Development.json içinde bırakın.");
        }

        services.AddSingleton<IPasswordHasher, PlainTextPasswordHasher>();
    }
}
