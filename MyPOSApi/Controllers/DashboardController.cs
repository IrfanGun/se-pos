using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyPOSApi.Models;

namespace MyPOSApi.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    [Authorize(Policy = AuthorizationPolicies.DashboardRead)]
    [HttpGet]
    public ActionResult<DashboardResponse> Get()
    {
        var dashboard = new DashboardResponse(
            DateOnly.FromDateTime(DateTime.Today),
            null,
            null,
            null,
            null,
            [],
            false,
            User.Identity?.Name ?? string.Empty);

        return Ok(dashboard);
    }
}
