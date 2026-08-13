using System.Collections.Concurrent;

namespace GameFlow.Api.Hubs;

/// <summary>
/// Çevrimiçi kullanıcıları bağlantı sayısıyla izler.
///
/// Aynı kullanıcı birden fazla sekme açabildiği için bağlantılar sayılır:
/// ilk bağlantıda kullanıcı çevrimiçi olur, son bağlantı kapandığında çevrimdışı.
/// Tek sunucu örneği için bellekte tutulur; birden fazla örneğe ölçeklenirse
/// bu sınıfın yerine Redis backplane kullanılmalıdır.
/// </summary>
public class PresenceTracker
{
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _connections = new();

    /// <summary>Bağlantıyı kaydeder. Kullanıcı yeni çevrimiçi olduysa true döner.</summary>
    public bool TrackConnection(Guid userId, string connectionId)
    {
        var isFirstConnection = false;

        _connections.AddOrUpdate(
            userId,
            _ =>
            {
                isFirstConnection = true;
                return [connectionId];
            },
            (_, existing) =>
            {
                lock (existing)
                {
                    existing.Add(connectionId);
                }

                return existing;
            });

        return isFirstConnection;
    }

    /// <summary>Bağlantıyı kaldırır. Kullanıcının son bağlantısıysa true döner.</summary>
    public bool RemoveConnection(Guid userId, string connectionId)
    {
        if (!_connections.TryGetValue(userId, out var existing))
        {
            return false;
        }

        lock (existing)
        {
            existing.Remove(connectionId);

            if (existing.Count > 0)
            {
                return false;
            }
        }

        _connections.TryRemove(userId, out _);

        return true;
    }

    public IReadOnlyCollection<Guid> GetOnlineUserIds() => _connections.Keys.ToList();

    public bool IsOnline(Guid userId) => _connections.ContainsKey(userId);
}
