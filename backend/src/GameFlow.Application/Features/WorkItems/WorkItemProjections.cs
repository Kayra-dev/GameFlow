using System.Linq.Expressions;
using GameFlow.Application.Features.Shared.Dtos;
using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Application.Features.WorkItems.Dtos;
using GameFlow.Domain.Entities;
using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.WorkItems;

/// <summary>
/// Görev projeksiyonları. Kanban, liste, arama ve dashboard aynı gösterimi
/// paylaştığı için tek bir yerde tanımlanır ve tüm sorgular buradan beslenir.
/// </summary>
public static class WorkItemProjections
{
    /// <summary>
    /// Özet projeksiyonu. <paramref name="now"/> parametresi deadline hesaplaması
    /// için gereklidir; sunucu saatinin sorguya gömülmesi yerine dışarıdan verilir
    /// ki tüm satırlar aynı referans zamanı kullansın.
    /// </summary>
    public static Expression<Func<WorkItem, WorkItemSummaryDto>> ToSummary(DateTime now)
        => item => new WorkItemSummaryDto(
            item.Id,
            item.Key,
            item.Title,
            item.Status,
            item.Priority,
            item.Type,
            item.StartDate,
            item.DueDate,
            item.BoardOrder,
            item.Assignee == null
                ? null
                : new UserSummaryDto(
                    item.Assignee.Id,
                    item.Assignee.FullName,
                    item.Assignee.Email,
                    item.Assignee.JobTitle,
                    item.Assignee.AvatarUrl,
                    (SystemRole)item.Assignee.RoleId,
                    item.Assignee.IsOnline,
                    item.Assignee.LastSeenAt),
            item.ProjectId,
            item.Project.Key,
            item.Project.Name,
            item.TeamId,
            item.Team == null ? null : item.Team.Name,
            item.SprintId,
            item.StoryPoints,
            item.Labels
                .Select(l => new LabelDto(l.Label.Id, l.Label.Name, l.Label.ColorHex))
                .ToList(),
            item.Comments.Count,
            item.Attachments.Count,
            item.ChecklistItems.Count,
            item.ChecklistItems.Count(c => c.IsCompleted),
            item.SubItems.Count,
            item.DueDate == null
                ? null
                : (int)(item.DueDate.Value.Date - now.Date).TotalDays,
            item.DueDate != null
            && item.DueDate < now
            && item.Status != WorkItemStatus.Done
            && item.Status != WorkItemStatus.Cancelled);

    /// <summary>Kanban kolon başlıkları.</summary>
    public static string GetStatusLabel(WorkItemStatus status) => status switch
    {
        WorkItemStatus.Pending => "Bekliyor",
        WorkItemStatus.Todo => "Yapılacak",
        WorkItemStatus.InProgress => "Devam Ediyor",
        WorkItemStatus.CodeReview => "Kod İncelemede",
        WorkItemStatus.Testing => "Testte",
        WorkItemStatus.Done => "Tamamlandı",
        WorkItemStatus.Cancelled => "İptal",
        _ => status.ToString()
    };

    public static string GetPriorityLabel(WorkItemPriority priority) => priority switch
    {
        WorkItemPriority.Lowest => "En Düşük",
        WorkItemPriority.Low => "Düşük",
        WorkItemPriority.Medium => "Orta",
        WorkItemPriority.High => "Yüksek",
        WorkItemPriority.Critical => "Kritik",
        _ => priority.ToString()
    };

    /// <summary>Kanban kolonlarının ekrandaki sırası.</summary>
    public static readonly WorkItemStatus[] BoardColumnOrder =
    [
        WorkItemStatus.Pending,
        WorkItemStatus.Todo,
        WorkItemStatus.InProgress,
        WorkItemStatus.CodeReview,
        WorkItemStatus.Testing,
        WorkItemStatus.Done,
        WorkItemStatus.Cancelled
    ];
}
