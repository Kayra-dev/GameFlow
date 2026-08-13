using GameFlow.Domain.Common;
using GameFlow.Domain.Enums;

namespace GameFlow.Domain.Entities;

/// <summary>
/// Sohbet odası. Takım odaları takıma, lider odası stüdyo geneline,
/// proje odası ise projeye bağlıdır.
/// </summary>
public class ChatRoom : BaseEntity
{
    public ChatRoomType Type { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? TeamId { get; set; }

    public Guid? ProjectId { get; set; }

    /// <summary>Sistem tarafından otomatik oluşturulan oda (silinemez).</summary>
    public bool IsSystem { get; set; }

    public Team? Team { get; set; }
    public Project? Project { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
