using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.Users.Dtos;

/// <summary>Listelerde ve atama alanlarında kullanılan hafif kullanıcı gösterimi.</summary>
public record UserSummaryDto(
    Guid Id,
    string FullName,
    string Email,
    string? JobTitle,
    string? AvatarUrl,
    SystemRole Role,
    bool IsOnline,
    DateTime? LastSeenAt);

/// <summary>Profil ve yönetim ekranlarında kullanılan ayrıntılı kullanıcı gösterimi.</summary>
public record UserDetailDto(
    Guid Id,
    string FullName,
    string Email,
    string? JobTitle,
    string? AvatarUrl,
    SystemRole Role,
    bool IsOnline,
    DateTime? LastSeenAt,
    string? Bio,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<UserTeamDto> Teams,
    IReadOnlyList<UserProjectDto> Projects,
    int CompletedTaskCount,
    int ActiveTaskCount);

public record UserTeamDto(Guid Id, string Name, TeamCategory Category, string ColorHex, TeamRole TeamRole);

public record UserProjectDto(Guid Id, string Name, string Key, string ColorHex);
