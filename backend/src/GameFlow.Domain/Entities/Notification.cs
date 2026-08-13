using GameFlow.Domain.Common;
using GameFlow.Domain.Enums;

namespace GameFlow.Domain.Entities;

/// <summary>Kullanıcıya özel bildirim. SignalR ile anlık iletilir, veritabanında da saklanır.</summary>
public class Notification : BaseEntity
{
    /// <summary>Bildirimin sahibi.</summary>
    public Guid UserId { get; set; }

    /// <summary>Bildirime sebep olan kullanıcı.</summary>
    public Guid? ActorId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    /// <summary>Tıklandığında gidilecek istemci içi yol (örn. "/gorevler/ODY-42").</summary>
    public string? Link { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public string? EntityType { get; set; }

    public Guid? EntityId { get; set; }

    public User User { get; set; } = null!;
    public User? Actor { get; set; }
}
