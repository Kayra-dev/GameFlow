using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.Projects.Dtos;

public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Görev anahtarı öneki (örn. "ODY"). Büyük harfe çevrilir.</summary>
    public string Key { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

    public string ColorHex { get; set; } = "#8B5CF6";

    public string? Genre { get; set; }

    public string? Platforms { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? TargetReleaseDate { get; set; }

    public List<Guid> MemberIds { get; set; } = [];
}

public class UpdateProjectRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ProjectStatus Status { get; set; }

    public string ColorHex { get; set; } = "#8B5CF6";

    public string? Genre { get; set; }

    public string? Platforms { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? TargetReleaseDate { get; set; }
}

public class AddProjectMembersRequest
{
    public List<Guid> UserIds { get; set; } = [];

    public bool IsManager { get; set; }
}

public class ProjectListRequest
{
    public string? Search { get; set; }

    public ProjectStatus? Status { get; set; }

    /// <summary>Yalnızca oturum sahibinin üyesi olduğu projeler.</summary>
    public bool OnlyMine { get; set; }
}
