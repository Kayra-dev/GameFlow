using GameFlow.Domain.Common;
using GameFlow.Domain.Enums;

namespace GameFlow.Domain.Entities;

/// <summary>Kullanıcı–takım ilişkisi ve takım içi rolü.</summary>
public class TeamMember : BaseEntity
{
    public Guid TeamId { get; set; }

    public Guid UserId { get; set; }

    public TeamRole Role { get; set; } = TeamRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Team Team { get; set; } = null!;
    public User User { get; set; } = null!;
}
