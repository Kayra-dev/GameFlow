namespace GameFlow.Domain.Entities;

/// <summary>Bir mesajın hangi kullanıcı tarafından ne zaman okunduğu.</summary>
public class MessageRead
{
    public Guid MessageId { get; set; }

    public Guid UserId { get; set; }

    public DateTime ReadAt { get; set; } = DateTime.UtcNow;

    public Message Message { get; set; } = null!;
    public User User { get; set; } = null!;
}
