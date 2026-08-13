using GameFlow.Domain.Enums;

namespace GameFlow.Domain.Entities;

/// <summary>
/// Sistem rolleri. Sabit üç kayıt olarak seed edilir ve <see cref="SystemRole"/>
/// enum değerleriyle birebir eşleşen tamsayı anahtar kullanır.
/// </summary>
public class Role
{
    public int Id { get; set; }

    /// <summary>Kod tarafında kullanılan teknik ad (Admin, TeamLeader, TeamMember).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Arayüzde gösterilen Türkçe ad.</summary>
    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
