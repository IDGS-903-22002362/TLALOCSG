using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TLALOCSG.Data;
using TLALOCSG.Models;

namespace TLALOCSG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IoTIrrigationDbContext _context;

        public OrdersController(IoTIrrigationDbContext context)
        {
            _context = context;
        }

        // 1. POST /api/orders - Cliente crea orden desde carrito
        [HttpPost]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto dto)
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (dto.Lines is null || dto.Lines.Count == 0)
                return BadRequest("La orden debe contener al menos un producto.");

            // Validación de stock
            foreach (var line in dto.Lines)
            {
                var bomItems = await _context.ProductBOMs
                    .Where(b => b.ProductId == line.ProductId)
                    .ToListAsync();

                foreach (var bom in bomItems)
                {
                    var stock = await _context.MaterialStocks
                        .FirstOrDefaultAsync(s => s.MaterialId == bom.MaterialId && s.Location == "MAIN");

                    var requiredQty = bom.Quantity * line.Quantity;

                    if (stock == null || stock.QuantityOnHand < requiredQty)
                        return BadRequest($"Stock insuficiente para el material ID {bom.MaterialId} del producto ID {line.ProductId}.");
                }
            }

            // Descontar stock
            foreach (var line in dto.Lines)
            {
                var bomItems = await _context.ProductBOMs
                    .Where(b => b.ProductId == line.ProductId)
                    .ToListAsync();

                foreach (var bom in bomItems)
                {
                    var stock = await _context.MaterialStocks
                        .FirstOrDefaultAsync(s => s.MaterialId == bom.MaterialId && s.Location == "MAIN");

                    stock!.QuantityOnHand -= bom.Quantity * line.Quantity;
                }
            }

            // Crear la orden
            var order = new Order
            {
                CustomerId = customerId!,
                OrderDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Status = "Pending",
                TotalAmount = dto.Lines.Sum(l => l.Quantity * l.UnitPrice),
                OrderLines = dto.Lines.Select(l => new OrderLine
                {
                    ProductId = l.ProductId,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { order.OrderId, message = "Orden creada exitosamente." });
        }

        // 2. GET /api/orders/mine - Cliente ve sus órdenes 
        [HttpGet("mine")]
        [Authorize(Roles = "Client")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetMyOrders()
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var orders = await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OrderResponseDto
                {
                    OrderId = o.OrderId,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    OrderDate = o.OrderDate,
                    Lines = o.OrderLines.Select(ol => new OrderLineDto
                    {
                        ProductId = ol.ProductId,
                        Quantity = ol.Quantity,
                        UnitPrice = ol.UnitPrice
                    }).ToList()
                })
                .ToListAsync();

            return Ok(orders);
        }

        // 3. GET /api/orders - Admin ve todas las órdenes
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetAllOrders()
        {
            var orders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OrderResponseDto
                {
                    OrderId = o.OrderId,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    OrderDate = o.OrderDate,
                    Lines = o.OrderLines.Select(ol => new OrderLineDto
                    {
                        ProductId = ol.ProductId,
                        Quantity = ol.Quantity,
                        UnitPrice = ol.UnitPrice
                    }).ToList()
                })
                .ToListAsync();

            return Ok(orders);
        }

        // 4. POST /api/orders/{id}/payments - Registrar pago
        [HttpPost("{id}/payments")]
        [Authorize]
        public async Task<IActionResult> RegisterPayment(int id, [FromBody] PaymentDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound("Orden no encontrada.");

            if (order.Status != "Pending")
                return BadRequest("La orden ya fue pagada o cancelada.");

            var payment = new Payment
            {
                OrderId = id,
                PaymentDate = DateTime.UtcNow,
                Amount = dto.Amount,
                Method = dto.Method,
                Reference = dto.Reference
            };

            _context.Payments.Add(payment);
            order.Status = "Paid";
            await _context.SaveChangesAsync();

            return Ok("Pago registrado exitosamente.");
        }

        // 5. PUT /api/orders/{id}/status - Actualizar estado
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string newStatus)
        {
            var validStatuses = new[] { "Pending", "Paid", "Shipped", "Cancelled" };

            if (!validStatuses.Contains(newStatus))
                return BadRequest("Estado no válido.");

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound("Orden no encontrada.");

            order.Status = newStatus;
            await _context.SaveChangesAsync();

            return Ok($"Estado actualizado a {newStatus}.");
        }
        /*────────────────────────── DTOs ───────────────────────────────*/
        public class OrderCreateDto
        {
            public List<OrderLineDto> Lines { get; set; } = new();
        }

        public class OrderLineDto
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }
        public class OrderResponseDto
        {
            public int OrderId { get; set; }
            public string Status { get; set; } = null!;
            public decimal TotalAmount { get; set; }
            public DateTime OrderDate { get; set; }
            public List<OrderLineDto> Lines { get; set; } = new();
        }
        public class PaymentDto
        {
            public decimal Amount { get; set; }
            public string Method { get; set; } = null!;
            public string? Reference { get; set; }
        }
    }
}
