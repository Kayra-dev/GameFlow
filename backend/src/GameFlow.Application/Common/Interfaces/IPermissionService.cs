namespace GameFlow.Application.Common.Interfaces;

/// <summary>
/// Kaynak bazlı yetki denetimleri. Controller seviyesindeki rol politikaları
/// "kim bu uç noktayı çağırabilir" sorusunu yanıtlar; bu servis ise
/// "bu kullanıcı tam olarak bu takıma/projeye/göreve dokunabilir mi" sorusunu yanıtlar.
/// Tüm modüller aynı kuralları burada paylaşır.
/// </summary>
public interface IPermissionService
{
    /// <summary>Kullanıcı bu takımın lideri mi (yöneticiler her zaman true).</summary>
    Task<bool> CanManageTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcı bu takımın üyesi mi (yöneticiler her zaman true).</summary>
    Task<bool> IsTeamMemberAsync(Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcı bu projenin üyesi mi (yöneticiler her zaman true).</summary>
    Task<bool> IsProjectMemberAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcı proje ayarlarını yönetebilir mi (yönetici veya proje yöneticisi).</summary>
    Task<bool> CanManageProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcı en az bir takımın lideri mi (lider sohbeti erişimi için).</summary>
    Task<bool> IsAnyTeamLeaderAsync(CancellationToken cancellationToken = default);

    /// <summary>Yetki yoksa <see cref="Domain.Exceptions.ForbiddenException"/> fırlatır.</summary>
    Task EnsureCanManageTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="EnsureCanManageTeamAsync"/>
    Task EnsureTeamMemberAsync(Guid teamId, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="EnsureCanManageTeamAsync"/>
    Task EnsureProjectMemberAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="EnsureCanManageTeamAsync"/>
    Task EnsureCanManageProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının liderlik ettiği takımların kimlikleri.
    /// Yönetici ise tüm takımlar döner.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetManageableTeamIdsAsync(CancellationToken cancellationToken = default);
}
