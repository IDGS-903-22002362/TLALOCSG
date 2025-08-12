using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TLALOCSG.Data;
using TLALOCSG.Models;

namespace TLALOCSG.Controllers;

[ApiController]
[Route("api/[controller]")]

public class SuppliersController : ControllerBase
{
    private readonly IoTIrrigationDbContext _context;

    public SuppliersController(IoTIrrigationDbContext context)
    {
        _context = context;
    }

    // GET: /api/suppliers
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliers()
    {
        var suppliers = await _context.Suppliers
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return Ok(suppliers);
    }

    // GET: /api/suppliers/{id}
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Supplier>> GetSupplier(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);

        if (supplier == null)
            return NotFound("Proveedor no encontrado.");

        return Ok(supplier);
    }

    // POST: /api/suppliers
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Supplier>> CreateSupplier([FromBody] Supplier supplier)
    {
        supplier.CreatedAt = DateTime.UtcNow;
        supplier.UpdatedAt = DateTime.UtcNow;

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSupplier), new { id = supplier.SupplierId }, supplier);
    }

    // PUT: /api/suppliers/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSupplier(int id, [FromBody] Supplier updated)
    {
        if (id != updated.SupplierId)
            return BadRequest("El ID del proveedor no coincide.");

        var existing = await _context.Suppliers.FindAsync(id);
        if (existing == null)
            return NotFound("Proveedor no encontrado.");

        existing.Name = updated.Name;
        existing.ContactName = updated.ContactName;
        existing.Email = updated.Email;
        existing.Phone = updated.Phone;
        existing.Address = updated.Address;
        existing.IsActive = updated.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        _context.Entry(existing).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: /api/suppliers/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.Purchases)
            .FirstOrDefaultAsync(s => s.SupplierId == id);

        if (supplier == null)
            return NotFound("Proveedor no encontrado.");

        // Opcional: Validar si tiene compras asociadas antes de eliminar
        if (supplier.Purchases.Any())
            return BadRequest("No se puede eliminar un proveedor con compras registradas.");

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();

        return NoContent();
    }


    // GET: /api/suppliers/active
    [HttpGet("active")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<Supplier>>> GetActiveSuppliers()
    {
        var activeSuppliers = await _context.Suppliers
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return Ok(activeSuppliers);
    }

    [HttpPatch("{id}/toggle-status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleSupplierStatus(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
            return NotFound("Proveedor no encontrado.");

        supplier.IsActive = !supplier.IsActive;


        await _context.SaveChangesAsync();

        return Ok(new
        {
            supplier.SupplierId,
            supplier.IsActive
        });
    }

}
