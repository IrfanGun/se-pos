using Microsoft.AspNetCore.Mvc;
using MyPOSApi.Models;

namespace MyPOSApi.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(ApiTokenService tokens) : ControllerBase
{
    [HttpGet]
    public ActionResult<DashboardResponse> Get()
    {
        var authorization = Request.Headers.Authorization.ToString();
        var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization[7..].Trim()
            : string.Empty;

        if (!tokens.TryValidate(token, out var username))
        {
            return Unauthorized();
        }

        var dashboard = new DashboardResponse(
            DateOnly.FromDateTime(DateTime.Today),
            null,
            null,
            null,
            null,
            [],
            false,
            username);

        return Ok(dashboard);
    }
}
