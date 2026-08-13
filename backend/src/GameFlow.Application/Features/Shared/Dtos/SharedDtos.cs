using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.Shared.Dtos;

/// <summary>Görev etiketi.</summary>
public record LabelDto(Guid Id, string Name, string ColorHex);

/// <summary>Yüklenen dosya. Görev ekleri ve sohbet dosyaları aynı gösterimi paylaşır.</summary>
public record AttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    AttachmentCategory Category,
    string Url,
    UserSummaryDto? UploadedBy,
    DateTime CreatedAt);

/// <summary>Aktivite/denetim kaydı gösterimi.</summary>
public record ActivityDto(
    Guid Id,
    ActivityType Type,
    string Description,
    UserSummaryDto? Actor,
    DateTime CreatedAt);
