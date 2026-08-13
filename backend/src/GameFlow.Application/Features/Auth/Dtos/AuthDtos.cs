using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.Auth.Dtos;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;

    public string? JobTitle { get; set; }

    public string? Bio { get; set; }
}

/// <summary>Oturum sahibinin arayüzde ihtiyaç duyduğu asgari bilgiler.</summary>
public record CurrentUserDto(
    Guid Id,
    string FullName,
    string Email,
    string? JobTitle,
    string? AvatarUrl,
    SystemRole Role,
    bool MustChangePassword,
    IReadOnlyList<Guid> LedTeamIds);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    CurrentUserDto User);
