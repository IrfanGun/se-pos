using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPOSApp.Models;

namespace MyPOSApp.Controllers;

[Authorize]
public sealed class ProductsController(IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var client = CreateApiClient();
            var products = await client.GetFromJsonAsync<List<ApiProductResponse>>(
                "api/products", cancellationToken);

            return View(products ?? []);
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiUnavailable = true;
            return View(new List<ApiProductResponse>());
        }
    }

    [HttpGet]
    public IActionResult Create() => View(new ProductFormModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var client = CreateApiClient();
            using var response = await client.PostAsJsonAsync("api/products", model, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Produk gagal ditambahkan.");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "MyPOSApi tidak dapat dijangkau.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var client = CreateApiClient();
            using var response = await client.GetAsync($"api/products/{id}", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            response.EnsureSuccessStatusCode();
            var product = await response.Content.ReadFromJsonAsync<ApiProductResponse>(cancellationToken);
            if (product is null)
            {
                return NotFound();
            }

            ViewBag.ProductId = id;
            return View(new ProductFormModel { Name = product.Name, Price = product.Price });
        }
        catch (HttpRequestException)
        {
            TempData["ProductError"] = "MyPOSApi tidak dapat dijangkau.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ProductFormModel model, CancellationToken cancellationToken)
    {
        ViewBag.ProductId = id;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var client = CreateApiClient();
            using var response = await client.PutAsJsonAsync($"api/products/{id}", model, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Produk gagal diperbarui.");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "MyPOSApi tidak dapat dijangkau.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var client = CreateApiClient();
            using var response = await client.DeleteAsync($"api/products/{id}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                TempData["ProductError"] = "Produk gagal dihapus.";
            }
        }
        catch (HttpRequestException)
        {
            TempData["ProductError"] = "MyPOSApi tidak dapat dijangkau.";
        }

        return RedirectToAction(nameof(Index));
    }

    private HttpClient CreateApiClient()
    {
        var client = httpClientFactory.CreateClient("MyPOSApi");
        var token = User.FindFirstValue("pos_api_token");
        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }
}
