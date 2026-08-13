using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameFlow.Api.Controllers;

/// <summary>
/// Tüm controller'ların türediği taban sınıf. Varsayılan olarak kimlik doğrulaması ister;
/// açık uç noktalar <see cref="AllowAnonymousAttribute"/> ile işaretlenir.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Ters vekil arkasında gerçek istemci IP adresini döner.</summary>
    protected string? ClientIpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();
}
