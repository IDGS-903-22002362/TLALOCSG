using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer; 
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

using TLALOCSG.Data;
using TLALOCSG.DTOs.Quotes;
using TLALOCSG.Models;
using TLALOCSG.Services.Email;

namespace TLALOCSG.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] 
public class QuotesController : ControllerBase
{
    private readonly IoTIrrigationDbContext _ctx;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _email;

    public QuotesController(
        IoTIrrigationDbContext ctx,
        UserManager<ApplicationUser> userManager,
        IEmailSender email)
    {
        _ctx = ctx;
        _userManager = userManager;
        _email = email;
    }

    // POST: /api/quotes
    [HttpPost]
    [Authorize(Roles = "Client,Admin")]
    public async Task<ActionResult<QuoteDetailDto>> CreateQuote([FromBody] CreateQuoteDto dto)
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
        await _ctx.SaveChangesAsync(); // obtiene QuoteId

        foreach (var line in dto.Lines)
        {
            var product = await _ctx.Products.FindAsync(line.ProductId);
            if (product is null) return BadRequest($"Producto {line.ProductId} inexistente.");

            _ctx.QuoteLines.Add(new QuoteLine
            {
                QuoteId = quote.QuoteId,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = 0 // se fijará al aprobar
            });
        }

        await _ctx.SaveChangesAsync();

        return CreatedAtAction(nameof(GetQuote),
            new { id = quote.QuoteId },
            await BuildDetailDtoAsync(quote.QuoteId));
    }

    // GET: /api/quotes/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuoteDetailDto>> GetQuote(int id)
    {
        var quote = await _ctx.Quotes.AsNoTracking().FirstOrDefaultAsync(q => q.QuoteId == id);
        if (quote is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!User.IsInRole("Admin") && quote.CustomerId != userId)
            return Forbid();

        return await BuildDetailDtoAsync(id);
    }

    // POST: /api/quotes/{id}/email
    [HttpPost("{id:int}/email")]
    [Authorize(Roles = "Client,Admin")]
    public async Task<IActionResult> EmailQuote(int id)
    {
        var quote = await _ctx.Quotes
            .Include(q => q.QuoteLines)
            .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(q => q.QuoteId == id);

        if (quote is null) return NotFound();

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!User.IsInRole("Admin") && quote.CustomerId != currentUserId)
            return Forbid();

        var customer = await _userManager.FindByIdAsync(quote.CustomerId);
        if (customer is null || string.IsNullOrWhiteSpace(customer.Email))
            return BadRequest("El usuario no tiene correo registrado.");

        var rows = quote.QuoteLines.Select(l => $@"
            <tr>
                <td style=""padding:6px 8px;border-bottom:1px solid #eee"">{l.Product!.Name}</td>
                <td style=""padding:6px 8px;border-bottom:1px solid #eee;text-align:right"">{l.Quantity}</td>
                <td style=""padding:6px 8px;border-bottom:1px solid #eee;text-align:right"">{l.UnitPrice:C}</td>
                <td style=""padding:6px 8px;border-bottom:1px solid #eee;text-align:right"">{(l.UnitPrice * l.Quantity):C}</td>
            </tr>");

        var total = quote.QuoteLines.Sum(x => x.UnitPrice * x.Quantity);

        var html = $@"
        <div style=""font-family:Inter,Arial,sans-serif"">
          <h2>Tu cotización #{quote.QuoteId}</h2>
          <p>Estado: <b>{quote.Status}</b></p>
          {(quote.ValidUntil.HasValue ? $"<p>Válido hasta: <b>{quote.ValidUntil:yyyy-MM-dd}</b></p>" : "")}
          <table cellpadding=""0"" cellspacing=""0"" style=""width:100%;border-collapse:collapse;margin-top:12px"">
            <thead>
              <tr style=""background:#f7f7f7"">
                <th style=""text-align:left;padding:8px"">Producto</th>
                <th style=""text-align:right;padding:8px"">Cant.</th>
                <th style=""text-align:right;padding:8px"">Precio</th>
                <th style=""text-align:right;padding:8px"">Total</th>
              </tr>
            </thead>
            <tbody>{string.Join("", rows)}</tbody>
            <tfoot>
              <tr>
                <td colspan=""3"" style=""text-align:right;padding:8px;border-top:1px solid #ddd"">
                  <b>Total</b>
                </td>
                <td style=""text-align:right;padding:8px;border-top:1px solid #ddd"">
                  <b>{total:C}</b>
                </td>
              </tr>
            </tfoot>
          </table>
          <p style=""margin-top:16px"">Gracias por tu interés en TLÁLOC. Responde este correo si tienes dudas.</p>
        </div>";

        await _email.SendAsync(customer.Email!, $"Tu cotización #{quote.QuoteId}", html);
        return NoContent();
    }

    // PUT: /api/quotes/{id}/approve
    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveQuote(int id, [FromBody] ApproveQuoteDto dto)
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
            if (product is null) return BadRequest($"Producto {line.ProductId} inexistente.");
            line.UnitPrice = product.BasePrice; // lógica simple de precio
            total += line.UnitPrice * line.Quantity;
        }

        quote.TotalAmount = total;
        quote.Status = "Approved";
        quote.ValidUntil = dto.ValidUntil ?? DateTime.UtcNow.AddDays(30);

        await _ctx.SaveChangesAsync();
        return Ok(await BuildDetailDtoAsync(id));
    }

    // PUT: /api/quotes/{id}/reject
    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectQuote(int id)
    {
        var quote = await _ctx.Quotes.FirstOrDefaultAsync(q => q.QuoteId == id);
        if (quote is null) return NotFound();
        if (quote.Status != "Draft") return BadRequest("Solo borradores pueden rechazarse.");

        quote.Status = "Rejected";
        await _ctx.SaveChangesAsync();
        return NoContent();
    }

    /* ───── Helper: construir DTO detalle ───── */
    private async Task<QuoteDetailDto> BuildDetailDtoAsync(int quoteId)
    {
        var data = await _ctx.Quotes
            .AsNoTracking()
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
