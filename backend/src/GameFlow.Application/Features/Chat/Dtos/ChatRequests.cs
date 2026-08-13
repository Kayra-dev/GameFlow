namespace GameFlow.Application.Features.Chat.Dtos;

public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;

    public Guid? ReplyToMessageId { get; set; }
}

public class EditMessageRequest
{
    public string Content { get; set; } = string.Empty;
}

public class MessageHistoryRequest
{
    private const int MaxPageSize = 100;

    private int _pageSize = 50;

    /// <summary>Bu zamandan önceki mesajlar getirilir. Boşsa en yeniden başlanır.</summary>
    public DateTime? Before { get; set; }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 50,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }
}

public class MarkMessagesReadRequest
{
    /// <summary>Okundu işaretlenecek mesajlar. Boş gönderilirse odadaki tüm mesajlar işaretlenir.</summary>
    public List<Guid> MessageIds { get; set; } = [];
}
