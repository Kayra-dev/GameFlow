using GameFlow.Application.Common.Models;
using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.WorkItems.Dtos;

public class CreateWorkItemRequest
{
    public Guid ProjectId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public WorkItemStatus Status { get; set; } = WorkItemStatus.Pending;

    public WorkItemPriority Priority { get; set; } = WorkItemPriority.Medium;

    public WorkItemType Type { get; set; } = WorkItemType.Task;

    public Guid? AssigneeId { get; set; }

    public Guid? TeamId { get; set; }

    public Guid? SprintId { get; set; }

    public Guid? ParentId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }

    public decimal? EstimatedHours { get; set; }

    public int? StoryPoints { get; set; }

    public List<Guid> LabelIds { get; set; } = [];

    /// <summary>Görevle birlikte oluşturulacak kontrol listesi maddeleri.</summary>
    public List<string> ChecklistItems { get; set; } = [];
}

public class UpdateWorkItemRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public WorkItemPriority Priority { get; set; }

    public WorkItemType Type { get; set; }

    public Guid? AssigneeId { get; set; }

    public Guid? TeamId { get; set; }

    public Guid? SprintId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }

    public decimal? EstimatedHours { get; set; }

    public decimal? LoggedHours { get; set; }

    public int? StoryPoints { get; set; }

    public List<Guid> LabelIds { get; set; } = [];
}

/// <summary>
/// Kanban'da sürükle-bırak. Hedef kolon ve komşu kartlar gönderilir;
/// sunucu iki komşunun ortasını alarak yalnızca taşınan kaydı güncelle.
/// </summary>
public class MoveWorkItemRequest
{
    public WorkItemStatus TargetStatus { get; set; }

    /// <summary>Bırakılan konumun üstündeki kart (yoksa null → en üste).</summary>
    public Guid? PrecedingItemId { get; set; }

    /// <summary>Bırakılan konumun altındaki kart (yoksa null → en alta).</summary>
    public Guid? FollowingItemId { get; set; }
}

public class ChangeStatusRequest
{
    public WorkItemStatus Status { get; set; }
}

public class AssignWorkItemRequest
{
    /// <summary>null gönderilirse atama kaldırılır.</summary>
    public Guid? AssigneeId { get; set; }
}

public class WorkItemListRequest : PagedRequest
{
    public Guid? ProjectId { get; set; }

    public Guid? TeamId { get; set; }

    public Guid? SprintId { get; set; }

    public Guid? AssigneeId { get; set; }

    /// <summary>Yalnızca oturum sahibine atanmış görevler.</summary>
    public bool OnlyMine { get; set; }

    public WorkItemStatus? Status { get; set; }

    public WorkItemPriority? Priority { get; set; }

    public WorkItemType? Type { get; set; }

    public Guid? LabelId { get; set; }

    public string? Search { get; set; }

    /// <summary>Yalnızca gecikmiş görevler.</summary>
    public bool OnlyOverdue { get; set; }

    /// <summary>Tamamlanan ve iptal edilen görevleri dışarıda bırakır.</summary>
    public bool OnlyActive { get; set; }

    public WorkItemSortField SortBy { get; set; } = WorkItemSortField.CreatedAt;

    public bool SortDescending { get; set; } = true;
}

public enum WorkItemSortField
{
    CreatedAt = 1,
    DueDate = 2,
    Priority = 3,
    Status = 4,
    Title = 5
}

public class CreateCommentRequest
{
    public string Content { get; set; } = string.Empty;

    public Guid? ParentCommentId { get; set; }
}

public class UpdateCommentRequest
{
    public string Content { get; set; } = string.Empty;
}

public class CreateChecklistItemRequest
{
    public string Text { get; set; } = string.Empty;
}

public class UpdateChecklistItemRequest
{
    public string Text { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }
}

public class CreateLabelRequest
{
    public string Name { get; set; } = string.Empty;

    public string ColorHex { get; set; } = "#64748B";
}
