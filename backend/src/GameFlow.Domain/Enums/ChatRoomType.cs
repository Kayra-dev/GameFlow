namespace GameFlow.Domain.Enums;

public enum ChatRoomType
{
    /// <summary>Takıma ait sohbet odası.</summary>
    Team = 1,
    /// <summary>Yalnızca takım liderlerinin ve adminlerin görebildiği oda.</summary>
    Leaders = 2,
    /// <summary>Proje genelinde herkese açık oda.</summary>
    Project = 3
}
