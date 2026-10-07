using Microsoft.AspNetCore.Mvc;
using MyPOSApi.Models;

namespace MyPOSApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IConfiguration configuration, ApiTokenService tokens) : ControllerBase
{
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        var username = configuration["Authentication:Username"];
        var password = configuration["Authentication:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
            !ApiTokenService.FixedTimeEquals(request.Username, username) ||
            !ApiTokenService.FixedTimeEquals(request.Password, password))
        {
            return Unauthorized();
        }

        return Ok(new LoginResponse(tokens.Create(username), username, 8 * 60 * 60));
    }
}
