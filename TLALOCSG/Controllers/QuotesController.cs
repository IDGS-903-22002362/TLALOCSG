using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TLALOCSG.Data;
using TLALOCSG.DTOs.Quotes;
using TLALOCSG.Models;

namespace TLALOCSG.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuotesController : ControllerBase
{
    private readonly IoTIrrigationDbContext _ctx;
    private readonly UserManager<ApplicationUser> _userManager;

    public QuotesController(IoTIrrigationDbContext ctx,
                            UserManager<ApplicationUser> userManager)
    {
        _ctx = ctx;
        _userManager = userManager;
    }

    //POST crear cotización 
    [HttpPost]
    [Authorize(Roles = "Client,Admin")]
    public async Task<ActionResult<QuoteDetailDto>> CreateQuote(CreateQuoteDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Garantizar fila en Customers (por si no existe)
        if (!await _ctx.Customers.AnyAsync(c => c.CustomerId == customerId))
        {
            var user = await _userManager.FindByIdAsync(customerId);
            _ctx.Customers.Add(new Customer
            {
                CustomerId = customerId,
                FullName = user!.FullName
            });
        }

        var quote = new Quote
        {
            CustomerId = customerId,
            QuoteDate = DateTime.UtcNow,
            Status = "Draft"
        };
        _ctx.Quotes.Add(quote);
        await _ctx.SaveChangesAsync();   // para obtener QuoteId

        decimal total = 0;
        foreach (var line in dto.Lines)
        {
            var product = await _ctx.Products.FindAsync(line.ProductId);
            if (product is null) return BadRequest($"Producto {line.ProductId} inexistente.");

            var ql = new QuoteLine
            {
                QuoteId = quote.QuoteId,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = 0 // se fijará al aprobar
            };
            _ctx.QuoteLines.Add(ql);
        }
        await _ctx.SaveChangesAsync();

        return CreatedAtAction(nameof(GetQuote),
              new { id = quote.QuoteId },
              await BuildDetailDtoAsync(quote.QuoteId));
    }

    //GET detalle 
    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuoteDetailDto>> GetQuote(int id)
    {
        var quote = await _ctx.Quotes.FindAsync(id);
        if (quote is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!User.IsInRole("Admin") && quote.CustomerId != userId)
            return Forbid();

        return await BuildDetailDtoAsync(id);
    }

    //PUT approve
    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveQuote(int id, ApproveQuoteDto dto)
    {
        var quote = await _ctx.Quotes
            .Include(q => q.QuoteLines)
            .FirstOrDefaultAsync(q => q.QuoteId == id);

        if (quote is null) return NotFound();
        if (quote.Status != "Draft") return BadRequest("Solo se pueden aprobar borradores.");

        decimal total = 0;
        foreach (var line in quote.QuoteLines)
        {
            var product = await _ctx.Products.FindAsync(line.ProductId);
            line.UnitPrice = product!.BasePrice;                 // lógica de precio
            total += line.UnitPrice * line.Quantity;
        }

        quote.TotalAmount = total;
        quote.Status = "Approved";
        quote.ValidUntil = dto.ValidUntil ?? DateTime.UtcNow.AddDays(30);

        await _ctx.SaveChangesAsync();
        return Ok(await BuildDetailDtoAsync(id));
    }

    //PUT reject 
    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectQuote(int id)
    {
        var quote = await _ctx.Quotes.FindAsync(id);
        if (quote is null) return NotFound();
        if (quote.Status != "Draft") return BadRequest("Solo borradores pueden rechazarse.");

        quote.Status = "Rejected";
        await _ctx.SaveChangesAsync();
        return NoContent();
    }

    //Helper: construir DTO detalle 
    private async Task<QuoteDetailDto> BuildDetailDtoAsync(int quoteId)
    {
        var data = await _ctx.Quotes
            .Include(q => q.QuoteLines)
            .ThenInclude(l => l.Product)
            .FirstAsync(q => q.QuoteId == quoteId);

        var lines = data.QuoteLines.Select(l => new QuoteLineDto(
            l.ProductId,
            l.Product!.Name,
            l.Quantity,
            l.UnitPrice,
            l.Quantity * l.UnitPrice)).ToList();

        return new QuoteDetailDto
        {
            Id = data.QuoteId,
            Status = data.Status,
            QuoteDate = data.QuoteDate,
            ValidUntil = data.ValidUntil,
            TotalAmount = lines.Sum(x => x.LineTotal),
            Lines = lines
        };
    }
}
