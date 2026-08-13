using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.Teams.Dtos;

/// <summary>Kenar çubuğu ve listelerde kullanılan takım gösterimi.</summary>
public record TeamSummaryDto(
    Guid Id,
    string Name,
    TeamCategory Category,
    string ColorHex,
    string? IconKey,
    int MemberCount,
    UserSummaryDto? Leader);

/// <summary>Takım sayfasının üst bilgisi: üyeler, ilerleme ve sohbet odası.</summary>
public record TeamDetailDto(
    Guid Id,
    string Name,
    TeamCategory Category,
    string ColorHex,
    string? IconKey,
    int MemberCount,
    UserSummaryDto? Leader,
    string? Description,
    DateTime CreatedAt,
    IReadOnlyList<TeamMemberDto> Members,
    Guid? ChatRoomId,
    int ProgressPercent,
    int TotalTaskCount,
    int CompletedTaskCount,
    int ActiveTaskCount,
    int OverdueTaskCount);

public record TeamMemberDto(
    Guid Id,
    UserSummaryDto User,
    TeamRole TeamRole,
    DateTime JoinedAt);
