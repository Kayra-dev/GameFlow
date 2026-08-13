namespace GameFlow.Domain.Entities;

/// <summary>Görev ile etiket arasındaki çoktan çoğa ilişki tablosu.</summary>
public class WorkItemLabel
{
    public Guid WorkItemId { get; set; }

    public Guid LabelId { get; set; }

    public WorkItem WorkItem { get; set; } = null!;
    public Label Label { get; set; } = null!;
}
