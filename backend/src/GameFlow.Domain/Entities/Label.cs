using GameFlow.Domain.Common;

namespace GameFlow.Domain.Entities;

/// <summary>Görev etiketi. Proje bazlıdır.</summary>
public class Label : BaseEntity
{
    public Guid ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ColorHex { get; set; } = "#64748B";

    public Project Project { get; set; } = null!;

    public ICollection<WorkItemLabel> WorkItems { get; set; } = new List<WorkItemLabel>();
}
