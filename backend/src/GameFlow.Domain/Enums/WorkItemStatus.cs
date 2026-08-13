namespace GameFlow.Domain.Enums;

/// <summary>Kanban kolonlarına karşılık gelen görev durumları.</summary>
public enum WorkItemStatus
{
    /// <summary>Bekliyor</summary>
    Pending = 1,
    /// <summary>Yapılacak</summary>
    Todo = 2,
    /// <summary>Devam Ediyor</summary>
    InProgress = 3,
    /// <summary>Kod İncelemede</summary>
    CodeReview = 4,
    /// <summary>Testte</summary>
    Testing = 5,
    /// <summary>Tamamlandı</summary>
    Done = 6,
    /// <summary>İptal</summary>
    Cancelled = 7
}
