using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace GameFlow.Api.Extensions;

/// <summary>appsettings üzerinden ayarlanabilen istek hızı sınırları.</summary>
public class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Giriş denemeleri: IP başına, dakikada. Kaba kuvvete karşı sıkı tutulur.</summary>
    public int LoginPermitPerMinute { get; set; } = 10;

    /// <summary>
    /// Token yenileme: kullanıcı başına, dakikada. Girişten ayrı ve daha geniştir;
    /// birden fazla sekme açan kullanıcı yalnızca yenilemeler yüzünden kilitlenmemeli.
    /// </summary>
    public int RefreshPermitPerMinute { get; set; } = 30;

    /// <summary>Genel API kullanımı: kullanıcı (yoksa IP) başına, dakikada.</summary>
    public int GlobalPermitPerMinute { get; set; } = 300;

    /// <summary>Dosya yükleme: kullanıcı başına, dakikada.</summary>
    public int UploadPermitPerMinute { get; set; } = 30;
}

/// <summary>
/// İstek hızı kısıtlaması. Giriş, token yenileme, dosya yükleme ve genel kullanım
/// ayrı politikalara bağlanır; biri diğerinin bütçesini tüketmez.
/// </summary>
public static class RateLimitingExtensions
{
    public const string LoginPolicy = "login";
    public const string RefreshPolicy = "refresh";
    public const string GlobalPolicy = "global";
    public const string UploadPolicy = "upload";

    public static IServiceCollection AddGameFlowRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var limits = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>()
                     ?? new RateLimitOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // İstemcinin ne zaman tekrar denemesi gerektiğini bilmesi için.
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }

                context.HttpContext.Response.ContentType = "application/problem+json";

                await context.HttpContext.Response.WriteAsync(
                    """{"title":"Çok fazla istek gönderdiniz. Lütfen biraz bekleyip tekrar deneyin.","status":429}""",
                    cancellationToken);
            };

            // Giriş: kimlik doğrulanmadığı için yalnızca IP'ye göre bölünebilir.
            options.AddPolicy(LoginPolicy, context => FixedWindow(
                PartitionByIp(context),
                limits.LoginPermitPerMinute));

            // Yenileme: token gövdede geldiği için IP'ye göre bölünür ama
            // giriş bütçesinden ayrıdır ve daha geniştir.
            options.AddPolicy(RefreshPolicy, context => FixedWindow(
                PartitionByIp(context),
                limits.RefreshPermitPerMinute));

            options.AddPolicy(GlobalPolicy, context => FixedWindow(
                PartitionByUserOrIp(context),
                limits.GlobalPermitPerMinute));

            options.AddPolicy(UploadPolicy, context => FixedWindow(
                PartitionByUserOrIp(context),
                limits.UploadPermitPerMinute));
        });

        return services;
    }

    private static RateLimitPartition<string> FixedWindow(string partitionKey, int permitLimit)
        => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1)
            });

    private static string PartitionByIp(HttpContext context)
        => context.Connection.RemoteIpAddress?.ToString() ?? "bilinmeyen-ip";

    /// <summary>
    /// Kimliği doğrulanmış kullanıcılar kendi bütçelerini kullanır; böylece aynı
    /// ofis ağındaki kullanıcılar birbirinin hakkını tüketmez.
    /// </summary>
    private static string PartitionByUserOrIp(HttpContext context)
        => context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
           ?? PartitionByIp(context);
}
