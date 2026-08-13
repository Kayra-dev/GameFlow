namespace GameFlow.Application.Features.Sprints.Dtos;

public class CreateSprintRequest
{
    public Guid ProjectId { get; set; }

    public Guid? TeamId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Goal { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}

public class UpdateSprintRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Goal { get; set; }

    public Guid? TeamId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}

public class CompleteSprintRequest
{
    public string? RetrospectiveNotes { get; set; }

    /// <summary>
    /// Tamamlanmayan görevlerin taşınacağı sprint. null ise görevler
    /// sprintten çıkarılıp backlog'a döner.
    /// </summary>
    public Guid? MoveUnfinishedToSprintId { get; set; }
}

public class SprintListRequest
{
    public Guid? ProjectId { get; set; }

    public Guid? TeamId { get; set; }

    public Domain.Enums.SprintStatus? Status { get; set; }
}
