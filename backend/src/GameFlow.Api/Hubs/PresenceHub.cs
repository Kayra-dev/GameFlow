using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.Notifications.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Api.Hubs;

/// <summary>
/// Bildirimler ve çevrimiçi durum. Her bağlantı kullanıcının kendi grubuna
/// eklenir; böylece bildirimler kullanıcının tüm açık sekmelerine ulaşır.
/// </summary>
[Authorize]
public class PresenceHub(
    ICurrentUserService currentUser,
    IApplicationDbContext context,
    PresenceTracker presenceTracker,
    IDateTimeProvider dateTime,
    ILogger<PresenceHub> logger) : Hub<IPresenceClient>
{
    public override async Task OnConnectedAsync()
    {
        if (currentUser.UserId is not { } userId)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.User(userId));

        var becameOnline = presenceTracker.TrackConnection(userId, Context.ConnectionId);

        // Son görülme zamanı her bağlantıda yenilenir; dashboard bayrağı bayat
        // kalmış kayıtları bu alandan ayıklar.
        await UpdatePresenceAsync(userId, isOnline: true, Context.ConnectionAborted);

        if (becameOnline)
        {
            await Clients.Others.UserOnline(userId);

            logger.LogDebug("Kullanıcı {UserId} çevrimiçi oldu.", userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (currentUser.UserId is { } userId)
        {
            var becameOffline = presenceTracker.RemoveConnection(userId, Context.ConnectionId);

            if (becameOffline)
            {
                // Yayın önce yapılır: veritabanı yazımı başarısız olsa bile diğer
                // istemciler kullanıcının ayrıldığını öğrenmeli.
                await Clients.Others.UserOffline(userId);

                // DİKKAT: Bu noktada Context.ConnectionAborted zaten iptal edilmiş
                // durumdadır; onu iletmek güncellemenin sessizce düşmesine yol açar.
                await UpdatePresenceAsync(userId, isOnline: false, CancellationToken.None);

                logger.LogDebug("Kullanıcı {UserId} çevrimdışı oldu.", userId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Bağlı olan kullanıcıların kimlikleri (çevrimiçi göstergeleri için).</summary>
    public IReadOnlyCollection<Guid> GetOnlineUsers() => presenceTracker.GetOnlineUserIds();

    /// <summary>
    /// Kanban panosunu açan istemci, aynı projedeki değişiklikleri almak için
    /// proje grubuna katılır.
    /// </summary>
    public async Task JoinProject(Guid projectId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Project(projectId));

    public async Task LeaveProject(Guid projectId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Project(projectId));

    private async Task UpdatePresenceAsync(
        Guid userId,
        bool isOnline,
        CancellationToken cancellationToken)
    {
        var now = dateTime.UtcNow;

        // Tek satırlık güncelleme; varlığı belleğe yüklemeye gerek yok.
        await context.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.IsOnline, isOnline)
                    .SetProperty(u => u.LastSeenAt, now),
                cancellationToken);
    }
}

/// <summary>İstemcinin dinlediği bildirim ve çevrimiçi durum olayları.</summary>
public interface IPresenceClient
{
    Task NotificationReceived(NotificationDto notification);

    Task UnreadCountChanged(int unreadCount);

    Task UserOnline(Guid userId);

    Task UserOffline(Guid userId);

    /// <summary>Panodaki bir görev değişti; istemci ilgili sorguyu tazeler.</summary>
    Task WorkItemChanged(Guid projectId, Guid workItemId, string changeKind);
}
