using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiBase : ControllerBase
{
    /// Id of the logged-in user (from the session). Only valid on [SessionAuthorize] endpoints.
    protected int CurrentUserId => HttpContext.Session.GetUserId() ?? 0;

    protected IActionResult Forbidden(string message) =>
        StatusCode(StatusCodes.Status403Forbidden, new { message });

    protected IActionResult Missing(string message) =>
        NotFound(new { message });
}
