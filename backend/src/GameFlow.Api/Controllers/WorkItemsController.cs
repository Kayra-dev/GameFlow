using GameFlow.Api.Extensions;
using GameFlow.Application.Common.Models;
using GameFlow.Application.Features.Shared.Dtos;
using GameFlow.Application.Features.WorkItems;
using GameFlow.Application.Features.WorkItems.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameFlow.Api.Controllers;

/// <summary>
/// Görev yönetimi ve kanban panosu. Yetki denetimleri kaynak bazlı olduğu için
/// servis katmanında yapılır (bkz. IPermissionService).
/// Adres şeması: /api/work-items
/// </summary>
public class WorkItemsController(
    IWorkItemService workItemService,
    IWorkItemChecklistService checklistService,
    IWorkItemCommentService commentService,
    IWorkItemAttachmentService attachmentService) : ApiControllerBase
{
    /// <summary>Filtrelenebilir, sayfalanmış görev listesi.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<WorkItemSummaryDto>>> GetList(
        [FromQuery] WorkItemListRequest request,
        CancellationToken cancellationToken)
        => Ok(await workItemService.GetListAsync(request, cancellationToken));

    /// <summary>Projenin kanban panosu (kolonlara bölünmüş).</summary>
    [HttpGet("board")]
    public async Task<ActionResult<KanbanBoardDto>> GetBoard(
        [FromQuery] Guid projectId,
        [FromQuery] Guid? teamId,
        [FromQuery] Guid? sprintId,
        CancellationToken cancellationToken)
        => Ok(await workItemService.GetBoardAsync(projectId, teamId, sprintId, cancellationToken));

    /// <summary>Deadline özeti: bugün bitecek, yaklaşan ve gecikmiş görevler.</summary>
    [HttpGet("deadlines")]
    public async Task<ActionResult<DeadlineOverviewDto>> GetDeadlines(
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? teamId,
        [FromQuery] int upcomingDays = 7,
        [FromQuery] bool onlyMine = false,
        CancellationToken cancellationToken = default)
        => Ok(await workItemService.GetDeadlineOverviewAsync(
            projectId,
            teamId,
            upcomingDays,
            onlyMine,
            cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkItemDetailDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await workItemService.GetByIdAsync(id, cancellationToken));

    /// <summary>Görev anahtarıyla erişim (örn. ODY-42).</summary>
    [HttpGet("by-key/{key}")]
    public async Task<ActionResult<WorkItemDetailDto>> GetByKey(
        string key,
        CancellationToken cancellationToken)
        => Ok(await workItemService.GetByKeyAsync(key, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<WorkItemDetailDto>> Create(
        CreateWorkItemRequest request,
        CancellationToken cancellationToken)
    {
        var workItem = await workItemService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = workItem.Id }, workItem);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkItemDetailDto>> Update(
        Guid id,
        UpdateWorkItemRequest request,
        CancellationToken cancellationToken)
        => Ok(await workItemService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await workItemService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Kanban'da sürükle-bırak: hedef kolon ve komşu kartlara göre yeni sıra.</summary>
    [HttpPut("{id:guid}/move")]
    public async Task<ActionResult<WorkItemSummaryDto>> Move(
        Guid id,
        MoveWorkItemRequest request,
        CancellationToken cancellationToken)
        => Ok(await workItemService.MoveAsync(id, request, cancellationToken));

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<WorkItemSummaryDto>> ChangeStatus(
        Guid id,
        ChangeStatusRequest request,
        CancellationToken cancellationToken)
        => Ok(await workItemService.ChangeStatusAsync(id, request, cancellationToken));

    [HttpPut("{id:guid}/assignee")]
    public async Task<ActionResult<WorkItemSummaryDto>> Assign(
        Guid id,
        AssignWorkItemRequest request,
        CancellationToken cancellationToken)
        => Ok(await workItemService.AssignAsync(id, request, cancellationToken));

    // ------------------------------------------------------- Kontrol listesi

    [HttpPost("{id:guid}/checklist")]
    public async Task<ActionResult<IReadOnlyList<ChecklistItemDto>>> AddChecklistItem(
        Guid id,
        CreateChecklistItemRequest request,
        CancellationToken cancellationToken)
        => Ok(await checklistService.AddAsync(id, request, cancellationToken));

    [HttpPut("{id:guid}/checklist/{itemId:guid}")]
    public async Task<ActionResult<IReadOnlyList<ChecklistItemDto>>> UpdateChecklistItem(
        Guid id,
        Guid itemId,
        UpdateChecklistItemRequest request,
        CancellationToken cancellationToken)
        => Ok(await checklistService.UpdateAsync(id, itemId, request, cancellationToken));

    [HttpDelete("{id:guid}/checklist/{itemId:guid}")]
    public async Task<ActionResult<IReadOnlyList<ChecklistItemDto>>> DeleteChecklistItem(
        Guid id,
        Guid itemId,
        CancellationToken cancellationToken)
        => Ok(await checklistService.DeleteAsync(id, itemId, cancellationToken));

    // ---------------------------------------------------------------- Yorumlar

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<CommentDto>> AddComment(
        Guid id,
        CreateCommentRequest request,
        CancellationToken cancellationToken)
        => Ok(await commentService.AddAsync(id, request, cancellationToken));

    [HttpPut("{id:guid}/comments/{commentId:guid}")]
    public async Task<ActionResult<CommentDto>> UpdateComment(
        Guid id,
        Guid commentId,
        UpdateCommentRequest request,
        CancellationToken cancellationToken)
        => Ok(await commentService.UpdateAsync(id, commentId, request, cancellationToken));

    [HttpDelete("{id:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(
        Guid id,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        await commentService.DeleteAsync(id, commentId, cancellationToken);
        return NoContent();
    }

    // ------------------------------------------------------------ Dosya ekleri

    /// <summary>Göreve dosya yükler (resim, PDF, ZIP, Word, Excel, video ...).</summary>
    [HttpPost("{id:guid}/attachments")]
    [RequestSizeLimit(52_428_800)]
    [EnableRateLimiting(RateLimitingExtensions.UploadPolicy)]
    public async Task<ActionResult<AttachmentDto>> UploadAttachment(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Boş dosya yüklenemez."
            });
        }

        await using var stream = file.OpenReadStream();

        var attachment = await attachmentService.UploadAsync(
            id,
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken);

        return Ok(attachment);
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(
        Guid id,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        await attachmentService.DeleteAsync(id, attachmentId, cancellationToken);
        return NoContent();
    }
}
