using GameFlow.Domain.Common;

namespace GameFlow.Domain.Entities;

/// <summary>Görev yorumu. Yanıtlanabilir ve düzenlenebilir.</summary>
public class TaskComment : BaseEntity, ISoftDeletable
{
    public Guid WorkItemId { get; set; }

    public Guid AuthorId { get; set; }

    public string Content { get; set; } = string.Empty;

    public bool IsEdited { get; set; }

    public DateTime? EditedAt { get; set; }

    public Guid? ParentCommentId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedById { get; set; }

    public WorkItem WorkItem { get; set; } = null!;
    public User Author { get; set; } = null!;
    public TaskComment? ParentComment { get; set; }
    public ICollection<TaskComment> Replies { get; set; } = new List<TaskComment>();
}
