using GameFlow.Domain.Common;
using GameFlow.Domain.Enums;

namespace GameFlow.Domain.Entities;

/// <summary>
/// Görev (Tasks tablosu). Sınıf adı, <see cref="System.Threading.Tasks.Task"/> ile
/// karışmaması için WorkItem seçilmiştir; tablo adı "Tasks" olarak eşlenir.
/// </summary>
public class WorkItem : BaseEntity, ISoftDeletable
{
    public Guid ProjectId { get; set; }

    public Guid? TeamId { get; set; }

    public Guid? SprintId { get; set; }

    /// <summary>Proje içinde artan sıra numarası.</summary>
    public int Number { get; set; }

    /// <summary>Okunabilir görev anahtarı (örn. "ODY-42"). Proje anahtarı + numara.</summary>
    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public WorkItemStatus Status { get; set; } = WorkItemStatus.Pending;

    public WorkItemPriority Priority { get; set; } = WorkItemPriority.Medium;

    public WorkItemType Type { get; set; } = WorkItemType.Task;

    public Guid? AssigneeId { get; set; }

    /// <summary>Görevi oluşturan kullanıcı.</summary>
    public Guid? ReporterId { get; set; }

    public DateTime? StartDate { get; set; }

    /// <summary>Son teslim tarihi. Deadline uyarıları bu alandan hesaplanır.</summary>
    public DateTime? DueDate { get; set; }

    public decimal? EstimatedHours { get; set; }

    public decimal? LoggedHours { get; set; }

    /// <summary>Scrum puanı; sprint raporlarında kullanılır.</summary>
    public int? StoryPoints { get; set; }

    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Kanban kolonu içindeki sıralama değeri. Sürükle-bırakta iki komşunun ortası
    /// alınarak yalnızca tek satır güncellenir.
    /// </summary>
    public double BoardOrder { get; set; }

    /// <summary>Alt görev ise üst görevin kimliği.</summary>
    public Guid? ParentId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedById { get; set; }

    public Project Project { get; set; } = null!;
    public Team? Team { get; set; }
    public Sprint? Sprint { get; set; }
    public User? Assignee { get; set; }
    public User? Reporter { get; set; }
    public WorkItem? Parent { get; set; }

    public ICollection<WorkItem> SubItems { get; set; } = new List<WorkItem>();
    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
    public ICollection<TaskChecklistItem> ChecklistItems { get; set; } = new List<TaskChecklistItem>();
    public ICollection<WorkItemLabel> Labels { get; set; } = new List<WorkItemLabel>();
    public ICollection<ActivityLog> Activities { get; set; } = new List<ActivityLog>();
}
