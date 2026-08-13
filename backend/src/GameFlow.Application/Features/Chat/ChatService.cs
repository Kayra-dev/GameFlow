using System.Linq.Expressions;
using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.Chat.Dtos;
using GameFlow.Application.Features.Notifications.Dtos;
using GameFlow.Application.Features.Shared.Dtos;
using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Domain.Entities;
using GameFlow.Domain.Enums;
using GameFlow.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Application.Features.Chat;

/// <summary>
/// Sohbet odaları ve mesajlar.
///
/// Erişim kuralları:
/// <list type="bullet">
///   <item>Takım odası: yalnızca takım üyeleri (ve yöneticiler).</item>
///   <item>Lider odası: yalnızca en az bir takımın lideri olanlar ve yöneticiler.</item>
///   <item>Proje odası: yalnızca proje üyeleri.</item>
/// </list>
/// </summary>
public class ChatService(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IPermissionService permissions,
    IChatNotifier chatNotifier,
    INotificationService notifications,
    IFileStorageService fileStorage,
    IDateTimeProvider dateTime) : IChatService
{
    /// <summary>Oda listesinde gösterilecek en fazla okunmamış sayısı hesaplanan mesaj.</summary>
    private const int UnreadCountCap = 99;

    public async Task<IReadOnlyList<ChatRoomDto>> GetRoomsAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var teamIds = await context.TeamMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.TeamId)
            .ToListAsync(cancellationToken);

        var projectIds = await context.ProjectMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.ProjectId)
            .ToListAsync(cancellationToken);

        var isLeader = await permissions.IsAnyTeamLeaderAsync(cancellationToken);
        var isAdmin = currentUser.IsAdmin;

        var rooms = await context.ChatRooms
            .AsNoTracking()
            .Where(room =>
                // Team/Project navigasyonlarının null olması, ana kaydın mantıksal
                // olarak silindiği anlamına gelir (global query filter). Silinmiş bir
                // takımın odası listede görünmemeli; mesajlar denetim için veritabanında
                // korunur ama oda artık kullanıma sunulmaz.
                (room.Type == ChatRoomType.Team
                 && room.Team != null
                 && (isAdmin || teamIds.Contains(room.TeamId!.Value)))
                || (room.Type == ChatRoomType.Leaders && isLeader)
                || (room.Type == ChatRoomType.Project
                    && room.Project != null
                    && (isAdmin || projectIds.Contains(room.ProjectId!.Value))))
            .Select(room => new
            {
                room.Id,
                room.Name,
                room.Type,
                room.Description,
                room.TeamId,
                room.ProjectId,
                ColorHex = room.Team != null
                    ? room.Team.ColorHex
                    : room.Project != null ? room.Project.ColorHex : null,
                // Okunmamış: başkasının gönderdiği ve okundu kaydı olmayan mesajlar.
                UnreadCount = room.Messages
                    .Count(m => m.SenderId != userId && m.Reads.All(r => r.UserId != userId)),
                LastMessageId = room.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => (Guid?)m.Id)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        // Son mesajlar tek sorguda çekilir; oda başına ayrı sorgu açılmaz.
        var lastMessageIds = rooms
            .Where(room => room.LastMessageId.HasValue)
            .Select(room => room.LastMessageId!.Value)
            .ToList();

        var lastMessages = await context.Messages
            .AsNoTracking()
            .Where(m => lastMessageIds.Contains(m.Id))
            .Select(MessageProjection(userId))
            .ToDictionaryAsync(m => m.Id, cancellationToken);

        return rooms
            .Select(room => new ChatRoomDto(
                room.Id,
                room.Name,
                room.Type,
                room.Description,
                room.TeamId,
                room.ProjectId,
                room.ColorHex,
                Math.Min(room.UnreadCount, UnreadCountCap),
                room.LastMessageId.HasValue
                    ? lastMessages.GetValueOrDefault(room.LastMessageId.Value)
                    : null))
            // Mesajı olan odalar önce, en yeni etkinliğe göre sıralı.
            .OrderByDescending(room => room.LastMessage?.CreatedAt ?? DateTime.MinValue)
            .ThenBy(room => room.Name)
            .ToList();
    }

    public async Task<ChatRoomDto> GetRoomAsync(
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanAccessRoomAsync(roomId, cancellationToken);

        var rooms = await GetRoomsAsync(cancellationToken);

        return rooms.FirstOrDefault(room => room.Id == roomId)
               ?? throw new NotFoundException("Sohbet odası", roomId);
    }

    public async Task<ChatRoomDto> GetLeadersRoomAsync(CancellationToken cancellationToken = default)
    {
        if (!await permissions.IsAnyTeamLeaderAsync(cancellationToken))
        {
            throw new ForbiddenException("Lider sohbetine yalnızca takım liderleri erişebilir.");
        }

        var roomId = await context.ChatRooms
            .Where(room => room.Type == ChatRoomType.Leaders)
            .Select(room => (Guid?)room.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Lider sohbet odası", ChatRoomType.Leaders);

        return await GetRoomAsync(roomId, cancellationToken);
    }

    public async Task<MessagePageDto> GetMessagesAsync(
        Guid roomId,
        MessageHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanAccessRoomAsync(roomId, cancellationToken);

        var userId = currentUser.RequireUserId();

        var query = context.Messages
            .AsNoTracking()
            .Where(m => m.ChatRoomId == roomId);

        if (request.Before.HasValue)
        {
            query = query.Where(m => m.CreatedAt < request.Before.Value);
        }

        // Bir fazla kayıt çekilir; devamı olup olmadığı ekstra sorgu olmadan anlaşılır.
        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(request.PageSize + 1)
            .Select(MessageProjection(userId))
            .ToListAsync(cancellationToken);

        var hasMore = messages.Count > request.PageSize;

        if (hasMore)
        {
            messages.RemoveAt(messages.Count - 1);
        }

        // İstemci eskiden yeniye sıralı bekler.
        messages.Reverse();

        return new MessagePageDto(
            messages,
            hasMore,
            messages.Count > 0 ? messages[0].CreatedAt : null);
    }

    public async Task<MessageDto> SendMessageAsync(
        Guid roomId,
        SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var room = await GetAccessibleRoomAsync(roomId, cancellationToken);
        var senderId = currentUser.RequireUserId();

        await EnsureReplyTargetIsValidAsync(roomId, request.ReplyToMessageId, cancellationToken);

        var message = new Message
        {
            ChatRoomId = roomId,
            SenderId = senderId,
            Content = request.Content.Trim(),
            ReplyToMessageId = request.ReplyToMessageId
        };

        context.Messages.Add(message);

        // Gönderen kendi mesajını okumuş sayılır.
        context.MessageReads.Add(new MessageRead
        {
            MessageId = message.Id,
            UserId = senderId,
            ReadAt = dateTime.UtcNow
        });

        await QueueMessageNotificationsAsync(room, message, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        var dto = await GetMessageAsync(message.Id, senderId, cancellationToken);

        await chatNotifier.MessageReceivedAsync(roomId, dto, cancellationToken);
        await notifications.FlushAsync(cancellationToken);

        return dto;
    }

    public async Task<MessageDto> EditMessageAsync(
        Guid roomId,
        Guid messageId,
        EditMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanAccessRoomAsync(roomId, cancellationToken);

        var userId = currentUser.RequireUserId();

        var message = await context.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ChatRoomId == roomId, cancellationToken)
            ?? throw new NotFoundException("Mesaj", messageId);

        // Mesajı yalnızca göndereni düzenleyebilir; yöneticiler bile içeriği değiştiremez.
        if (message.SenderId != userId)
        {
            throw new ForbiddenException("Yalnızca kendi mesajınızı düzenleyebilirsiniz.");
        }

        message.Content = request.Content.Trim();
        message.IsEdited = true;
        message.EditedAt = dateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        var dto = await GetMessageAsync(messageId, userId, cancellationToken);

        await chatNotifier.MessageEditedAsync(roomId, dto, cancellationToken);

        return dto;
    }

    public async Task DeleteMessageAsync(
        Guid roomId,
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var room = await GetAccessibleRoomAsync(roomId, cancellationToken);
        var userId = currentUser.RequireUserId();

        var message = await context.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ChatRoomId == roomId, cancellationToken)
            ?? throw new NotFoundException("Mesaj", messageId);

        // Gönderen kendi mesajını, takım lideri/yönetici ise moderasyon amacıyla silebilir.
        if (message.SenderId != userId && !await CanModerateAsync(room, cancellationToken))
        {
            throw new ForbiddenException("Bu mesajı silme yetkiniz bulunmuyor.");
        }

        // Mantıksal silme: geçmişin bütünlüğü korunur, mesaj listelerde görünmez.
        context.Messages.Remove(message);

        await context.SaveChangesAsync(cancellationToken);

        await chatNotifier.MessageDeletedAsync(roomId, messageId, cancellationToken);
    }

    public async Task<int> MarkAsReadAsync(
        Guid roomId,
        MarkMessagesReadRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanAccessRoomAsync(roomId, cancellationToken);

        var userId = currentUser.RequireUserId();

        var query = context.Messages
            .Where(m => m.ChatRoomId == roomId
                        && m.SenderId != userId
                        && m.Reads.All(r => r.UserId != userId));

        if (request.MessageIds.Count > 0)
        {
            query = query.Where(m => request.MessageIds.Contains(m.Id));
        }

        var unreadIds = await query.Select(m => m.Id).ToListAsync(cancellationToken);

        if (unreadIds.Count > 0)
        {
            var now = dateTime.UtcNow;

            foreach (var messageId in unreadIds)
            {
                context.MessageReads.Add(new MessageRead
                {
                    MessageId = messageId,
                    UserId = userId,
                    ReadAt = now
                });
            }

            await context.SaveChangesAsync(cancellationToken);

            await chatNotifier.MessagesReadAsync(roomId, userId, unreadIds, cancellationToken);
        }

        return await context.Messages
            .CountAsync(
                m => m.ChatRoomId == roomId
                     && m.SenderId != userId
                     && m.Reads.All(r => r.UserId != userId),
                cancellationToken);
    }

    public async Task<IReadOnlyList<MessageReadReceiptDto>> GetReadReceiptsAsync(
        Guid roomId,
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanAccessRoomAsync(roomId, cancellationToken);

        var messageExists = await context.Messages
            .AnyAsync(m => m.Id == messageId && m.ChatRoomId == roomId, cancellationToken);

        if (!messageExists)
        {
            throw new NotFoundException("Mesaj", messageId);
        }

        return await context.MessageReads
            .AsNoTracking()
            .Where(r => r.MessageId == messageId)
            .OrderBy(r => r.ReadAt)
            .Select(r => new MessageReadReceiptDto(
                new UserSummaryDto(
                    r.User.Id,
                    r.User.FullName,
                    r.User.Email,
                    r.User.JobTitle,
                    r.User.AvatarUrl,
                    (SystemRole)r.User.RoleId,
                    r.User.IsOnline,
                    r.User.LastSeenAt),
                r.ReadAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<MessageDto> SendAttachmentAsync(
        Guid roomId,
        Stream content,
        string fileName,
        string contentType,
        string? caption,
        CancellationToken cancellationToken = default)
    {
        var room = await GetAccessibleRoomAsync(roomId, cancellationToken);
        var senderId = currentUser.RequireUserId();

        var stored = await fileStorage.SaveAsync(
            content,
            fileName,
            contentType,
            $"sohbet/{roomId:N}",
            cancellationToken);

        var message = new Message
        {
            ChatRoomId = roomId,
            SenderId = senderId,
            // Dosya mesajlarında içerik boş kalamayacağı için dosya adı kullanılır.
            Content = string.IsNullOrWhiteSpace(caption) ? stored.FileName : caption.Trim()
        };

        context.Messages.Add(message);

        context.MessageAttachments.Add(new MessageAttachment
        {
            MessageId = message.Id,
            FileName = stored.FileName,
            StoredFileName = stored.StoredFileName,
            ContentType = stored.ContentType,
            SizeBytes = stored.SizeBytes,
            Category = stored.Category,
            Url = stored.Url
        });

        context.MessageReads.Add(new MessageRead
        {
            MessageId = message.Id,
            UserId = senderId,
            ReadAt = dateTime.UtcNow
        });

        await QueueMessageNotificationsAsync(room, message, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        var dto = await GetMessageAsync(message.Id, senderId, cancellationToken);

        await chatNotifier.MessageReceivedAsync(roomId, dto, cancellationToken);
        await notifications.FlushAsync(cancellationToken);

        return dto;
    }

    public async Task EnsureCanAccessRoomAsync(
        Guid roomId,
        CancellationToken cancellationToken = default)
        => await GetAccessibleRoomAsync(roomId, cancellationToken);

    // ------------------------------------------------------------- Yardımcılar

    /// <summary>Odayı getirir ve türüne göre erişim yetkisini denetler.</summary>
    private async Task<ChatRoom> GetAccessibleRoomAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await context.ChatRooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken)
            ?? throw new NotFoundException("Sohbet odası", roomId);

        switch (room.Type)
        {
            case ChatRoomType.Team when room.TeamId.HasValue:
                await permissions.EnsureTeamMemberAsync(room.TeamId.Value, cancellationToken);
                break;

            case ChatRoomType.Project when room.ProjectId.HasValue:
                await permissions.EnsureProjectMemberAsync(room.ProjectId.Value, cancellationToken);
                break;

            case ChatRoomType.Leaders:
                if (!await permissions.IsAnyTeamLeaderAsync(cancellationToken))
                {
                    throw new ForbiddenException(
                        "Lider sohbetine yalnızca takım liderleri ve yöneticiler erişebilir.");
                }

                break;

            default:
                // Takım/proje bağı kopmuş bir oda kimseye açılmaz.
                if (!currentUser.IsAdmin)
                {
                    throw new ForbiddenException("Bu sohbet odasına erişemezsiniz.");
                }

                break;
        }

        return room;
    }

    /// <summary>Mesaj silme gibi moderasyon yetkisi.</summary>
    private async Task<bool> CanModerateAsync(ChatRoom room, CancellationToken cancellationToken)
    {
        if (currentUser.IsAdmin)
        {
            return true;
        }

        if (room.TeamId.HasValue)
        {
            return await permissions.CanManageTeamAsync(room.TeamId.Value, cancellationToken);
        }

        if (room.ProjectId.HasValue)
        {
            return await permissions.CanManageProjectAsync(room.ProjectId.Value, cancellationToken);
        }

        return false;
    }

    private async Task EnsureReplyTargetIsValidAsync(
        Guid roomId,
        Guid? replyToMessageId,
        CancellationToken cancellationToken)
    {
        if (!replyToMessageId.HasValue)
        {
            return;
        }

        // Yanıtlanan mesaj aynı odada olmalı; başka odadan alıntı yapılamaz.
        var exists = await context.Messages.AnyAsync(
            m => m.Id == replyToMessageId.Value && m.ChatRoomId == roomId,
            cancellationToken);

        if (!exists)
        {
            throw new NotFoundException("Yanıtlanan mesaj", replyToMessageId.Value);
        }
    }

    /// <summary>Odanın diğer üyelerine "yeni mesaj" bildirimi kuyruğa alır.</summary>
    private async Task QueueMessageNotificationsAsync(
        ChatRoom room,
        Message message,
        CancellationToken cancellationToken)
    {
        var recipientIds = room.Type switch
        {
            ChatRoomType.Team when room.TeamId.HasValue => await context.TeamMembers
                .Where(m => m.TeamId == room.TeamId.Value)
                .Select(m => m.UserId)
                .ToListAsync(cancellationToken),

            ChatRoomType.Project when room.ProjectId.HasValue => await context.ProjectMembers
                .Where(m => m.ProjectId == room.ProjectId.Value)
                .Select(m => m.UserId)
                .ToListAsync(cancellationToken),

            ChatRoomType.Leaders => await context.TeamMembers
                .Where(m => m.Role == TeamRole.Leader)
                .Select(m => m.UserId)
                .Distinct()
                .ToListAsync(cancellationToken),

            _ => []
        };

        var preview = message.Content.Length > 120
            ? message.Content[..120] + "…"
            : message.Content;

        notifications.QueueMany(recipientIds.Select(recipientId => new NotificationRequest(
            recipientId,
            NotificationType.MessageReceived,
            room.Name,
            preview,
            $"/sohbet/{room.Id}",
            nameof(ChatRoom),
            room.Id)));
    }

    private async Task<MessageDto> GetMessageAsync(
        Guid messageId,
        Guid userId,
        CancellationToken cancellationToken)
        => await context.Messages
            .AsNoTracking()
            .Where(m => m.Id == messageId)
            .Select(MessageProjection(userId))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Mesaj", messageId);

    /// <summary>
    /// Mesaj projeksiyonu. Okundu bilgisi kullanıcıya göre değiştiği için
    /// <paramref name="userId"/> parametre olarak alınır.
    /// </summary>
    private static Expression<Func<Message, MessageDto>> MessageProjection(Guid userId)
        => message => new MessageDto(
            message.Id,
            message.ChatRoomId,
            message.Content,
            new UserSummaryDto(
                message.Sender.Id,
                message.Sender.FullName,
                message.Sender.Email,
                message.Sender.JobTitle,
                message.Sender.AvatarUrl,
                (SystemRole)message.Sender.RoleId,
                message.Sender.IsOnline,
                message.Sender.LastSeenAt),
            message.CreatedAt,
            message.IsEdited,
            message.EditedAt,
            message.ReplyToMessageId,
            message.ReplyToMessage == null
                ? null
                : message.ReplyToMessage.Content.Length > 80
                    ? message.ReplyToMessage.Content.Substring(0, 80) + "…"
                    : message.ReplyToMessage.Content,
            message.ReplyToMessage == null ? null : message.ReplyToMessage.Sender.FullName,
            message.Attachments
                .Select(a => new AttachmentDto(
                    a.Id,
                    a.FileName,
                    a.ContentType,
                    a.SizeBytes,
                    a.Category,
                    a.Url,
                    null,
                    a.CreatedAt))
                .ToList(),
            message.Reads.Count,
            message.Reads.Any(r => r.UserId == userId));
}
