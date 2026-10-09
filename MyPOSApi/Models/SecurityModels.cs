namespace MyPOSApi.Models;

public static class AuthorizationPolicies
{
    public const string DashboardRead = "dashboard.read";
    public const string ProductsRead = "products.read";
    public const string ProductsWrite = "products.write";
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";

}

public sealed class RbacOptions
{
    public string ProtectedRole { get; set; } = string.Empty;
    public List<PermissionDefinition> Permissions { get; set; } = [];
    public List<RoleDefinition> Roles { get; set; } = [];
}

public sealed class PermissionDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class RoleDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = [];
}

public class AppUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NormalizedUsername { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = [];
}

public class AppRole
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

public class AppPermission
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

public class UserRole
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public AppRole Role { get; set; } = null!;
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public AppRole Role { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public AppPermission Permission { get; set; } = null!;
}

public sealed record UserResponse(Guid Id, string Username, string DisplayName, bool IsActive,
    DateTime CreatedAt, DateTime UpdatedAt, IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);

public sealed record RoleResponse(Guid Id, string Name, string Description);

public sealed record CreateUserRequest(string Username, string DisplayName, string Password, string Role);

public sealed record UpdateUserRequest(string Username, string DisplayName, string? Password,
    string Role, bool IsActive);
