using GameFlow.Domain.Enums;

namespace GameFlow.Application.Common.Interfaces;

/// <summary>
/// Denetim ve "son aktiviteler" akışı için kayıt oluşturur.
/// Kayıtlar çağıran işlemin SaveChanges'i ile birlikte yazılır.
/// </summary>
public interface IActivityLogger
{
    void Log(
        ActivityType type,
        string description,
        Guid? projectId = null,
        Guid? teamId = null,
        Guid? workItemId = null,
        string? entityType = null,
        Guid? entityId = null,
        object? metadata = null);
}
