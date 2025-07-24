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
            var now = DateTime.UtcNow;

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

    public class CompraRequest
    {
        public int SupplierId { get; set; }
        public int MaterialId { get; set; }
        public string MovementType { get; set; } = "E"; // Entrada
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }
}
