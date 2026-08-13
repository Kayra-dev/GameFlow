using System.Text;
using GameFlow.Domain.Enums;
using GameFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GameFlow.Api.Extensions;

/// <summary>Kimlik doğrulama ve yetkilendirme yapılandırması.</summary>
public static class AuthenticationExtensions
{
    /// <summary>Yalnızca yöneticilerin erişebildiği uç noktalar.</summary>
    public const string AdminPolicy = "AdminOnly";

    /// <summary>Yönetici veya takım lideri gerektiren uç noktalar.</summary>
    public const string LeaderPolicy = "LeaderOrAdmin";

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                  ?? throw new InvalidOperationException("JWT yapılandırması bulunamadı.");

        if (string.IsNullOrWhiteSpace(jwt.Secret) || jwt.Secret.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Secret en az 32 karakter olmalıdır. Üretimde ortam değişkeni ile verin.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ValidateLifetime = true,
                    // Süresi dolan token gerçekten reddedilsin diye varsayılan 5 dakikalık pay kaldırılır.
                    ClockSkew = TimeSpan.Zero
                };

                // SignalR tarayıcıda WebSocket için Authorization başlığı gönderemez;
                // token query string üzerinden alınır.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AdminPolicy, policy => policy.RequireRole(nameof(SystemRole.Admin)))
            .AddPolicy(LeaderPolicy, policy => policy.RequireRole(
                nameof(SystemRole.Admin),
                nameof(SystemRole.TeamLeader)));

        return services;
    }
}
