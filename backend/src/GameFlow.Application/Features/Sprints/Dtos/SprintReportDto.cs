using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.Sprints.Dtos;

/// <summary>Sprint kapanış/ilerleme raporu.</summary>
public record SprintReportDto(
    Guid SprintId,
    string SprintName,
    SprintStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    int TotalTaskCount,
    int CompletedTaskCount,
    int CancelledTaskCount,
    int RemainingTaskCount,
    int OverdueTaskCount,
    int TotalStoryPoints,
    int CompletedStoryPoints,
    int ProgressPercent,
    /// <summary>Tamamlanan görev / toplam görev oranına göre başarı yüzdesi.</summary>
    int SuccessPercent,
    decimal TotalEstimatedHours,
    decimal TotalLoggedHours,
    IReadOnlyList<SprintStatusBreakdownDto> StatusBreakdown,
    IReadOnlyList<SprintMemberContributionDto> MemberContributions);

public record SprintStatusBreakdownDto(WorkItemStatus Status, string Label, int Count);

public record SprintMemberContributionDto(
    UserSummaryDto User,
    int AssignedCount,
    int CompletedCount,
    int StoryPoints);
