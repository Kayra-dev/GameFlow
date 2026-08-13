using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.Chat.Dtos;

namespace GameFlow.Infrastructure.Services;

/// <summary>
/// SignalR devrede değilken kullanılan etkisiz sohbet yayıncısı.
/// API katmanı hub uygulamasını kaydettiğinde bu kayıt ezilir.
/// </summary>
public class NullChatNotifier : IChatNotifier
{
    public Task MessageReceivedAsync(
        Guid roomId,
        MessageDto message,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task MessageEditedAsync(
        Guid roomId,
        MessageDto message,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task MessageDeletedAsync(
        Guid roomId,
        Guid messageId,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task MessagesReadAsync(
        Guid roomId,
        Guid userId,
        IReadOnlyCollection<Guid> messageIds,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
