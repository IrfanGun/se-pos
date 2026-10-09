using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPOSApi.Data;
using MyPOSApi.Models;

namespace MyPOSApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = AuthorizationPolicies.UsersRead)]
public sealed class UsersController(AppDbContext db, IPasswordHasher<AppUser> passwordHasher,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await db.Users.AsSplitQuery()
            .AsNoTracking()
            .Include(item => item.UserRoles)
                .ThenInclude(item => item.Role)
                    .ThenInclude(item => item.RolePermissions)
                        .ThenInclude(item => item.Permission)
            .OrderBy(item => item.Username)
            .ToListAsync(cancellationToken);

        return Ok(users.Select(ToResponse).ToArray());
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RoleResponse>>> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await db.Roles.AsNoTracking().Where(item => item.IsActive)
            .OrderBy(item => item.Name).ToListAsync(cancellationToken);
        return Ok(roles.Select(item => new RoleResponse(item.Id, item.Name, item.Description)).ToArray());
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.UsersManage)]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsValidUsername(request.Username) || string.IsNullOrWhiteSpace(request.DisplayName) ||
            string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return BadRequest(new { message = "Username, nama, dan password minimal 8 karakter wajib diisi." });
        }

        var normalizedUsername = request.Username.Trim().ToUpperInvariant();
        if (await db.Users.AnyAsync(item => item.NormalizedUsername == normalizedUsername, cancellationToken))
        {
            return Conflict(new { message = "Username sudah digunakan." });
        }

        var role = await db.Roles.SingleOrDefaultAsync(item => item.Name == request.Role && item.IsActive,
            cancellationToken);
        if (role is null)
        {
            return BadRequest(new { message = "Role tidak valid." });
        }

        var now = DateTime.UtcNow;
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = request.Username.Trim(),
            NormalizedUsername = normalizedUsername,
            DisplayName = request.DisplayName.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { id = user.Id },
            new UserResponse(user.Id, user.Username, user.DisplayName, user.IsActive,
                user.CreatedAt, user.UpdatedAt, [role.Name], []));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.UsersManage)]
    public async Task<ActionResult<UserResponse>> Update(Guid id, [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(item => item.UserRoles)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (!IsValidUsername(request.Username) || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return BadRequest(new { message = "Username dan nama wajib diisi." });
        }

        var normalizedUsername = request.Username.Trim().ToUpperInvariant();
        if (await db.Users.AnyAsync(item => item.Id != id && item.NormalizedUsername == normalizedUsername,
                cancellationToken))
        {
            return Conflict(new { message = "Username sudah digunakan." });
        }

        var role = await db.Roles.SingleOrDefaultAsync(item => item.Name == request.Role && item.IsActive,
            cancellationToken);
        if (role is null)
        {
            return BadRequest(new { message = "Role tidak valid." });
        }

        var protectedRole = configuration["Rbac:ProtectedRole"] ?? string.Empty;
        if (await IsLastProtectedUser(user, cancellationToken) &&
            (!request.IsActive || request.Role != protectedRole))
        {
            return BadRequest(new { message = $"User dengan role {protectedRole} yang aktif terakhir harus tetap aktif dengan role tersebut." });
        }

        user.Username = request.Username.Trim();
        user.NormalizedUsername = normalizedUsername;
        user.DisplayName = request.DisplayName.Trim();
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (request.Password.Length < 8)
            {
                return BadRequest(new { message = "Password minimal 8 karakter." });
            }
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        }

        db.UserRoles.RemoveRange(user.UserRoles);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new UserResponse(user.Id, user.Username, user.DisplayName, user.IsActive,
            user.CreatedAt, user.UpdatedAt, [role.Name], []));
    }

    private async Task<bool> IsLastProtectedUser(AppUser user, CancellationToken cancellationToken)
    {
        var protectedRole = configuration["Rbac:ProtectedRole"];
        if (string.IsNullOrWhiteSpace(protectedRole)) return false;
        var role = await db.Roles.SingleAsync(item => item.Name == protectedRole && item.IsActive, cancellationToken);
        return await db.UserRoles.Where(item => item.RoleId == role.Id)
            .Join(db.Users.Where(item => item.IsActive), item => item.UserId, item => item.Id,
                (_, activeUser) => activeUser.Id).CountAsync(cancellationToken) == 1 &&
            user.UserRoles.Any(item => item.RoleId == role.Id);
    }

    private static bool IsValidUsername(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 320 && value.Contains('@');

    private static UserResponse ToResponse(AppUser user) =>
        new(user.Id, user.Username, user.DisplayName, user.IsActive, user.CreatedAt, user.UpdatedAt,
            user.UserRoles.Where(item => item.Role.IsActive).Select(item => item.Role.Name).Distinct().ToArray(),
            user.UserRoles.Where(item => item.Role.IsActive).SelectMany(item => item.Role.RolePermissions)
                .Select(item => item.Permission.Name).Distinct().ToArray());
}
