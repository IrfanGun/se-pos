using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPOSApp.Models;

namespace MyPOSApp.Controllers;

[Authorize(Policy = AppPermissions.UsersRead)]
public sealed class UsersController(IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var client = CreateApiClient();
            var users = await client.GetFromJsonAsync<List<ApiUserResponse>>("api/users", cancellationToken) ?? [];
            var roles = await client.GetFromJsonAsync<List<ApiRoleResponse>>("api/users/roles", cancellationToken) ?? [];
            return View(new UsersPageViewModel(users, roles));
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiUnavailable = true;
            return View(new UsersPageViewModel([], []));
        }
    }

    [HttpGet]
    [Authorize(Policy = AppPermissions.UsersManage)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var roles = await GetRoles(cancellationToken);
        return View(new UserFormPageViewModel(new UserFormModel { Role = roles.FirstOrDefault()?.Name ?? "" }, roles,
            false));
    }

    [HttpPost]
    [Authorize(Policy = AppPermissions.UsersManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormPageViewModel page, CancellationToken cancellationToken)
    {
        var roles = await GetRoles(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(page with { Roles = roles, IsEdit = false });
        }

        var client = CreateApiClient();
        using var response = await client.PostAsJsonAsync("api/users", page.Form, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, await GetApiError(response, "User gagal ditambahkan."));
            return View(page with { Roles = roles, IsEdit = false });
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = AppPermissions.UsersManage)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var client = CreateApiClient();
        using var response = await client.GetAsync("api/users", cancellationToken);
        response.EnsureSuccessStatusCode();
        var users = await response.Content.ReadFromJsonAsync<List<ApiUserResponse>>(cancellationToken) ?? [];
        var user = users.SingleOrDefault(item => item.Id == id);
        if (user is null) return NotFound();
        var roles = await GetRoles(cancellationToken);
        return View("Create", new UserFormPageViewModel(new UserFormModel
        {
            Username = user.Username, DisplayName = user.DisplayName, Role = user.Roles.FirstOrDefault() ?? "",
            IsActive = user.IsActive
        }, roles, true, id));
    }

    [HttpPost]
    [Authorize(Policy = AppPermissions.UsersManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UserFormPageViewModel page, CancellationToken cancellationToken)
    {
        var roles = await GetRoles(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View("Create", page with { Roles = roles, IsEdit = true, Id = id });
        }

        var client = CreateApiClient();
        using var response = await client.PutAsJsonAsync($"api/users/{id}", page.Form, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, await GetApiError(response, "User gagal diperbarui."));
            return View("Create", page with { Roles = roles, IsEdit = true, Id = id });
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<ApiRoleResponse>> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await CreateApiClient().GetFromJsonAsync<List<ApiRoleResponse>>("api/users/roles", cancellationToken);
        return roles ?? [];
    }

    private HttpClient CreateApiClient()
    {
        var client = httpClientFactory.CreateClient("MyPOSApi");
        var token = User.FindFirstValue("pos_api_token");
        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<string> GetApiError(HttpResponseMessage response, string fallback)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            return error?.Message ?? fallback;
        }
        catch (System.Text.Json.JsonException) { return fallback; }
    }
}

public sealed record UserFormPageViewModel(UserFormModel Form, IReadOnlyList<ApiRoleResponse> Roles,
    bool IsEdit, Guid? Id = null);

public sealed record ApiErrorResponse(string Message);
