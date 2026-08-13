using System.Security.Claims;
using GameFlow.Application.Common.Interfaces;
using GameFlow.Domain.Enums;
using GameFlow.Domain.Exceptions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace GameFlow.Api.Services;

/// <summary>Aktif isteğin JWT claim'lerinden kullanıcı bilgisini okur.</summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email)
                            ?? Principal?.FindFirstValue(JwtRegisteredClaimNames.Email);

    public SystemRole? Role
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<SystemRole>(value, ignoreCase: true, out var role) ? role : null;
        }
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true && UserId.HasValue;

    public bool IsAdmin => Role == SystemRole.Admin;

    public Guid RequireUserId()
        => UserId ?? throw new UnauthorizedException("Oturum bilgisi okunamadı. Lütfen tekrar giriş yapın.");
}
