using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Features.Teams.Dtos;
using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Domain.Entities;
using GameFlow.Domain.Enums;
using GameFlow.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Application.Features.Teams;

/// <summary>
/// Takım yönetimi. Takım oluşturma/silme ve lider atama yalnızca yöneticilere,
/// üye ekleme/çıkarma ise yönetici ve ilgili takımın liderine açıktır.
/// </summary>
public class TeamService(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IPermissionService permissions,
    IDateTimeProvider dateTime,
    IActivityLogger activityLogger) : ITeamService
{
    public async Task<IReadOnlyList<TeamSummaryDto>> GetListAsync(
        TeamListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = context.Teams.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(t => t.Name.ToLower().Contains(term));
        }

        if (request.Category.HasValue)
        {
            query = query.Where(t => t.Category == request.Category.Value);
        }

        if (request.OnlyMine)
        {
            var userId = currentUser.RequireUserId();
            query = query.Where(t => t.Members.Any(m => m.UserId == userId));
        }

        return await query
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
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
    }

    public async Task<TeamDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = dateTime.UtcNow;

        var team = await context.Teams
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TeamDetailDto(
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
                        t.Leader.LastSeenAt),
                t.Description,
                t.CreatedAt,
                t.Members
                    .OrderBy(m => m.Role)
                    .ThenBy(m => m.User.FullName)
                    .Select(m => new TeamMemberDto(
                        m.Id,
                        new UserSummaryDto(
                            m.User.Id,
                            m.User.FullName,
                            m.User.Email,
                            m.User.JobTitle,
                            m.User.AvatarUrl,
                            (SystemRole)m.User.RoleId,
                            m.User.IsOnline,
                            m.User.LastSeenAt),
                        m.Role,
                        m.JoinedAt))
                    .ToList(),
                t.ChatRooms.Where(r => r.Type == ChatRoomType.Team).Select(r => (Guid?)r.Id).FirstOrDefault(),
                // İlerleme yüzdesi iptal edilen görevler hariç tutularak hesaplanır.
                t.WorkItems.Count(w => w.Status != WorkItemStatus.Cancelled) == 0
                    ? 0
                    : t.WorkItems.Count(w => w.Status == WorkItemStatus.Done) * 100
                      / t.WorkItems.Count(w => w.Status != WorkItemStatus.Cancelled),
                t.WorkItems.Count,
                t.WorkItems.Count(w => w.Status == WorkItemStatus.Done),
                t.WorkItems.Count(w =>
                    w.Status != WorkItemStatus.Done && w.Status != WorkItemStatus.Cancelled),
                t.WorkItems.Count(w =>
                    w.DueDate != null
                    && w.DueDate < now
                    && w.Status != WorkItemStatus.Done
                    && w.Status != WorkItemStatus.Cancelled)))
            .FirstOrDefaultAsync(cancellationToken);

        return team ?? throw new NotFoundException("Takım", id);
    }

    public async Task<TeamDetailDto> CreateAsync(
        CreateTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        if (await context.Teams.AnyAsync(t => t.Name == name, cancellationToken))
        {
            throw new ConflictException("Bu adda bir takım zaten var.");
        }

        // Lider de takımın üyesi olmak zorundadır.
        var memberIds = request.MemberIds.ToHashSet();

        if (request.LeaderId.HasValue)
        {
            memberIds.Add(request.LeaderId.Value);
        }

        await EnsureUsersExistAsync(memberIds, cancellationToken);

        var team = new Team
        {
            Name = name,
            Description = Normalize(request.Description),
            Category = request.Category,
            ColorHex = request.ColorHex,
            IconKey = Normalize(request.IconKey),
            LeaderId = request.LeaderId
        };

        context.Teams.Add(team);

        foreach (var userId in memberIds)
        {
            context.TeamMembers.Add(new TeamMember
            {
                TeamId = team.Id,
                UserId = userId,
                Role = userId == request.LeaderId ? TeamRole.Leader : TeamRole.Member,
                JoinedAt = dateTime.UtcNow
            });
        }

        // Her takımın kendi sohbet odası otomatik oluşur.
        context.ChatRooms.Add(new ChatRoom
        {
            Type = ChatRoomType.Team,
            Name = team.Name,
            Description = $"{team.Name} takımının sohbet odası.",
            TeamId = team.Id,
            IsSystem = true
        });

        if (request.LeaderId.HasValue)
        {
            await PromoteToLeaderRoleAsync(request.LeaderId.Value, cancellationToken);
        }

        activityLogger.Log(
            ActivityType.TeamCreated,
            $"{team.Name} takımı oluşturuldu.",
            teamId: team.Id,
            entityType: nameof(Team),
            entityId: team.Id);

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(team.Id, cancellationToken);
    }

    public async Task<TeamDetailDto> UpdateAsync(
        Guid id,
        UpdateTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        await permissions.EnsureCanManageTeamAsync(id, cancellationToken);

        var team = await context.Teams.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
                   ?? throw new NotFoundException("Takım", id);

        var name = request.Name.Trim();

        if (await context.Teams.AnyAsync(t => t.Name == name && t.Id != id, cancellationToken))
        {
            throw new ConflictException("Bu adda bir takım zaten var.");
        }

        var renamed = team.Name != name;

        team.Name = name;
        team.Description = Normalize(request.Description);
        team.Category = request.Category;
        team.ColorHex = request.ColorHex;
        team.IconKey = Normalize(request.IconKey);

        // Sohbet odası adı takım adını takip eder.
        if (renamed)
        {
            var chatRoom = await context.ChatRooms
                .FirstOrDefaultAsync(r => r.TeamId == id && r.Type == ChatRoomType.Team, cancellationToken);

            if (chatRoom is not null)
            {
                chatRoom.Name = name;
            }
        }

        activityLogger.Log(
            ActivityType.TeamUpdated,
            $"{team.Name} takımı güncellendi.",
            teamId: team.Id,
            entityType: nameof(Team),
            entityId: team.Id);

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(team.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var team = await context.Teams.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
                   ?? throw new NotFoundException("Takım", id);

        // Üyelikler fiilen kaldırılır; sohbet geçmişi ve görevler korunur.
        // Görevlerin TeamId alanı SetNull kuralıyla değil, mantıksal silme olduğu için
        // el ile boşaltılır ki artık var olmayan bir takıma bağlı kalmasınlar.
        var memberships = await context.TeamMembers
            .Where(m => m.TeamId == id)
            .ToListAsync(cancellationToken);

        foreach (var membership in memberships)
        {
            context.TeamMembers.Remove(membership);
        }

        var workItems = await context.WorkItems
            .Where(w => w.TeamId == id)
            .ToListAsync(cancellationToken);

        foreach (var workItem in workItems)
        {
            workItem.TeamId = null;
        }

        var sprints = await context.Sprints
            .Where(s => s.TeamId == id)
            .ToListAsync(cancellationToken);

        foreach (var sprint in sprints)
        {
            sprint.TeamId = null;
        }

        context.Teams.Remove(team);

        activityLogger.Log(
            ActivityType.TeamDeleted,
            $"{team.Name} takımı silindi.",
            entityType: nameof(Team),
            entityId: team.Id);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TeamDetailDto> AssignLeaderAsync(
        Guid id,
        AssignLeaderRequest request,
        CancellationToken cancellationToken = default)
    {
        var team = await context.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("Takım", id);

        // Önceki liderin takım içi rolü üyeliğe düşürülür.
        foreach (var member in team.Members.Where(m => m.Role == TeamRole.Leader))
        {
            member.Role = TeamRole.Member;
        }

        if (request.UserId is null)
        {
            team.LeaderId = null;
        }
        else
        {
            var userId = request.UserId.Value;

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                       ?? throw new NotFoundException("Kullanıcı", userId);

            if (!user.IsActive)
            {
                throw new DomainException("Devre dışı bir kullanıcı takım lideri yapılamaz.");
            }

            var membership = team.Members.FirstOrDefault(m => m.UserId == userId);

            if (membership is null)
            {
                // Lider yapılan kullanıcı otomatik olarak takıma da eklenir.
                context.TeamMembers.Add(new TeamMember
                {
                    TeamId = team.Id,
                    UserId = userId,
                    Role = TeamRole.Leader,
                    JoinedAt = dateTime.UtcNow
                });
            }
            else
            {
                membership.Role = TeamRole.Leader;
            }

            team.LeaderId = userId;

            await PromoteToLeaderRoleAsync(userId, cancellationToken);

            activityLogger.Log(
                ActivityType.TeamUpdated,
                $"{user.FullName}, {team.Name} takımına lider olarak atandı.",
                teamId: team.Id,
                entityType: nameof(Team),
                entityId: team.Id);
        }

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(team.Id, cancellationToken);
    }

    public async Task<TeamDetailDto> AddMembersAsync(
        Guid id,
        AddTeamMembersRequest request,
        CancellationToken cancellationToken = default)
    {
        await permissions.EnsureCanManageTeamAsync(id, cancellationToken);

        var team = await context.Teams.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
                   ?? throw new NotFoundException("Takım", id);

        var requestedIds = request.UserIds.Distinct().ToList();

        await EnsureUsersExistAsync(requestedIds, cancellationToken);

        var existingIds = await context.TeamMembers
            .Where(m => m.TeamId == id)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);

        var newIds = requestedIds.Except(existingIds).ToList();

        if (newIds.Count == 0)
        {
            throw new DomainException("Seçilen kullanıcılar bu takımda zaten var.");
        }

        foreach (var userId in newIds)
        {
            context.TeamMembers.Add(new TeamMember
            {
                TeamId = id,
                UserId = userId,
                Role = TeamRole.Member,
                JoinedAt = dateTime.UtcNow
            });
        }

        activityLogger.Log(
            ActivityType.TeamMemberAdded,
            $"{team.Name} takımına {newIds.Count} yeni üye eklendi.",
            teamId: team.Id,
            entityType: nameof(Team),
            entityId: team.Id);

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task RemoveMemberAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await permissions.EnsureCanManageTeamAsync(id, cancellationToken);

        var membership = await context.TeamMembers
            .Include(m => m.User)
            .Include(m => m.Team)
            .FirstOrDefaultAsync(m => m.TeamId == id && m.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Takım üyeliği", userId);

        // Lider çıkarılırsa takım lidersiz kalır; yeni lider ataması yönetici işidir.
        if (membership.Role == TeamRole.Leader)
        {
            membership.Team.LeaderId = null;
        }

        // Kullanıcının bu takımdaki açık görevleri atamasız kalır.
        var assignedWorkItems = await context.WorkItems
            .Where(w => w.TeamId == id
                        && w.AssigneeId == userId
                        && w.Status != WorkItemStatus.Done
                        && w.Status != WorkItemStatus.Cancelled)
            .ToListAsync(cancellationToken);

        foreach (var workItem in assignedWorkItems)
        {
            workItem.AssigneeId = null;
        }

        context.TeamMembers.Remove(membership);

        activityLogger.Log(
            ActivityType.TeamMemberRemoved,
            $"{membership.User.FullName}, {membership.Team.Name} takımından çıkarıldı.",
            teamId: id,
            entityType: nameof(Team),
            entityId: id);

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Takım lideri yapılan kullanıcının sistem rolü "Takım Üyesi" ise
    /// "Takım Lideri"ne yükseltilir. Yönetici rolü asla düşürülmez.
    /// </summary>
    private async Task PromoteToLeaderRoleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is not null && user.RoleId == (int)SystemRole.TeamMember)
        {
            user.RoleId = (int)SystemRole.TeamLeader;
        }
    }

    private async Task EnsureUsersExistAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return;
        }

        var foundCount = await context.Users
            .CountAsync(u => userIds.Contains(u.Id) && u.IsActive, cancellationToken);

        if (foundCount != userIds.Distinct().Count())
        {
            throw new NotFoundException("Kullanıcı", string.Join(", ", userIds));
        }
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
