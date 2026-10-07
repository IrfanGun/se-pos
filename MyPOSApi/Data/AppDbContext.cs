using Microsoft.EntityFrameworkCore;
using MyPOSApi.Models;

namespace MyPOSApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

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
    }
}
