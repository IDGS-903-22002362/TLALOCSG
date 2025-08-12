using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TLALOCSG.Data;
using System.Security.Claims;

using TLALOCSG.Models;

namespace TLALOCSG.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchasesController : ControllerBase
{
    private readonly IoTIrrigationDbContext _context;

    public PurchasesController(IoTIrrigationDbContext context)
    {
        _context = context;
    }

    [HttpPost("surtir")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SurtirMaterial([FromBody] CompraRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.Now;


            // 1. Registrar movimiento de entrada
            var movimiento = new MaterialMovement
            {
                MaterialId = request.MaterialId,
                MovementType = request.MovementType,
                Quantity = request.Quantity,
                UnitCost = request.UnitCost,
                MovementDate = now,
                Reference = "Surtido",
                CreatedAt = now
            };
            _context.MaterialMovements.Add(movimiento);
            await _context.SaveChangesAsync();

            // 2. Buscar stock existente por MaterialId (sin importar Location)
            var stock = await _context.MaterialStocks
                .FirstOrDefaultAsync(s => s.MaterialId == request.MaterialId);

            if (stock != null)
            {
                // Ya existe: solo sumar cantidad
                stock.QuantityOnHand += request.Quantity;
                _context.MaterialStocks.Update(stock);
            }
            else
            {
                // No existe: crear nuevo stock con Location "ALMACEN"
                stock = new MaterialStock
                {
                    MaterialId = request.MaterialId,
                    Location = "ALMACEN", // Puedes cambiarlo si deseas otro valor por defecto
                    QuantityOnHand = request.Quantity
                };
                _context.MaterialStocks.Add(stock);
            }
            await _context.SaveChangesAsync();

            // 3. Crear compra
            var total = request.Quantity * request.UnitCost;
            var compra = new Purchase
            {
                SupplierId = request.SupplierId,
                PurchaseDate = now,
                Status = "Pagada",
                TotalAmount = total,
                CreatedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1",
                // Reemplazar con el usuario autenticado si aplica
                CreatedAt = now
            };
            _context.Purchases.Add(compra);
            await _context.SaveChangesAsync();

            // 4. Crear línea de compra
            var linea = new PurchaseLine
            {
                PurchaseId = compra.PurchaseId,
                MaterialId = request.MaterialId,
                Quantity = request.Quantity,
                UnitCost = request.UnitCost,
                LineTotal = total
            };
            _context.PurchaseLines.Add(linea);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Compra registrada y material surtido con éxito.",
                stockActualizado = new
                {
                    stock.MaterialId,
                    stock.QuantityOnHand
                }
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin")] // Opcional, según si quieres protegerlo
    public async Task<IActionResult> GetPurchasesHistory()
    {
        var compras = await _context.Purchases
            .Include(p => p.Supplier) // Para traer datos del proveedor
            .OrderByDescending(p => p.PurchaseDate)
            .Select(p => new
            {
                p.PurchaseId,
                SupplierName = p.Supplier.Name,
                p.PurchaseDate,
                p.TotalAmount,
                p.Status
            })
            .ToListAsync();

        return Ok(compras);
    }

    [HttpGet("suppliers/basic")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSuppliersBasic()
    {
        var suppliers = await _context.Suppliers
            .Select(s => new { s.SupplierId, s.Name })
            .ToListAsync();
        return Ok(suppliers);
    }

    [HttpGet("materials/basic")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetMaterialsBasic()
    {
        var materials = await _context.Materials
            .Select(m => new { m.MaterialId, m.Name })
            .ToListAsync();
        return Ok(materials);
    }
    [HttpPut("editar/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EditarCompra(int id, [FromBody] CompraRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var compra = await _context.Purchases
                .Include(p => p.PurchaseLines)
                .FirstOrDefaultAsync(p => p.PurchaseId == id);

            if (compra == null)
                return NotFound(new { message = "Compra no encontrada" });

            // Actualizar proveedor
            compra.SupplierId = request.SupplierId;
            compra.PurchaseDate = DateTime.Now;

            // Actualizar línea de compra (solo tomaremos la primera línea asociada)
            var linea = compra.PurchaseLines.FirstOrDefault();
            if (linea != null)
            {
                linea.MaterialId = request.MaterialId;
                linea.Quantity = request.Quantity;
                linea.UnitCost = request.UnitCost;
                linea.LineTotal = request.Quantity * request.UnitCost;
            }

            // Actualizar total
            compra.TotalAmount = request.Quantity * request.UnitCost;

            _context.Purchases.Update(compra);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok(new { message = "Compra actualizada con éxito" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("cancelar/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CancelarCompra(int id)
    {
        var compra = await _context.Purchases.FindAsync(id);
        if (compra == null)
            return NotFound(new { message = "Compra no encontrada" });

        compra.Status = "Cancelada";
        _context.Purchases.Update(compra);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Compra cancelada con éxito" });
    }
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetCompraById(int id)
    {
        var compra = await _context.Purchases
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseLines)
            .ThenInclude(pl => pl.Material)
            .FirstOrDefaultAsync(p => p.PurchaseId == id);

        if (compra == null)
            return NotFound(new { message = "Compra no encontrada" });

        var compraDto = new
        {
            compra.PurchaseId,
            SupplierId = compra.SupplierId,
            SupplierName = compra.Supplier.Name,
            MaterialId = compra.PurchaseLines.FirstOrDefault()?.MaterialId,
            MaterialName = compra.PurchaseLines.FirstOrDefault()?.Material?.Name,
            Quantity = compra.PurchaseLines.FirstOrDefault()?.Quantity,
            UnitCost = compra.PurchaseLines.FirstOrDefault()?.UnitCost,
            TotalAmount = compra.TotalAmount,
            PurchaseDate = compra.PurchaseDate
        };

        return Ok(compraDto);
    }

    public class CompraRequest
    {
        public int SupplierId { get; set; }
        public int MaterialId { get; set; }
        public string MovementType { get; set; } = "E"; // Entrada
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }
}
