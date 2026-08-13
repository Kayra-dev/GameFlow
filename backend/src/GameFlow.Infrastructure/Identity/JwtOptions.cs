namespace GameFlow.Infrastructure.Identity;

/// <summary>appsettings / ortam değişkenlerinden okunan JWT yapılandırması.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "GameFlow";

    public string Audience { get; set; } = "GameFlowClient";

    /// <summary>HMAC imzalama anahtarı. Üretimde ortam değişkeni ile verilir.</summary>
    public string Secret { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 30;

    public int RefreshTokenDays { get; set; } = 14;
}
