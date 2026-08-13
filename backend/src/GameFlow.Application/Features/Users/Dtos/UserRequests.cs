using GameFlow.Application.Common.Models;
using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.Users.Dtos;

/// <summary>Yeni kullanıcı oluşturma isteği. Yalnızca yöneticiler kullanabilir.</summary>
public class CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public SystemRole Role { get; set; } = SystemRole.TeamMember;

    public string? JobTitle { get; set; }

    public string? Bio { get; set; }

    /// <summary>Kullanıcı ilk girişinde şifresini değiştirmek zorunda olsun mu.</summary>
    public bool MustChangePassword { get; set; } = true;

    /// <summary>Oluşturulurken doğrudan eklenecek takımlar.</summary>
    public List<Guid> TeamIds { get; set; } = [];
}

public class UpdateUserRequest
{
    public string FullName { get; set; } = string.Empty;

    public SystemRole Role { get; set; }

    public string? JobTitle { get; set; }

    public string? Bio { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Yöneticinin bir kullanıcının şifresini sıfırlaması.</summary>
public class ResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;

    public bool MustChangePassword { get; set; } = true;
}

public class UserListRequest : PagedRequest
{
    /// <summary>Ad veya e-posta içinde geçen metin.</summary>
    public string? Search { get; set; }

    public SystemRole? Role { get; set; }

    public Guid? TeamId { get; set; }

    public bool? IsActive { get; set; }
}
