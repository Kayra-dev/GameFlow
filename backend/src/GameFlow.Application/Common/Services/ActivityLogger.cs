using System.Text.Json;
using GameFlow.Application.Common.Interfaces;
using GameFlow.Domain.Entities;
using GameFlow.Domain.Enums;

namespace GameFlow.Application.Common.Services;

/// <inheritdoc cref="IActivityLogger"/>
public class ActivityLogger(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTime) : IActivityLogger
{
    public void Log(
        ActivityType type,
        string description,
        Guid? projectId = null,
        Guid? teamId = null,
        Guid? workItemId = null,
        string? entityType = null,
        Guid? entityId = null,
        object? metadata = null)
    {
        context.ActivityLogs.Add(new ActivityLog
        {
            ActorId = currentUser.UserId,
            Type = type,
            Description = description,
            ProjectId = projectId,
            TeamId = teamId,
            WorkItemId = workItemId,
            EntityType = entityType,
            EntityId = entityId,
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata),
            CreatedAt = dateTime.UtcNow
        });
    }
}
