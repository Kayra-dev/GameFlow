using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.Notifications.Dtos;

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    string? Link,
    bool IsRead,
    DateTime CreatedAt,
    UserSummaryDto? Actor);

/// <summary>Bildirim oluşturma isteği (yalnızca sunucu içi kullanım).</summary>
public record NotificationRequest(
    Guid RecipientId,
    NotificationType Type,
    string Title,
    string Message,
    string? Link = null,
    string? EntityType = null,
    Guid? EntityId = null);
