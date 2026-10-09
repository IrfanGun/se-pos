namespace MyPOSApi.Models;

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(string AccessToken, string Username, int ExpiresIn,
    IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);
