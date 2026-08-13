using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.Teams.Dtos;

public class CreateTeamRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TeamCategory Category { get; set; } = TeamCategory.Software;

    public string ColorHex { get; set; } = "#6366F1";

    public string? IconKey { get; set; }

    /// <summary>Takım lideri. Boş bırakılabilir, sonradan atanabilir.</summary>
    public Guid? LeaderId { get; set; }

    public List<Guid> MemberIds { get; set; } = [];
}

public class UpdateTeamRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TeamCategory Category { get; set; }

    public string ColorHex { get; set; } = "#6366F1";

    public string? IconKey { get; set; }
}

public class AssignLeaderRequest
{
    /// <summary>null gönderilirse takımın liderliği boşaltılır.</summary>
    public Guid? UserId { get; set; }
}

public class AddTeamMembersRequest
{
    public List<Guid> UserIds { get; set; } = [];
}

public class TeamListRequest
{
    public string? Search { get; set; }

    public TeamCategory? Category { get; set; }

    /// <summary>Yalnızca oturum sahibinin üyesi olduğu takımlar.</summary>
    public bool OnlyMine { get; set; }
}
