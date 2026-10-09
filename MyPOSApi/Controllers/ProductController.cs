using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MyPOSApi.Data;
using MyPOSApi.Models;

namespace MyPOSApi.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Policy = AuthorizationPolicies.ProductsRead)]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await _context.Products
            .OrderByDescending(product => product.Id)
            .ToListAsync(cancellationToken);

        return Ok(products);
    }

    // GET: api/products/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FindAsync([id], cancellationToken);

        if (product == null)
        {
            return NotFound(new
            {
                message = "Product not found"
            });
        }

        return Ok(product);
    }

    // POST: api/products
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ProductsWrite)]
    public async Task<ActionResult<Product>> Create(Product product, CancellationToken cancellationToken)
    {
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.Products.AddAsync(product, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product
        );
    }

    // PUT: api/products/{id}
    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ProductsWrite)]
    public async Task<IActionResult> Update(Guid id, Product request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FindAsync([id], cancellationToken);

        if (product == null)
        {
            return NotFound(new
            {
                message = "Product not found"
            });
        }

        product.Name = request.Name;
        product.Price = request.Price;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(product);
    }

    // DELETE: api/products/{id}
    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.ProductsWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FindAsync([id], cancellationToken);

        if (product == null)
        {
            return NotFound(new
            {
                message = "Product not found"
            });
        }

        _context.Products.Remove(product);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Product deleted successfully"
        });
    }
}
