using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.Sprints.Dtos;

/// <summary>Sprint kartı ve dashboard göstergesi.</summary>
public record SprintSummaryDto(
    Guid Id,
    string Name,
    SprintStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    int TaskCount,
    int CompletedTaskCount,
    int ProgressPercent);

public record SprintDetailDto(
    Guid Id,
    string Name,
    SprintStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    int TaskCount,
    int CompletedTaskCount,
    int ProgressPercent,
    string? Goal,
    Guid ProjectId,
    string ProjectKey,
    Guid? TeamId,
    string? TeamName,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? RetrospectiveNotes,
    int TotalStoryPoints,
    int CompletedStoryPoints,
    /// <summary>Sprint bitiş tarihine kalan gün sayısı (negatifse gecikmiş).</summary>
    int RemainingDays);
