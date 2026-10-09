using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MyPOSApp.Models;

namespace MyPOSApp.Controllers;

public sealed class AccountController(IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var client = httpClientFactory.CreateClient("MyPOSApi");
            using var response = await client.PostAsJsonAsync("api/auth/login",
                new { username = model.Username, password = model.Password }, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                ModelState.AddModelError(string.Empty, "Email atau password tidak sesuai.");
                return View(model);
            }

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "API POS tidak dapat memproses login saat ini.");
                return View(model);
            }

            var login = await response.Content.ReadFromJsonAsync<ApiLoginResponse>(cancellationToken);
            if (login is null || string.IsNullOrWhiteSpace(login.AccessToken))
            {
                ModelState.AddModelError(string.Empty, "Respons login dari API tidak valid.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, login.Username),
                new("pos_api_token", login.AccessToken)
            };
            claims.AddRange(login.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
            claims.AddRange(login.Permissions.Select(permission => new Claim("permission", permission)));
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var properties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity), properties);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "API POS belum terhubung. Pastikan MyPOSApi sedang berjalan.");
            return View(model);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            ModelState.AddModelError(string.Empty, "Koneksi ke API POS melewati batas waktu. Coba lagi.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
