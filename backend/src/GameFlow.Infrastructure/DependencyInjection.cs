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
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));

        AddPasswordHasher(services, configuration);

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
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
