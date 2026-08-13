using GameFlow.Domain.Common;
using GameFlow.Domain.Enums;

namespace GameFlow.Domain.Entities;

/// <summary>Sprint. Proje bazlı, istenirse tek bir takıma özel olabilir.</summary>
public class Sprint : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Guid? TeamId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Sprint hedefi.</summary>
    public string? Goal { get; set; }

    public SprintStatus Status { get; set; } = SprintStatus.Planned;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>Sprintin fiilen başlatıldığı an.</summary>
    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    /// <summary>Sprint kapatılırken girilen retrospektif notları.</summary>
    public string? RetrospectiveNotes { get; set; }

    public Project Project { get; set; } = null!;
    public Team? Team { get; set; }
    public ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
}
