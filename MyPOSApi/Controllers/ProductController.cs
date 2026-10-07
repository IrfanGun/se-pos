using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPOSApi.Data;
using MyPOSApi.Models;

namespace MyPOSApi.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        var products = await _context.Products
            .OrderByDescending(product => product.Id)
            .ToListAsync();

        return Ok(products);
    }

    // GET: api/products/1
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await _context.Products.FindAsync(id);

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
    public async Task<ActionResult<Product>> Create(Product product)
    {
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        _context.Products.Add(product);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product
        );
    }

    // PUT: api/products/1
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Product request)
    {
        var product = await _context.Products.FindAsync(id);

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

        await _context.SaveChangesAsync();

        return Ok(product);
    }

    // DELETE: api/products/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound(new
            {
                message = "Product not found"
            });
        }

        _context.Products.Remove(product);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Product deleted successfully"
        });
    }
}