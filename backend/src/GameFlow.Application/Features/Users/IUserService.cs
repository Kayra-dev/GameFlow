using GameFlow.Application.Common.Models;
using GameFlow.Application.Features.Users.Dtos;

namespace GameFlow.Application.Features.Users;

public interface IUserService
{
    Task<PagedResult<UserSummaryDto>> GetListAsync(
        UserListRequest request,
        CancellationToken cancellationToken = default);

    Task<UserDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Görev atama ve üye seçimi alanlarında kullanılan aktif kullanıcı listesi.</summary>
    Task<IReadOnlyList<UserSummaryDto>> GetAssignableAsync(CancellationToken cancellationToken = default);

    Task<UserDetailDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserDetailDto> UpdateAsync(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(
        Guid id,
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);
}
