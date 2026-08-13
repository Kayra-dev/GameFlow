using GameFlow.Domain.Common;
using GameFlow.Domain.Enums;

namespace GameFlow.Domain.Entities;

/// <summary>
/// Denetim ve "son aktiviteler" akışı için kayıt. Salt yazılır, güncellenmez.
/// </summary>
public class ActivityLog : BaseEntity
{
    public Guid? ActorId { get; set; }

    public ActivityType Type { get; set; }

    /// <summary>Kullanıcıya gösterilecek Türkçe açıklama.</summary>
    public string Description { get; set; } = string.Empty;

    public Guid? ProjectId { get; set; }

    public Guid? TeamId { get; set; }

    public Guid? WorkItemId { get; set; }

    public string? EntityType { get; set; }

    public Guid? EntityId { get; set; }

    /// <summary>Alan değişikliği gibi ek veriler (jsonb).</summary>
    public string? MetadataJson { get; set; }

    public User? Actor { get; set; }
    public Project? Project { get; set; }
    public Team? Team { get; set; }
    public WorkItem? WorkItem { get; set; }
}
