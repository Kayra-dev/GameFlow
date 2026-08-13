using GameFlow.Application.Features.Sprints.Dtos;

namespace GameFlow.Application.Features.Sprints;

public interface ISprintService
{
    Task<IReadOnlyList<SprintSummaryDto>> GetListAsync(
        SprintListRequest request,
        CancellationToken cancellationToken = default);

    Task<SprintDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SprintDetailDto> CreateAsync(
        CreateSprintRequest request,
        CancellationToken cancellationToken = default);

    Task<SprintDetailDto> UpdateAsync(
        Guid id,
        UpdateSprintRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Sprinti başlatır. Bir projede aynı anda tek aktif sprint olabilir.</summary>
    Task<SprintDetailDto> StartAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprinti tamamlar; bitmemiş görevleri hedef sprinte taşır veya backlog'a döndürür.
    /// </summary>
    Task<SprintReportDto> CompleteAsync(
        Guid id,
        CompleteSprintRequest request,
        CancellationToken cancellationToken = default);

    Task<SprintReportDto> GetReportAsync(Guid id, CancellationToken cancellationToken = default);
}
