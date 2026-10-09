using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyPOSApi.Models;

namespace MyPOSApi.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext db, IConfiguration configuration,
        RbacOptions rbac,
        CancellationToken cancellationToken = default)
    {
        Validate(rbac);
        await db.Database.MigrateAsync(cancellationToken);

        foreach (var definition in rbac.Permissions)
        {
            var permission = await db.Permissions.SingleOrDefaultAsync(item => item.Name == definition.Name,
                cancellationToken);
            if (permission is null)
            {
                db.Permissions.Add(new AppPermission
                {
                    Id = Guid.NewGuid(),
                    Name = definition.Name,
                    Description = definition.Description
                });
            }
            else if (permission.Description != definition.Description)
            {
                permission.Description = definition.Description;
            }
        }

        foreach (var definition in rbac.Roles)
        {
            var role = await db.Roles.SingleOrDefaultAsync(item => item.Name == definition.Name, cancellationToken);
            if (role is null)
            {
                db.Roles.Add(new AppRole
                {
                    Id = Guid.NewGuid(), Name = definition.Name, Description = definition.Description, IsActive = true
                });
            }
            else
            {
                role.Description = definition.Description;
                role.IsActive = true;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var configuredRoleNames = rbac.Roles.Select(item => item.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removedRoles = await db.Roles
            .Where(item => !configuredRoleNames.Contains(item.Name))
            .ToListAsync(cancellationToken);
        foreach (var role in removedRoles)
        {
            role.IsActive = false;
        }
        await db.SaveChangesAsync(cancellationToken);

        var permissions = await db.Permissions.ToDictionaryAsync(item => item.Name, cancellationToken);
        var roles = await db.Roles.Where(item => item.IsActive).ToDictionaryAsync(item => item.Name, cancellationToken);
        foreach (var definition in rbac.Roles)
        {
            var role = roles[definition.Name];
            var existing = await db.RolePermissions.Where(item => item.RoleId == role.Id).ToListAsync(cancellationToken);
            var desired = definition.Permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
            db.RolePermissions.RemoveRange(existing.Where(item => !desired.Contains(item.Permission.Name)));
            foreach (var permissionName in desired)
            {
                if (!permissions.TryGetValue(permissionName, out var permission))
                {
                    throw new InvalidOperationException($"Role '{definition.Name}' merujuk permission '{permissionName}' yang tidak ada.");
                }
                if (existing.All(item => item.PermissionId != permission.Id))
                {
                    db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
                }
            }
        }

        var seedUsername = configuration["Authentication:Username"]?.Trim();
        var seedPassword = configuration["Authentication:Password"];
        if (!string.IsNullOrWhiteSpace(seedUsername) && !string.IsNullOrWhiteSpace(seedPassword))
        {
            var normalizedUsername = seedUsername.ToUpperInvariant();
            var admin = await db.Users.SingleOrDefaultAsync(item => item.NormalizedUsername == normalizedUsername,
                cancellationToken);
            if (admin is null)
            {
                var now = DateTime.UtcNow;
                admin = new AppUser
                {
                    Id = Guid.NewGuid(),
                    Username = seedUsername,
                    NormalizedUsername = normalizedUsername,
                    DisplayName = configuration["Authentication:DisplayName"]?.Trim() ?? seedUsername,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                admin.PasswordHash = new PasswordHasher<AppUser>().HashPassword(admin, seedPassword);
                db.Users.Add(admin);
                db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = roles[rbac.ProtectedRole].Id });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(RbacOptions rbac)
    {
        if (string.IsNullOrWhiteSpace(rbac.ProtectedRole) || rbac.Permissions.Count == 0 || rbac.Roles.Count == 0 ||
            !rbac.Roles.Any(item => item.Name == rbac.ProtectedRole))
        {
            throw new InvalidOperationException("Konfigurasi Rbac harus memiliki ProtectedRole, Roles, dan Permissions.");
        }
        var permissionNames = rbac.Permissions.Select(item => item.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (permissionNames.Count != rbac.Permissions.Count || rbac.Roles.Select(item => item.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase).Count != rbac.Roles.Count ||
            rbac.Roles.SelectMany(item => item.Permissions).Any(item => !permissionNames.Contains(item)))
        {
            throw new InvalidOperationException("Konfigurasi Rbac memiliki nama duplikat atau permission yang tidak terdaftar.");
        }
    }
}
