using GameFlow.Application.Features.Chat.Dtos;

namespace GameFlow.Application.Common.Interfaces;

/// <summary>
/// Sohbet olaylarının anlık yayını. Uygulama katmanı SignalR'ı tanımaz;
/// gerçek yayın API katmanındaki hub uygulaması tarafından yapılır.
/// </summary>
public interface IChatNotifier
{
    Task MessageReceivedAsync(Guid roomId, MessageDto message, CancellationToken cancellationToken = default);

    Task MessageEditedAsync(Guid roomId, MessageDto message, CancellationToken cancellationToken = default);

    Task MessageDeletedAsync(Guid roomId, Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>Bir kullanıcının hangi mesajları okuduğunu odadakilere bildirir.</summary>
    Task MessagesReadAsync(
        Guid roomId,
        Guid userId,
        IReadOnlyCollection<Guid> messageIds,
        CancellationToken cancellationToken = default);
}
