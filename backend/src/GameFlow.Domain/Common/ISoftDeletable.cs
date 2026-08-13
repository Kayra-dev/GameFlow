namespace GameFlow.Domain.Common;

/// <summary>
/// Fiziksel silme yerine mantıksal silme uygulanan varlıklar.
/// DbContext üzerinde global query filter ile otomatik olarak filtrelenirler.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    Guid? DeletedById { get; set; }
}
