using Microsoft.EntityFrameworkCore;
using MyPOSApi.Models;

namespace MyPOSApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AppRole> Roles => Set<AppRole>();
    public DbSet<AppPermission> Permissions => Set<AppPermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var product = modelBuilder.Entity<Product>();

        product.ToTable("Products");
        product.HasKey(item => item.Id);
        product.Property(item => item.Name)
            .HasMaxLength(200)
            .IsRequired();
        product.Property(item => item.Price)
            .HasPrecision(18, 2)
            .IsRequired();
        product.Property(item => item.CreatedAt)
            .IsRequired();
        product.Property(item => item.UpdatedAt)
            .IsRequired();

        var user = modelBuilder.Entity<AppUser>();
        user.ToTable("Users");
        user.HasKey(item => item.Id);
        user.Property(item => item.Username).HasMaxLength(320).IsRequired();
        user.Property(item => item.NormalizedUsername).HasMaxLength(320).IsRequired();
        user.HasIndex(item => item.NormalizedUsername).IsUnique();
        user.Property(item => item.DisplayName).HasMaxLength(160).IsRequired();
        user.Property(item => item.PasswordHash).IsRequired();

        var role = modelBuilder.Entity<AppRole>();
        role.ToTable("Roles");
        role.HasKey(item => item.Id);
        role.Property(item => item.Name).HasMaxLength(80).IsRequired();
        role.HasIndex(item => item.Name).IsUnique();
        role.Property(item => item.Description).HasMaxLength(240).IsRequired();
        role.Property(item => item.IsActive).IsRequired();

        var permission = modelBuilder.Entity<AppPermission>();
        permission.ToTable("Permissions");
        permission.HasKey(item => item.Id);
        permission.Property(item => item.Name).HasMaxLength(100).IsRequired();
        permission.HasIndex(item => item.Name).IsUnique();
        permission.Property(item => item.Description).HasMaxLength(240).IsRequired();

        var userRole = modelBuilder.Entity<UserRole>();
        userRole.ToTable("UserRoles");
        userRole.HasKey(item => new { item.UserId, item.RoleId });
        userRole.HasOne(item => item.User).WithMany(item => item.UserRoles)
            .HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        userRole.HasOne(item => item.Role).WithMany(item => item.UserRoles)
            .HasForeignKey(item => item.RoleId).OnDelete(DeleteBehavior.Cascade);

        var rolePermission = modelBuilder.Entity<RolePermission>();
        rolePermission.ToTable("RolePermissions");
        rolePermission.HasKey(item => new { item.RoleId, item.PermissionId });
        rolePermission.HasOne(item => item.Role).WithMany(item => item.RolePermissions)
            .HasForeignKey(item => item.RoleId).OnDelete(DeleteBehavior.Cascade);
        rolePermission.HasOne(item => item.Permission).WithMany(item => item.RolePermissions)
            .HasForeignKey(item => item.PermissionId).OnDelete(DeleteBehavior.Cascade);
    }
}
