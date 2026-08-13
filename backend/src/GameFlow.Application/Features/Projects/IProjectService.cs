using GameFlow.Application.Features.Projects.Dtos;

namespace GameFlow.Application.Features.Projects;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectSummaryDto>> GetListAsync(
        ProjectListRequest request,
        CancellationToken cancellationToken = default);

    Task<ProjectDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProjectDetailDto> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken = default);

    Task<ProjectDetailDto> UpdateAsync(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProjectDetailDto> AddMembersAsync(
        Guid id,
        AddProjectMembersRequest request,
        CancellationToken cancellationToken = default);

    Task RemoveMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Proje yöneticiliği yetkisini açar veya kapatır.</summary>
    Task SetMemberManagerAsync(
        Guid id,
        Guid userId,
        bool isManager,
        CancellationToken cancellationToken = default);
}
