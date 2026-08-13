using GameFlow.Domain.Common;

namespace GameFlow.Domain.Entities;

/// <summary>
/// Erişim tokenını yenilemek için kullanılan tek kullanımlık token.
/// Rotasyon uygulanır: kullanılan token iptal edilir ve yerine yenisi verilir.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    /// <summary>Rotasyon zincirini takip etmek için, bu tokenın yerine geçen token.</summary>
    public string? ReplacedByToken { get; set; }

    public string? CreatedByIp { get; set; }

    public User User { get; set; } = null!;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsActive => RevokedAt is null && !IsExpired;
}
