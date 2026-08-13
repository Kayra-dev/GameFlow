using GameFlow.Domain.Common;

namespace GameFlow.Domain.Entities;

/// <summary>Kullanıcı–proje ilişkisi.</summary>
public class ProjectMember : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Proje ayarlarını yönetme yetkisi (proje yöneticisi).</summary>
    public bool IsManager { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Project Project { get; set; } = null!;
    public User User { get; set; } = null!;
}
