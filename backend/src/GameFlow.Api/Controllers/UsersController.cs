using GameFlow.Api.Extensions;
using GameFlow.Application.Common.Models;
using GameFlow.Application.Features.Users;
using GameFlow.Application.Features.Users.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameFlow.Api.Controllers;

/// <summary>
/// Kullanıcı yönetimi. Oluşturma, güncelleme, silme ve şifre sıfırlama
/// işlemleri yalnızca yöneticilere açıktır.
/// </summary>
public class UsersController(IUserService userService) : ApiControllerBase
{
    /// <summary>Filtrelenebilir, sayfalanmış kullanıcı listesi.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserSummaryDto>>> GetList(
        [FromQuery] UserListRequest request,
        CancellationToken cancellationToken)
        => Ok(await userService.GetListAsync(request, cancellationToken));

    /// <summary>Görev atama ve üye seçimi için aktif kullanıcı listesi.</summary>
    [HttpGet("assignable")]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> GetAssignable(
        CancellationToken cancellationToken)
        => Ok(await userService.GetAssignableAsync(cancellationToken));

    /// <summary>Kullanıcı ayrıntısı (profil ekranı).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await userService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<ActionResult<UserDetailDto>> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<ActionResult<UserDetailDto>> Update(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
        => Ok(await userService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await userService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Yöneticinin bir kullanıcının şifresini sıfırlaması.</summary>
    [HttpPost("{id:guid}/reset-password")]
    [Authorize(AuthenticationExtensions.AdminPolicy)]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await userService.ResetPasswordAsync(id, request, cancellationToken);
        return NoContent();
    }
}
