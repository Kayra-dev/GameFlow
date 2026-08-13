using System.Linq.Expressions;
using GameFlow.Application.Features.Users.Dtos;
using GameFlow.Domain.Entities;
using GameFlow.Domain.Enums;

namespace GameFlow.Application.Features.Users;

/// <summary>
/// Kullanıcı projeksiyonları. Tek bir yerde tutulur ki farklı modüller aynı
/// gösterimi tekrarlamadan (ve fazladan sütun çekmeden) kullanabilsin.
/// </summary>
public static class UserProjections
{
    public static readonly Expression<Func<User, UserSummaryDto>> ToSummary = user => new UserSummaryDto(
        user.Id,
        user.FullName,
        user.Email,
        user.JobTitle,
        user.AvatarUrl,
        (SystemRole)user.RoleId,
        user.IsOnline,
        user.LastSeenAt);

    /// <summary>
    /// Bellekteki bir varlığı DTO'ya çevirir. Yalnızca sorgu sonucu
    /// materyalize edildikten sonra kullanılmalıdır; EF Core bu metot çağrısını
    /// SQL'e çeviremez. Sorgu içinde <see cref="ToSummary"/> kullanın.
    /// </summary>
    public static UserSummaryDto MapSummary(User user) => new(
        user.Id,
        user.FullName,
        user.Email,
        user.JobTitle,
        user.AvatarUrl,
        (SystemRole)user.RoleId,
        user.IsOnline,
        user.LastSeenAt);
}
