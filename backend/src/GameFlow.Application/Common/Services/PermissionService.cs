using GameFlow.Application.Common.Interfaces;
using GameFlow.Domain.Enums;
using GameFlow.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Application.Common.Services;

/// <inheritdoc cref="IPermissionService"/>
public class PermissionService(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : IPermissionService
{
    public async Task<bool> CanManageTeamAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        if (currentUser.IsAdmin)
        {
            return true;
        }

        var userId = currentUser.RequireUserId();

        return await context.TeamMembers.AnyAsync(
            m => m.TeamId == teamId && m.UserId == userId && m.Role == TeamRole.Leader,
            cancellationToken);
    }

    public async Task<bool> IsTeamMemberAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        if (currentUser.IsAdmin)
        {
            return true;
        }

        var userId = currentUser.RequireUserId();

        return await context.TeamMembers.AnyAsync(
            m => m.TeamId == teamId && m.UserId == userId,
            cancellationToken);
    }

    public async Task<bool> IsProjectMemberAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        if (currentUser.IsAdmin)
        {
            return true;
        }

        var userId = currentUser.RequireUserId();

        return await context.ProjectMembers.AnyAsync(
            m => m.ProjectId == projectId && m.UserId == userId,
            cancellationToken);
    }

    public async Task<bool> CanManageProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        if (currentUser.IsAdmin)
        {
            return true;
        }

        var userId = currentUser.RequireUserId();

        return await context.ProjectMembers.AnyAsync(
            m => m.ProjectId == projectId && m.UserId == userId && m.IsManager,
            cancellationToken);
    }

    public async Task<bool> IsAnyTeamLeaderAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.IsAdmin)
        {
            return true;
        }

        var userId = currentUser.RequireUserId();

        return await context.TeamMembers.AnyAsync(
            m => m.UserId == userId && m.Role == TeamRole.Leader,
            cancellationToken);
    }

    public async Task EnsureCanManageTeamAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        if (!await CanManageTeamAsync(teamId, cancellationToken))
        {
            throw new ForbiddenException("Bu işlem için takım lideri veya yönetici olmanız gerekiyor.");
        }
    }

    public async Task EnsureTeamMemberAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        if (!await IsTeamMemberAsync(teamId, cancellationToken))
        {
            throw new ForbiddenException("Bu takımın üyesi olmadığınız için erişemezsiniz.");
        }
    }

    public async Task EnsureProjectMemberAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        if (!await IsProjectMemberAsync(projectId, cancellationToken))
        {
            throw new ForbiddenException("Bu projenin üyesi olmadığınız için erişemezsiniz.");
        }
    }

    public async Task EnsureCanManageProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        if (!await CanManageProjectAsync(projectId, cancellationToken))
        {
            throw new ForbiddenException("Bu proje üzerinde yönetim yetkiniz bulunmuyor.");
        }
    }

    public async Task<IReadOnlyList<Guid>> GetManageableTeamIdsAsync(
        CancellationToken cancellationToken = default)
    {
        if (currentUser.IsAdmin)
        {
            return await context.Teams.Select(t => t.Id).ToListAsync(cancellationToken);
        }

        var userId = currentUser.RequireUserId();

        return await context.TeamMembers
            .Where(m => m.UserId == userId && m.Role == TeamRole.Leader)
            .Select(m => m.TeamId)
            .ToListAsync(cancellationToken);
    }
}
