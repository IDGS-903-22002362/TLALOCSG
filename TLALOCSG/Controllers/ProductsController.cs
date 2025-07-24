using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TLALOCSG.Data;
using TLALOCSG.Models;

namespace TLALOCSG.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IoTIrrigationDbContext _context;

    public ProductsController(IoTIrrigationDbContext context)
    {
        _context = context;
    }

    // GET: /api/products?name=Filtro
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts([FromQuery] string? name)
    {
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(p => p.Name.Contains(name));

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Ok(products);
    }

    // GET: /api/products/{id}/bom
    [HttpGet("{id}/bom")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<object>>> GetProductBOM(int id)
    {
        var bom = await _context.ProductBOMs
            .Where(b => b.ProductId == id)
            .Include(b => b.Material)
            .Select(b => new
            {
                b.MaterialId,
                b.Material.Name,
                b.Material.SKU,
                b.Quantity,
                b.Material.UnitOfMeasure,
                b.Material.StandardCost
            })
            .ToListAsync();

        if (!bom.Any())
            return NotFound("Este producto no tiene lista de materiales.");

        return Ok(bom);
    }

    // POST: /api/products
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] Product product)
    {
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProductBOM), new { id = product.ProductId }, product);
    }

    // POST: /api/products/bom
    [HttpPost("with-bom")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProductWithBOM([FromBody] ProductWithBOMDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var product = new Product
        {
            SKU = dto.SKU,
            Name = dto.Name,
            Description = dto.Description,
            BasePrice = dto.BasePrice,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        foreach (var item in dto.BOM)
        {
            var materialExists = await _context.Materials.AnyAsync(m => m.MaterialId == item.MaterialId);
            if (!materialExists)
                return BadRequest($"El material con ID {item.MaterialId} no existe.");

            _context.ProductBOMs.Add(new ProductBOM
            {
                ProductId = product.ProductId,
                MaterialId = item.MaterialId,
                Quantity = item.Quantity
            });
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProductBOM), new { id = product.ProductId }, new
        {
            product.ProductId,
            product.SKU,
            product.Name,
            product.BasePrice,
            dto.BOM
        });
    }

    // PUT: /api/products/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product updated)
    {
        if (id != updated.ProductId)
            return BadRequest("El ID del producto no coincide.");

        var existing = await _context.Products.FindAsync(id);
        if (existing == null)
            return NotFound("Producto no encontrado.");

        existing.Name = updated.Name;
        existing.SKU = updated.SKU;
        existing.Description = updated.Description;
        existing.BasePrice = updated.BasePrice;
        existing.UpdatedAt = DateTime.UtcNow;

        _context.Entry(existing).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Elimina un producto y su lista de materiales (BOM).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.ProductBOMs)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
            return NotFound("Producto no encontrado.");

        _context.ProductBOMs.RemoveRange(product.ProductBOMs);
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /*────────────────────────── DTOs ───────────────────────────────*/
    public class ProductWithBOMDto
    {
        public string SKU { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public List<BOMItemDto> BOM { get; set; } = new();
    }

    public class BOMItemDto
    {
        public int MaterialId { get; set; }
        public decimal Quantity { get; set; }
    }
}
