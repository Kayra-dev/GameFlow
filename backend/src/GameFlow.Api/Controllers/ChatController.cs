using GameFlow.Api.Extensions;
using GameFlow.Application.Features.Chat;
using GameFlow.Application.Features.Chat.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameFlow.Api.Controllers;

/// <summary>
/// Sohbet REST uç noktaları. Anlık iletim SignalR (/hubs/chat) üzerinden yapılır;
/// bu uç noktalar geçmiş yükleme ve dosya paylaşımı için kullanılır.
/// </summary>
public class ChatController(IChatService chatService) : ApiControllerBase
{
    /// <summary>Kullanıcının erişebildiği sohbet odaları.</summary>
    [HttpGet("rooms")]
    public async Task<ActionResult<IReadOnlyList<ChatRoomDto>>> GetRooms(
        CancellationToken cancellationToken)
        => Ok(await chatService.GetRoomsAsync(cancellationToken));

    /// <summary>Lider sohbet odası (yalnızca takım liderleri ve yöneticiler).</summary>
    [HttpGet("rooms/leaders")]
    public async Task<ActionResult<ChatRoomDto>> GetLeadersRoom(CancellationToken cancellationToken)
        => Ok(await chatService.GetLeadersRoomAsync(cancellationToken));

    [HttpGet("rooms/{roomId:guid}")]
    public async Task<ActionResult<ChatRoomDto>> GetRoom(
        Guid roomId,
        CancellationToken cancellationToken)
        => Ok(await chatService.GetRoomAsync(roomId, cancellationToken));

    /// <summary>Mesaj geçmişi. Sayfalama imleç (before) tabanlıdır.</summary>
    [HttpGet("rooms/{roomId:guid}/messages")]
    public async Task<ActionResult<MessagePageDto>> GetMessages(
        Guid roomId,
        [FromQuery] MessageHistoryRequest request,
        CancellationToken cancellationToken)
        => Ok(await chatService.GetMessagesAsync(roomId, request, cancellationToken));

    [HttpPost("rooms/{roomId:guid}/messages")]
    public async Task<ActionResult<MessageDto>> SendMessage(
        Guid roomId,
        SendMessageRequest request,
        CancellationToken cancellationToken)
        => Ok(await chatService.SendMessageAsync(roomId, request, cancellationToken));

    [HttpPut("rooms/{roomId:guid}/messages/{messageId:guid}")]
    public async Task<ActionResult<MessageDto>> EditMessage(
        Guid roomId,
        Guid messageId,
        EditMessageRequest request,
        CancellationToken cancellationToken)
        => Ok(await chatService.EditMessageAsync(roomId, messageId, request, cancellationToken));

    [HttpDelete("rooms/{roomId:guid}/messages/{messageId:guid}")]
    public async Task<IActionResult> DeleteMessage(
        Guid roomId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        await chatService.DeleteMessageAsync(roomId, messageId, cancellationToken);
        return NoContent();
    }

    /// <summary>Mesajları okundu işaretler; kalan okunmamış sayısını döner.</summary>
    [HttpPut("rooms/{roomId:guid}/read")]
    public async Task<ActionResult<int>> MarkAsRead(
        Guid roomId,
        MarkMessagesReadRequest request,
        CancellationToken cancellationToken)
        => Ok(await chatService.MarkAsReadAsync(roomId, request, cancellationToken));

    /// <summary>Bir mesajı kimlerin okuduğu.</summary>
    [HttpGet("rooms/{roomId:guid}/messages/{messageId:guid}/reads")]
    public async Task<ActionResult<IReadOnlyList<MessageReadReceiptDto>>> GetReadReceipts(
        Guid roomId,
        Guid messageId,
        CancellationToken cancellationToken)
        => Ok(await chatService.GetReadReceiptsAsync(roomId, messageId, cancellationToken));

    /// <summary>Sohbete dosya veya resim paylaşır.</summary>
    [HttpPost("rooms/{roomId:guid}/attachments")]
    [RequestSizeLimit(52_428_800)]
    [EnableRateLimiting(RateLimitingExtensions.UploadPolicy)]
    public async Task<ActionResult<MessageDto>> SendAttachment(
        Guid roomId,
        IFormFile file,
        [FromForm] string? caption,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Boş dosya paylaşılamaz."
            });
        }

        await using var stream = file.OpenReadStream();

        return Ok(await chatService.SendAttachmentAsync(
            roomId,
            stream,
            file.FileName,
            file.ContentType,
            caption,
            cancellationToken));
    }
}
