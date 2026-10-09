using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MyPOSApi.Data;
using MyPOSApi.Models;
using MyPOSApi;

var builder = WebApplication.CreateBuilder(args);
var rbacOptions = builder.Configuration.GetSection("Rbac").Get<RbacOptions>()
                  ?? throw new InvalidOperationException("Konfigurasi Rbac wajib diisi.");

builder.Services.AddControllers();
builder.Services.AddSingleton<ApiTokenService>();
builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddAuthentication(AuthorizationConstants.Scheme)
    .AddScheme<AuthenticationSchemeOptions, ApiAuthenticationHandler>(AuthorizationConstants.Scheme, _ => { });
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in rbacOptions.Permissions.Where(item => !string.IsNullOrWhiteSpace(item.Name)))
    {
        options.AddPolicy(permission.Name, policy => policy.RequireAuthenticatedUser()
            .RequireClaim(AuthorizationConstants.PermissionClaim, permission.Name));
    }
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await DbInitializer.InitializeAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(),
        scope.ServiceProvider.GetRequiredService<IConfiguration>(), rbacOptions);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
