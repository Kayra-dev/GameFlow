using GameFlow.Application.Features.Sprints.Dtos;
using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.Projects.Dtos;

/// <summary>Proje seçici ve liste kartlarında kullanılan gösterim.</summary>
public record ProjectSummaryDto(
    Guid Id,
    string Name,
    string Key,
    ProjectStatus Status,
    string ColorHex,
    string? CoverImageUrl,
    int MemberCount,
    int TaskCount,
    int CompletedTaskCount);

public record ProjectDetailDto(
    Guid Id,
    string Name,
    string Key,
    ProjectStatus Status,
    string ColorHex,
    string? CoverImageUrl,
    int MemberCount,
    int TaskCount,
    int CompletedTaskCount,
    string? Description,
    string? Genre,
    string? Platforms,
    DateTime? StartDate,
    DateTime? TargetReleaseDate,
    DateTime CreatedAt,
    IReadOnlyList<ProjectMemberDto> Members,
    SprintSummaryDto? ActiveSprint,
    int OverdueTaskCount,
    int ProgressPercent);

public record ProjectMemberDto(
    Guid Id,
    UserSummaryDto User,
    bool IsManager,
    DateTime JoinedAt);
