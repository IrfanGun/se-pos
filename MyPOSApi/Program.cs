using Microsoft.EntityFrameworkCore;
using MyPOSApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ApiTokenService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapPost("/api/auth/login", (LoginRequest request, IConfiguration configuration, ApiTokenService tokens) =>
{
    var username = configuration["Authentication:Username"];
    var password = configuration["Authentication:Password"];

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
        !ApiTokenService.FixedTimeEquals(request.Username, username) ||
        !ApiTokenService.FixedTimeEquals(request.Password, password))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new LoginResponse(tokens.Create(username), username, 8 * 60 * 60));
})
.WithName("Login");

app.MapGet("/api/dashboard", (HttpRequest request, ApiTokenService tokens) =>
{
    var authorization = request.Headers.Authorization.ToString();
    var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        ? authorization[7..].Trim()
        : string.Empty;

    if (!tokens.TryValidate(token, out var username))
    {
        return Results.Unauthorized();
    }

    var dashboard = new DashboardResponse(
        DateOnly.FromDateTime(DateTime.Today),
        null,
        null,
        null,
        null,
        [],
        false,
        username);

    return Results.Ok(dashboard);
})
.WithName("GetDashboard");

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record LoginRequest(string Username, string Password);
record LoginResponse(string AccessToken, string Username, int ExpiresIn);
record TransactionSummary(string Number, string Time, string Customer, string Total, string Status);
record DashboardResponse(DateOnly Date, decimal? SalesToday, int? TransactionsToday, int? LowStockProducts,
    decimal? AverageTransaction, IReadOnlyList<TransactionSummary> RecentTransactions, bool DataAvailable, string Username);

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
