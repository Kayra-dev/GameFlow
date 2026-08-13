using GameFlow.Domain.Enums;

namespace GameFlow.Application.Common.Interfaces;

/// <summary>Aktif HTTP isteğini yapan kullanıcının kimlik bilgilerine erişim.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Email { get; }

    SystemRole? Role { get; }

    bool IsAuthenticated { get; }

    bool IsAdmin { get; }

    /// <summary>Kimlik doğrulanmışsa kullanıcı kimliğini döner, aksi halde istisna fırlatır.</summary>
    Guid RequireUserId();
}
