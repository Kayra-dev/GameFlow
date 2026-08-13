using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.Projects.Dtos;
using GameFlow.Application.Features.Shared.Dtos;
using GameFlow.Application.Features.Teams.Dtos;
using GameFlow.Application.Features.Users;
using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Application.Features.WorkItems;
using GameFlow.Application.Features.WorkItems.Dtos;
using GameFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Application.Features.Search;

/// <summary>Global arama sonuçları. Her tür için en fazla birkaç kayıt döner.</summary>
public record SearchResultsDto(
    IReadOnlyList<UserSummaryDto> Users,
    IReadOnlyList<WorkItemSummaryDto> Tasks,
    IReadOnlyList<TeamSummaryDto> Teams,
    IReadOnlyList<ProjectSummaryDto> Projects,
    IReadOnlyList<AttachmentDto> Attachments,
    int TotalCount);

public class SearchRequest
{
    private const int MaxLimit = 20;

    private int _limitPerType = 5;

    public string Query { get; set; } = string.Empty;

    /// <summary>Her sonuç türü için getirilecek en fazla kayıt.</summary>
    public int LimitPerType
    {
        get => _limitPerType;
        set => _limitPerType = value switch
        {
            < 1 => 5,
            > MaxLimit => MaxLimit,
            _ => value
        };
    }
}

public interface ISearchService
{
    Task<SearchResultsDto> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Global arama (komut paleti). Kullanıcı yalnızca erişebildiği projelerin
/// görevlerini ve dosyalarını görür; kullanıcı ve takım listeleri stüdyo genelidir.
/// </summary>
public class SearchService(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTime) : ISearchService
{
    /// <summary>Bu uzunluğun altındaki sorgular çok geniş sonuç döndüreceği için çalıştırılmaz.</summary>
    private const int MinimumQueryLength = 2;

    public async Task<SearchResultsDto> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var term = request.Query.Trim().ToLowerInvariant();

        if (term.Length < MinimumQueryLength)
        {
            return new SearchResultsDto([], [], [], [], [], 0);
        }

        var userId = currentUser.RequireUserId();
        var limit = request.LimitPerType;
        var now = dateTime.UtcNow;

        // Görev ve dosya aramaları kullanıcının üyesi olduğu projelerle sınırlıdır.
        var accessibleProjectIds = currentUser.IsAdmin
            ? null
            : await context.ProjectMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.ProjectId)
                .ToListAsync(cancellationToken);

        var users = await context.Users
            .AsNoTracking()
            .Where(u => u.IsActive
                        && (u.FullName.ToLower().Contains(term) || u.Email.Contains(term)))
            .OrderBy(u => u.FullName)
            .Take(limit)
            .Select(UserProjections.ToSummary)
            .ToListAsync(cancellationToken);

        var taskQuery = context.WorkItems.AsNoTracking();

        if (accessibleProjectIds is not null)
        {
            taskQuery = taskQuery.Where(w => accessibleProjectIds.Contains(w.ProjectId));
        }

        var tasks = await taskQuery
            .Where(w => w.Title.ToLower().Contains(term) || w.Key.ToLower().Contains(term))
            // Anahtarla tam eşleşen kayıt (örn. "ODY-42") en üstte olmalı.
            .OrderByDescending(w => w.Key.ToLower() == term)
            .ThenByDescending(w => w.CreatedAt)
            .Take(limit)
            .Select(WorkItemProjections.ToSummary(now))
            .ToListAsync(cancellationToken);

        var teams = await context.Teams
            .AsNoTracking()
            .Where(t => t.Name.ToLower().Contains(term))
            .OrderBy(t => t.Name)
            .Take(limit)
            .Select(t => new TeamSummaryDto(
                t.Id,
                t.Name,
                t.Category,
                t.ColorHex,
                t.IconKey,
                t.Members.Count,
                t.Leader == null
                    ? null
                    : new UserSummaryDto(
                        t.Leader.Id,
                        t.Leader.FullName,
                        t.Leader.Email,
                        t.Leader.JobTitle,
                        t.Leader.AvatarUrl,
                        (SystemRole)t.Leader.RoleId,
                        t.Leader.IsOnline,
                        t.Leader.LastSeenAt)))
            .ToListAsync(cancellationToken);

        var projectQuery = context.Projects.AsNoTracking();

        if (accessibleProjectIds is not null)
        {
            projectQuery = projectQuery.Where(p => accessibleProjectIds.Contains(p.Id));
        }

        var projects = await projectQuery
            .Where(p => p.Name.ToLower().Contains(term) || p.Key.ToLower().Contains(term))
            .OrderBy(p => p.Name)
            .Take(limit)
            .Select(p => new ProjectSummaryDto(
                p.Id,
                p.Name,
                p.Key,
                p.Status,
                p.ColorHex,
                p.CoverImageUrl,
                p.Members.Count,
                p.WorkItems.Count,
                p.WorkItems.Count(w => w.Status == WorkItemStatus.Done)))
            .ToListAsync(cancellationToken);

        var attachmentQuery = context.TaskAttachments.AsNoTracking();

        if (accessibleProjectIds is not null)
        {
            attachmentQuery = attachmentQuery
                .Where(a => accessibleProjectIds.Contains(a.WorkItem.ProjectId));
        }

        var attachments = await attachmentQuery
            .Where(a => a.FileName.ToLower().Contains(term))
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new AttachmentDto(
                a.Id,
                a.FileName,
                a.ContentType,
                a.SizeBytes,
                a.Category,
                a.Url,
                a.UploadedBy == null
                    ? null
                    : new UserSummaryDto(
                        a.UploadedBy.Id,
                        a.UploadedBy.FullName,
                        a.UploadedBy.Email,
                        a.UploadedBy.JobTitle,
                        a.UploadedBy.AvatarUrl,
                        (SystemRole)a.UploadedBy.RoleId,
                        a.UploadedBy.IsOnline,
                        a.UploadedBy.LastSeenAt),
                a.CreatedAt))
            .ToListAsync(cancellationToken);

        return new SearchResultsDto(
            users,
            tasks,
            teams,
            projects,
            attachments,
            users.Count + tasks.Count + teams.Count + projects.Count + attachments.Count);
    }
}
