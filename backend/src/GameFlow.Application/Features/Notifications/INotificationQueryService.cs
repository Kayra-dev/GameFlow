using GameFlow.Application.Common.Models;
using GameFlow.Application.Features.Notifications.Dtos;

namespace GameFlow.Application.Features.Notifications;

/// <summary>
/// Bildirimlerin okunması ve okundu işaretlenmesi.
/// Üretim tarafı <see cref="Common.Interfaces.INotificationService"/> içindedir;
/// okuma ve yazma sorumlulukları ayrı tutulur.
/// </summary>
public interface INotificationQueryService
{
    Task<PagedResult<NotificationDto>> GetListAsync(
        NotificationListRequest request,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class NotificationListRequest : PagedRequest
{
    /// <summary>Yalnızca okunmamış bildirimler.</summary>
    public bool OnlyUnread { get; set; }
}
