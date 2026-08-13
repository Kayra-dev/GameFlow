using GameFlow.Application.Features.Notifications.Dtos;

namespace GameFlow.Application.Common.Interfaces;

/// <summary>
/// Bildirim üretimi. Kayıtlar veritabanına yazılır ve
/// <see cref="IRealtimeNotifier"/> aracılığıyla anlık olarak iletilir.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Bildirimleri kuyruğa alır. Kayıtlar çağıranın SaveChanges'i ile birlikte yazılır;
    /// anlık iletim <see cref="FlushAsync"/> çağrısında yapılır.
    /// </summary>
    void Queue(NotificationRequest request);

    void QueueMany(IEnumerable<NotificationRequest> requests);

    /// <summary>
    /// Kuyruğa alınmış bildirimleri veritabanına yazar ve bağlı istemcilere iletir.
    /// Çağıran, kendi SaveChanges'inden sonra bunu çağırmalıdır.
    /// </summary>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
