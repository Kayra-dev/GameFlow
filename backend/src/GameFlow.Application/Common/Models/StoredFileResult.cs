using GameFlow.Domain.Enums;

namespace GameFlow.Application.Common.Models;

/// <summary>Depoya yazılan dosyanın metadata bilgisi.</summary>
public record StoredFileResult(
    string FileName,
    string StoredFileName,
    string ContentType,
    long SizeBytes,
    AttachmentCategory Category,
    string Url);
