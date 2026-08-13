using GameFlow.Application.Common.Interfaces;
using GameFlow.Application.Common.Models;
using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Domain.Entities;
using GameFlow.Domain.Enums;
using GameFlow.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GameFlow.Application.Features.Users;

/// <summary>
/// Kullanıcı yönetimi. Kayıt ekranı bulunmadığı için kullanıcı oluşturma,
/// güncelleme ve silme işlemleri yalnızca yöneticilere açıktır (yetki kontrolü
/// API katmanındaki politikalarla sağlanır).
/// </summary>
public class UserService(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ICurrentUserService currentUser,
    IDateTimeProvider dateTime,
    IActivityLogger activityLogger) : IUserService
{
    public async Task<PagedResult<UserSummaryDto>> GetListAsync(
        UserListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // Application katmanı sağlayıcıya bağımlı olmadığı için Npgsql'e özel
            // ILike yerine sağlayıcıdan bağımsız LOWER(...) LIKE çevirisi kullanılır.
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(term) || u.Email.Contains(term));
        }

        if (request.Role.HasValue)
        {
            query = query.Where(u => u.RoleId == (int)request.Role.Value);
        }

        if (request.TeamId.HasValue)
        {
            query = query.Where(u => u.TeamMemberships.Any(m => m.TeamId == request.TeamId.Value));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == request.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.FullName)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(UserProjections.ToSummary)
            .ToListAsync(cancellationToken);

        return PagedResult<UserSummaryDto>.Create(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<UserDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDetailDto(
                u.Id,
                u.FullName,
                u.Email,
                u.JobTitle,
                u.AvatarUrl,
                (SystemRole)u.RoleId,
                u.IsOnline,
                u.LastSeenAt,
                u.Bio,
                u.IsActive,
                u.CreatedAt,
                u.TeamMemberships
                    .Select(m => new UserTeamDto(
                        m.Team.Id,
                        m.Team.Name,
                        m.Team.Category,
                        m.Team.ColorHex,
                        m.Role))
                    .ToList(),
                u.ProjectMemberships
                    .Select(m => new UserProjectDto(
                        m.Project.Id,
                        m.Project.Name,
                        m.Project.Key,
                        m.Project.ColorHex))
                    .ToList(),
                u.AssignedWorkItems.Count(t => t.Status == WorkItemStatus.Done),
                u.AssignedWorkItems.Count(t =>
                    t.Status != WorkItemStatus.Done && t.Status != WorkItemStatus.Cancelled)))
            .FirstOrDefaultAsync(cancellationToken);

        return user ?? throw new NotFoundException("Kullanıcı", id);
    }

    public async Task<IReadOnlyList<UserSummaryDto>> GetAssignableAsync(
        CancellationToken cancellationToken = default)
        => await context.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(UserProjections.ToSummary)
            .ToListAsync(cancellationToken);

    public async Task<UserDetailDto> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // Mantıksal olarak silinmiş kullanıcılar da e-posta tekilliğini korur.
        var emailTaken = await context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (emailTaken)
        {
            throw new ConflictException("Bu e-posta adresi zaten kullanılıyor.");
        }

        await EnsureTeamsExistAsync(request.TeamIds, cancellationToken);

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            RoleId = (int)request.Role,
            JobTitle = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim(),
            Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim(),
            MustChangePassword = request.MustChangePassword,
            IsActive = true
        };

        context.Users.Add(user);

        foreach (var teamId in request.TeamIds.Distinct())
        {
            context.TeamMembers.Add(new TeamMember
            {
                TeamId = teamId,
                UserId = user.Id,
                // Sistem rolü lider olsa bile takım içi rol ataması ayrı bir işlemdir.
                Role = TeamRole.Member,
                JoinedAt = dateTime.UtcNow
            });
        }

        activityLogger.Log(
            ActivityType.UserCreated,
            $"{user.FullName} adlı kullanıcı oluşturuldu.",
            entityType: nameof(User),
            entityId: user.Id);

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task<UserDetailDto> UpdateAsync(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
                   ?? throw new NotFoundException("Kullanıcı", id);

        var previousRole = (SystemRole)user.RoleId;

        if (previousRole == SystemRole.Admin && request.Role != SystemRole.Admin)
        {
            await EnsureNotLastAdminAsync(user.Id, cancellationToken);
        }

        // Yönetici kendi hesabını devre dışı bırakıp sisteme erişimini kaybedemez.
        if (!request.IsActive && user.Id == currentUser.UserId)
        {
            throw new DomainException("Kendi hesabınızı devre dışı bırakamazsınız.");
        }

        user.FullName = request.FullName.Trim();
        user.RoleId = (int)request.Role;
        user.JobTitle = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim();
        user.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();
        user.IsActive = request.IsActive;

        // Liderlik yetkisi kaldırıldıysa yönettiği takımlardaki liderliği de düşer.
        if (previousRole == SystemRole.TeamLeader && request.Role == SystemRole.TeamMember)
        {
            await RevokeTeamLeadershipAsync(user.Id, cancellationToken);
        }

        activityLogger.Log(
            ActivityType.UserUpdated,
            $"{user.FullName} adlı kullanıcı güncellendi.",
            entityType: nameof(User),
            entityId: user.Id);

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == currentUser.UserId)
        {
            throw new DomainException("Kendi hesabınızı silemezsiniz.");
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
                   ?? throw new NotFoundException("Kullanıcı", id);

        if ((SystemRole)user.RoleId == SystemRole.Admin)
        {
            await EnsureNotLastAdminAsync(user.Id, cancellationToken);
        }

        await RevokeTeamLeadershipAsync(user.Id, cancellationToken);

        // Kullanıcı mantıksal olarak silinir; yorumları, görev geçmişi ve
        // denetim kayıtları korunur. Üyelikler ise fiilen kaldırılır.
        var memberships = await context.TeamMembers
            .Where(m => m.UserId == user.Id)
            .ToListAsync(cancellationToken);

        foreach (var membership in memberships)
        {
            context.TeamMembers.Remove(membership);
        }

        var projectMemberships = await context.ProjectMembers
            .Where(m => m.UserId == user.Id)
            .ToListAsync(cancellationToken);

        foreach (var membership in projectMemberships)
        {
            context.ProjectMembers.Remove(membership);
        }

        user.IsActive = false;
        // SaveChanges sırasında ISoftDeletable olduğu için mantıksal silmeye çevrilir.
        context.Users.Remove(user);

        activityLogger.Log(
            ActivityType.UserDeleted,
            $"{user.FullName} adlı kullanıcı silindi.",
            entityType: nameof(User),
            entityId: user.Id);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(
        Guid id,
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
                   ?? throw new NotFoundException("Kullanıcı", id);

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = request.MustChangePassword;

        // Şifre sıfırlandığında kullanıcının açık oturumları düşürülür.
        var tokens = await context.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = dateTime.UtcNow;
        }

        activityLogger.Log(
            ActivityType.UserUpdated,
            $"{user.FullName} adlı kullanıcının şifresi sıfırlandı.",
            entityType: nameof(User),
            entityId: user.Id);

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Sistemde en az bir yönetici kalmasını garanti eder.</summary>
    private async Task EnsureNotLastAdminAsync(Guid excludedUserId, CancellationToken cancellationToken)
    {
        var otherAdminExists = await context.Users
            .AnyAsync(
                u => u.Id != excludedUserId && u.RoleId == (int)SystemRole.Admin && u.IsActive,
                cancellationToken);

        if (!otherAdminExists)
        {
            throw new DomainException(
                "Sistemdeki son yöneticiyi kaldıramazsınız. Önce başka bir yönetici tanımlayın.");
        }
    }

    /// <summary>Kullanıcının liderlik ettiği takımlarda liderliği kaldırır.</summary>
    private async Task RevokeTeamLeadershipAsync(Guid userId, CancellationToken cancellationToken)
    {
        var ledTeams = await context.Teams
            .Where(t => t.LeaderId == userId)
            .ToListAsync(cancellationToken);

        foreach (var team in ledTeams)
        {
            team.LeaderId = null;
        }

        var leaderMemberships = await context.TeamMembers
            .Where(m => m.UserId == userId && m.Role == TeamRole.Leader)
            .ToListAsync(cancellationToken);

        foreach (var membership in leaderMemberships)
        {
            membership.Role = TeamRole.Member;
        }
    }

    private async Task EnsureTeamsExistAsync(
        IReadOnlyCollection<Guid> teamIds,
        CancellationToken cancellationToken)
    {
        if (teamIds.Count == 0)
        {
            return;
        }

        var existingCount = await context.Teams
            .CountAsync(t => teamIds.Contains(t.Id), cancellationToken);

        if (existingCount != teamIds.Distinct().Count())
        {
            throw new NotFoundException("Takım", string.Join(", ", teamIds));
        }
    }
}
