namespace GameFlow.Domain.Common;

/// <summary>
/// Tüm veritabanı varlıklarının türediği temel sınıf.
/// Anahtar olarak sıralı (v7) GUID kullanılır; bu sayede kümelenmiş index
/// parçalanması yaşanmaz ve ekleme performansı korunur.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
