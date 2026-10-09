using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyPOSApi.Data;

namespace MyPOSApi;

public sealed class ApiAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ApiTokenService tokens,
    AppDbContext db)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = header[7..].Trim();
        if (!tokens.TryValidate(token, out var username))
        {
            return AuthenticateResult.Fail("Token tidak valid atau sudah kedaluwarsa.");
        }

        var user = await db.Users.AsSplitQuery()
            .Include(item => item.UserRoles)
                .ThenInclude(item => item.Role)
                    .ThenInclude(item => item.RolePermissions)
                        .ThenInclude(item => item.Permission)
            .SingleOrDefaultAsync(item => item.NormalizedUsername == username.ToUpperInvariant(), Context.RequestAborted);

        if (user is null || !user.IsActive)
        {
            return AuthenticateResult.Fail("User tidak aktif.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("display_name", user.DisplayName)
        };
        var activeRoles = user.UserRoles.Where(item => item.Role.IsActive).ToArray();
        claims.AddRange(activeRoles.Select(item => new Claim(ClaimTypes.Role, item.Role.Name)));
        claims.AddRange(activeRoles.SelectMany(item => item.Role.RolePermissions)
            .Select(item => new Claim(AuthorizationConstants.PermissionClaim, item.Permission.Name))
            .DistinctBy(item => item.Value));

        return AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name));
    }
}
