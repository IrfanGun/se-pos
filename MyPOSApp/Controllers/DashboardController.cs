using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPOSApp.Models;

namespace MyPOSApp.Controllers;

[Authorize(Policy = AppPermissions.DashboardRead)]
public sealed class DashboardController(IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var token = User.FindFirstValue("pos_api_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var client = httpClientFactory.CreateClient("MyPOSApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.GetAsync("api/dashboard", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }

            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<ApiDashboardResponse>(cancellationToken);
            if (data is null)
            {
                ViewBag.ApiUnavailable = true;
                return View(new DashboardViewModel(DateOnly.FromDateTime(DateTime.Today), null, null, null, null, false, []));
            }

            return View(new DashboardViewModel(data.Date, data.SalesToday, data.TransactionsToday,
                data.LowStockProducts, data.AverageTransaction, data.DataAvailable,
                data.RecentTransactions.Select(item => new TransactionViewModel(
                    item.Number, item.Time, item.Customer, item.Total, item.Status)).ToArray()));
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiUnavailable = true;
            return View(new DashboardViewModel(DateOnly.FromDateTime(DateTime.Today), null, null, null, null, false, []));
        }
    }

}
