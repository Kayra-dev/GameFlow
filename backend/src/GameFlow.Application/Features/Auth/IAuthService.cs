using GameFlow.Application.Features.Auth.Dtos;

namespace GameFlow.Application.Features.Auth;

public interface IAuthService
{
    /// <summary>E-posta ve şifre ile oturum açar.</summary>
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Refresh token ile yeni bir erişim tokenı üretir ve tokenı döndürerek rotasyon uygular.</summary>
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Verilen refresh tokenı iptal eder.</summary>
    Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>Oturum sahibinin güncel bilgilerini döner.</summary>
    Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>Oturum sahibinin şifresini değiştirir ve tüm refresh tokenlarını iptal eder.</summary>
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Oturum sahibinin kendi profil bilgilerini güncellemesi.</summary>
    Task<CurrentUserDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
}
