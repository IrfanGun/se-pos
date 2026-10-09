using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyPOSApi.Data;
using MyPOSApi.Models;

namespace MyPOSApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AppDbContext db, IPasswordHasher<AppUser> passwordHasher,
    ApiTokenService tokens) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = request.Username.Trim().ToUpperInvariant();
        var user = await db.Users.AsSplitQuery()
            .Include(item => item.UserRoles)
                .ThenInclude(item => item.Role)
                    .ThenInclude(item => item.RolePermissions)
                        .ThenInclude(item => item.Permission)
            .SingleOrDefaultAsync(item => item.NormalizedUsername == normalizedUsername, cancellationToken);

        if (user is null || !user.IsActive ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized();
        }

        var roles = user.UserRoles.Where(item => item.Role.IsActive)
            .Select(item => item.Role.Name).Distinct().ToArray();
        var permissions = user.UserRoles.Where(item => item.Role.IsActive)
            .SelectMany(item => item.Role.RolePermissions)
            .Select(item => item.Permission.Name).Distinct().ToArray();
        return Ok(new LoginResponse(tokens.Create(user.Username), user.Username, 8 * 60 * 60,
            roles, permissions));
    }
}
