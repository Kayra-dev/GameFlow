using GameFlow.Domain.Common;

namespace GameFlow.Domain.Entities;

/// <summary>Görev içindeki kontrol listesi maddesi.</summary>
public class TaskChecklistItem : BaseEntity
{
    public Guid WorkItemId { get; set; }

    public string Text { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }

    public int Order { get; set; }

    public DateTime? CompletedAt { get; set; }

    public Guid? CompletedById { get; set; }

    public WorkItem WorkItem { get; set; } = null!;
}
