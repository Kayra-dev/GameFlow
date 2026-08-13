using GameFlow.Application.Common.Models;
using GameFlow.Application.Features.Notifications;
using GameFlow.Application.Features.Notifications.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace GameFlow.Api.Controllers;

/// <summary>
/// Oturum sahibinin bildirimleri. Bildirim üretimi sunucu içi olaylarla yapılır;
/// dışarıdan bildirim oluşturulamaz.
/// </summary>
public class NotificationsController(INotificationQueryService notificationService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetList(
        [FromQuery] NotificationListRequest request,
        CancellationToken cancellationToken)
        => Ok(await notificationService.GetListAsync(request, cancellationToken));

    /// <summary>Bildirim çanındaki okunmamış sayısı.</summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken cancellationToken)
        => Ok(await notificationService.GetUnreadCountAsync(cancellationToken));

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await notificationService.MarkAsReadAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await notificationService.MarkAllAsReadAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await notificationService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
