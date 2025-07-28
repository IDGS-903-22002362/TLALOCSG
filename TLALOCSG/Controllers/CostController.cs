using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TLALOCSG.Data;
using TLALOCSG.Models;

namespace TLALOCSG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CostController : ControllerBase
    {
        private readonly IoTIrrigationDbContext _context;

        public CostController(IoTIrrigationDbContext context)
        {
            _context = context;
        }

        [HttpGet("promedio/{materialId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetCosteoPromedio(int materialId)
        {
            var entradas = await (from p in _context.Purchases
                                  join pl in _context.PurchaseLines on p.PurchaseId equals pl.PurchaseId
                                  where pl.MaterialId == materialId
                                  select new
                                  {
                                      Fecha = p.PurchaseDate,
                                      Cantidad = pl.Quantity,
                                      CostoUnitario = pl.UnitCost
                                  }).ToListAsync();

            var salidas = await (from o in _context.Orders
                                 join ol in _context.OrderLines on o.OrderId equals ol.OrderId
                                 join pr in _context.Products on ol.ProductId equals pr.ProductId
                                 join bom in _context.ProductBOMs on pr.ProductId equals bom.ProductId
                                 where bom.MaterialId == materialId
                                 select new
                                 {
                                     Fecha = o.OrderDate,
                                     Cantidad = ol.Quantity * bom.Quantity,
                                     OrderFecha = o.OrderDate
                                 }).ToListAsync();

            var movimientos = new List<dynamic>();

            // Agregar ENTRADAS
            foreach (var entrada in entradas)
            {
                movimientos.Add(new
                {
                    entrada.Fecha,
                    Entrada = entrada.Cantidad,
                    Salida = 0,
                    CostoUnitario = entrada.CostoUnitario
                });
            }

            // Agregar SALIDAS con CostoUnitario correcto (última compra antes o igual a esa fecha)
            foreach (var salida in salidas)
            {
                // Buscar el último precio de compra antes o igual a la fecha de la salida
                var ultimaCompra = entradas
                    .Where(e => e.Fecha <= salida.Fecha)
                    .OrderByDescending(e => e.Fecha)
                    .FirstOrDefault();

                var costoUnitarioSalida = ultimaCompra?.CostoUnitario ?? 0m;

                movimientos.Add(new
                {
                    salida.Fecha,
                    Entrada = 0,
                    Salida = salida.Cantidad,
                    CostoUnitario = costoUnitarioSalida
                });
            }

            // Ordenar cronológicamente
            var tabla = movimientos.OrderBy(m => m.Fecha).ToList();

            decimal existencias = 0;
            decimal saldo = 0;
            decimal promedio = 0;
            decimal? precioAnterior = null;
            var resultado = new List<object>();

            foreach (var fila in tabla)
            {
                decimal debo = 0;
                decimal haber = 0;
                decimal costoUnitario = fila.CostoUnitario;

                if (fila.Entrada > 0)
                {
                    // ENTRADA
                    existencias += fila.Entrada;
                    debo = fila.Entrada * costoUnitario;
                    saldo += debo;

                    if (precioAnterior != costoUnitario)
                    {
                        promedio = costoUnitario;
                        precioAnterior = costoUnitario;
                    }
                }
                else
                {
                    // SALIDA
                    existencias -= fila.Salida;
                    haber = fila.Salida * promedio;
                    saldo -= haber;
                }

                resultado.Add(new
                {
                    Fecha = fila.Fecha.ToString("yyyy-MM-dd"),
                    Entrada = fila.Entrada,
                    Salida = fila.Salida,
                    Existencias = existencias,
                    CostoUnitario = fila.CostoUnitario,
                    Promedio = fila.Entrada > 0 && precioAnterior == fila.CostoUnitario ? promedio : (decimal?)null,
                    Debo = debo,
                    Haber = haber,
                    Saldo = saldo
                });
            }

            return Ok(resultado);
        }
    }
}
