using GameFlow.Application.Features.Shared.Dtos;
using GameFlow.Application.Features.WorkItems.Dtos;

namespace GameFlow.Application.Features.WorkItems;

public interface IWorkItemChecklistService
{
    Task<IReadOnlyList<ChecklistItemDto>> AddAsync(
        Guid workItemId,
        CreateChecklistItemRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChecklistItemDto>> UpdateAsync(
        Guid workItemId,
        Guid itemId,
        UpdateChecklistItemRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChecklistItemDto>> DeleteAsync(
        Guid workItemId,
        Guid itemId,
        CancellationToken cancellationToken = default);
}

public interface IWorkItemCommentService
{
    Task<CommentDto> AddAsync(
        Guid workItemId,
        CreateCommentRequest request,
        CancellationToken cancellationToken = default);

    Task<CommentDto> UpdateAsync(
        Guid workItemId,
        Guid commentId,
        UpdateCommentRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid workItemId, Guid commentId, CancellationToken cancellationToken = default);
}

public interface IWorkItemAttachmentService
{
    Task<AttachmentDto> UploadAsync(
        Guid workItemId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid workItemId, Guid attachmentId, CancellationToken cancellationToken = default);
}

public interface ILabelService
{
    Task<IReadOnlyList<LabelDto>> GetListAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<LabelDto> CreateAsync(
        Guid projectId,
        CreateLabelRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid projectId, Guid labelId, CancellationToken cancellationToken = default);
}
