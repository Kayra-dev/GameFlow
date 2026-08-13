namespace GameFlow.Api.Hubs;

/// <summary>
/// SignalR grup adlarını tek yerde üretir. Hub'lar ve yayıncılar aynı adı
/// kullanmak zorunda olduğu için elle dize birleştirmekten kaçınılır.
/// </summary>
public static class HubGroups
{
    /// <summary>Bir kullanıcının tüm açık sekmeleri/cihazları.</summary>
    public static string User(Guid userId) => $"user:{userId}";

    /// <summary>Bir sohbet odasını açık tutan istemciler.</summary>
    public static string ChatRoom(Guid roomId) => $"room:{roomId}";

    /// <summary>Bir projenin kanban panosunu açık tutan istemciler.</summary>
    public static string Project(Guid projectId) => $"project:{projectId}";
}
