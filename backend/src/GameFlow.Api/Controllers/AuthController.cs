using GameFlow.Api.Extensions;
using GameFlow.Application.Features.Auth;
using GameFlow.Application.Features.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameFlow.Api.Controllers;

/// <summary>
/// Kimlik doğrulama uç noktaları. Sistemde kayıt (register) uç noktası bulunmaz;
/// kullanıcılar yalnızca yönetici tarafından oluşturulur.
/// </summary>
public class AuthController(IAuthService authService) : ApiControllerBase
{
    /// <summary>E-posta ve şifre ile oturum açar.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.LoginPolicy)]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
        => Ok(await authService.LoginAsync(request, ClientIpAddress, cancellationToken));

    /// <summary>Refresh token ile yeni erişim tokenı alır.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.RefreshPolicy)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
        => Ok(await authService.RefreshAsync(request, ClientIpAddress, cancellationToken));

    /// <summary>Refresh tokenı iptal ederek oturumu kapatır.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Oturum sahibinin güncel bilgilerini döner.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
        => Ok(await authService.GetCurrentUserAsync(cancellationToken));

    /// <summary>Oturum sahibinin şifresini değiştirir.</summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await authService.ChangePasswordAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Oturum sahibinin profil bilgilerini günceller.</summary>
    [HttpPut("profile")]
    public async Task<ActionResult<CurrentUserDto>> UpdateProfile(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
        => Ok(await authService.UpdateProfileAsync(request, cancellationToken));
}
