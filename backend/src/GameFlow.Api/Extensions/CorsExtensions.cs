namespace GameFlow.Api.Extensions;

/// <summary>
/// Frontend GitHub Pages üzerinde ayrı bir origin'de çalıştığı için CORS zorunludur.
/// İzinli origin listesi yapılandırmadan okunur; joker karakter kullanılmaz çünkü
/// SignalR kimlik doğrulaması için credentials gönderilmesi gerekir.
/// </summary>
public static class CorsExtensions
{
    public const string PolicyName = "GameFlowCors";

    public static IServiceCollection AddGameFlowCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(PolicyName, policy =>
        {
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                // Sayfalama üstbilgisinin istemciden okunabilmesi için.
                .WithExposedHeaders("X-Total-Count");
        }));

        return services;
    }
}
