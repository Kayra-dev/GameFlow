using GameFlow.Domain.Entities;

namespace GameFlow.Application.Common.Interfaces;

public interface IJwtTokenService
{
    /// <summary>Kullanıcı için imzalı erişim tokenı ve geçerlilik süresini üretir.</summary>
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user);

    /// <summary>Kriptografik olarak güvenli rastgele refresh token üretir.</summary>
    string CreateRefreshToken();

    TimeSpan RefreshTokenLifetime { get; }
}
