using GameFlow.Application.Features.Teams.Dtos;

namespace GameFlow.Application.Features.Teams;

public interface ITeamService
{
    Task<IReadOnlyList<TeamSummaryDto>> GetListAsync(
        TeamListRequest request,
        CancellationToken cancellationToken = default);

    Task<TeamDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TeamDetailDto> CreateAsync(CreateTeamRequest request, CancellationToken cancellationToken = default);

    Task<TeamDetailDto> UpdateAsync(
        Guid id,
        UpdateTeamRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Takım lideri atar veya kaldırır. Yalnızca yöneticiler çağırabilir.</summary>
    Task<TeamDetailDto> AssignLeaderAsync(
        Guid id,
        AssignLeaderRequest request,
        CancellationToken cancellationToken = default);

    Task<TeamDetailDto> AddMembersAsync(
        Guid id,
        AddTeamMembersRequest request,
        CancellationToken cancellationToken = default);

    Task RemoveMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
