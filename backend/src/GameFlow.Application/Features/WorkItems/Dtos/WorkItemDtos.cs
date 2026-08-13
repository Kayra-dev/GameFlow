using GameFlow.Application.Features.Shared.Dtos;
using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.WorkItems.Dtos;

/// <summary>Kanban kartı ve görev listelerinde kullanılan gösterim.</summary>
public record WorkItemSummaryDto(
    Guid Id,
    string Key,
    string Title,
    WorkItemStatus Status,
    WorkItemPriority Priority,
    WorkItemType Type,
    DateTime? StartDate,
    DateTime? DueDate,
    double BoardOrder,
    UserSummaryDto? Assignee,
    Guid ProjectId,
    string ProjectKey,
    string ProjectName,
    Guid? TeamId,
    string? TeamName,
    Guid? SprintId,
    int? StoryPoints,
    IReadOnlyList<LabelDto> Labels,
    int CommentCount,
    int AttachmentCount,
    int ChecklistTotal,
    int ChecklistCompleted,
    int SubItemCount,
    /// <summary>Son teslim tarihine kalan gün. null ise tarih yok, negatif ise gecikmiş.</summary>
    int? DaysUntilDue,
    bool IsOverdue);

public record WorkItemDetailDto(
    Guid Id,
    string Key,
    string Title,
    WorkItemStatus Status,
    WorkItemPriority Priority,
    WorkItemType Type,
    DateTime? StartDate,
    DateTime? DueDate,
    double BoardOrder,
    UserSummaryDto? Assignee,
    Guid ProjectId,
    string ProjectKey,
    string ProjectName,
    Guid? TeamId,
    string? TeamName,
    Guid? SprintId,
    string? SprintName,
    int? StoryPoints,
    IReadOnlyList<LabelDto> Labels,
    int CommentCount,
    int AttachmentCount,
    int ChecklistTotal,
    int ChecklistCompleted,
    int? DaysUntilDue,
    bool IsOverdue,
    string? Description,
    decimal? EstimatedHours,
    decimal? LoggedHours,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    UserSummaryDto? Reporter,
    Guid? ParentId,
    string? ParentKey,
    IReadOnlyList<WorkItemSummaryDto> SubItems,
    IReadOnlyList<ChecklistItemDto> ChecklistItems,
    IReadOnlyList<AttachmentDto> Attachments,
    IReadOnlyList<CommentDto> Comments,
    IReadOnlyList<ActivityDto> Activities);

public record ChecklistItemDto(
    Guid Id,
    string Text,
    bool IsCompleted,
    int Order,
    DateTime? CompletedAt);

public record CommentDto(
    Guid Id,
    string Content,
    UserSummaryDto Author,
    DateTime CreatedAt,
    bool IsEdited,
    DateTime? EditedAt,
    Guid? ParentCommentId);

/// <summary>Tek bir kanban kolonu ve içindeki kartlar.</summary>
public record KanbanColumnDto(
    WorkItemStatus Status,
    string Title,
    int TotalCount,
    IReadOnlyList<WorkItemSummaryDto> Items);

public record KanbanBoardDto(
    Guid ProjectId,
    string ProjectKey,
    IReadOnlyList<KanbanColumnDto> Columns);

/// <summary>Dashboard ve takım sayfasındaki deadline özetleri.</summary>
public record DeadlineOverviewDto(
    IReadOnlyList<WorkItemSummaryDto> DueToday,
    IReadOnlyList<WorkItemSummaryDto> Upcoming,
    IReadOnlyList<WorkItemSummaryDto> Overdue);
