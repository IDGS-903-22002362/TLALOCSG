using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using TLALOCSG.Data;
using TLALOCSG.Models;
using Microsoft.EntityFrameworkCore;

namespace TLALOCSG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly IoTIrrigationDbContext _context;

        public ReviewsController(IoTIrrigationDbContext context)
        {
            _context = context;
        }

        //GET: Obtiene todas las reseñas aprobadas de un producto.
        [HttpGet("product/{productId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ReviewResponseDto>>> GetReviewsForProduct(int productId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.Customer)
                .Where(r => r.ProductId == productId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewResponseDto
                {
                    ReviewId = r.ReviewId,
                    ProductId = r.ProductId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    CustomerName = r.Customer.FullName
                })
                .ToListAsync();

            return Ok(reviews);
        }


        //POST: Permite a un cliente dejar una reseña si ha comprado y recibido el producto.
        [HttpPost("product/{productId}")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> PostReview(int productId, [FromBody] ReviewDto reviewDto)
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Verificar que el producto exista
            var productExists = await _context.Products.AnyAsync(p => p.ProductId == productId);
            if (!productExists)
                return NotFound("El producto no existe.");

            // 2. Verificar que el cliente haya comprado ese producto y que el pedido esté entregado
            var hasDeliveredOrder = await _context.Orders
                .Include(o => o.OrderLines)
                .AnyAsync(o =>
                    o.CustomerId == customerId &&
                    o.Status == "Delivered" &&
                    o.OrderLines.Any(ol => ol.ProductId == productId));

            if (!hasDeliveredOrder)
                return BadRequest("Solo puedes dejar una reseña si has comprado y recibido este producto.");

            // 3. Crear la reseña
            var review = new Review
            {
                ProductId = productId,
                CustomerId = customerId!,
                Rating = reviewDto.Rating,
                Comment = reviewDto.Comment,
                CreatedAt = DateTime.UtcNow,
                IsApproved = false  // Pendiente de aprobación por Admin
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok("Tu reseña ha sido enviada para revisión.");
        }

        //PUT: Aprobar la reseña
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveReview(int id, [FromQuery] bool approve = true)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound("Reseña no encontrada.");

            review.IsApproved = approve;
            await _context.SaveChangesAsync();

            return Ok(approve ? "Reseña aprobada." : "Reseña rechazada.");
        }
        /*────────────────────────── DTOs ───────────────────────────────*/
        public class ReviewDto
        {
            [Range(1, 5)]
            public byte Rating { get; set; }

            [MaxLength(1000)]
            public string? Comment { get; set; }
        }
        public class ReviewResponseDto
        {
            public int ReviewId { get; set; }
            public int ProductId { get; set; }
            public byte Rating { get; set; }
            public string? Comment { get; set; }
            public DateTime CreatedAt { get; set; }
            public string CustomerName { get; set; } = default!;
        }
    }
}
