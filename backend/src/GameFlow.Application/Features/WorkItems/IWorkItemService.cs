using GameFlow.Application.Common.Models;
using GameFlow.Application.Features.WorkItems.Dtos;

namespace GameFlow.Application.Features.WorkItems;

public interface IWorkItemService
{
    Task<PagedResult<WorkItemSummaryDto>> GetListAsync(
        WorkItemListRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkItemDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Görev anahtarıyla (örn. ODY-42) erişim; derin bağlantılar için.</summary>
    Task<WorkItemDetailDto> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Projenin kanban panosunu kolonlara bölünmüş olarak döner.</summary>
    Task<KanbanBoardDto> GetBoardAsync(
        Guid projectId,
        Guid? teamId = null,
        Guid? sprintId = null,
        CancellationToken cancellationToken = default);

    Task<WorkItemDetailDto> CreateAsync(
        CreateWorkItemRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkItemDetailDto> UpdateAsync(
        Guid id,
        UpdateWorkItemRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Kanban'da sürükle-bırak: kolon ve sıra günceller.</summary>
    Task<WorkItemSummaryDto> MoveAsync(
        Guid id,
        MoveWorkItemRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Yalnızca durum değiştirir (kart menüsünden hızlı geçiş).</summary>
    Task<WorkItemSummaryDto> ChangeStatusAsync(
        Guid id,
        ChangeStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkItemSummaryDto> AssignAsync(
        Guid id,
        AssignWorkItemRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deadline özeti: bugün bitecek, yaklaşan ve gecikmiş görevler.
    /// Kapsam, kullanıcının erişebildiği projelerle sınırlıdır.
    /// </summary>
    Task<DeadlineOverviewDto> GetDeadlineOverviewAsync(
        Guid? projectId = null,
        Guid? teamId = null,
        int upcomingDays = 7,
        bool onlyMine = false,
        CancellationToken cancellationToken = default);
}
