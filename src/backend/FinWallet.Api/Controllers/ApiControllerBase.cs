using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FinWallet.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected Guid UserId
    {
        get
        {
            var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(subClaim, out var parsedGuid) ? parsedGuid : Guid.Empty;
        }
    }

    protected string IpAddress => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

    protected string UserAgent => HttpContext.Request.Headers.UserAgent.ToString() ?? "Unknown";
}
