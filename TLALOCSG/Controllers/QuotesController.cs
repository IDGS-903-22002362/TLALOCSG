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
using TLALOCSG.Services.Quotes;


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
                UnitPrice = product.BasePrice
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
            .Include(q => q.QuoteLines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(q => q.QuoteId == id);

        if (quote is null) return NotFound();

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!User.IsInRole("Admin") && quote.CustomerId != currentUserId) return Forbid();

        var customer = await _userManager.FindByIdAsync(quote.CustomerId);
        if (customer is null || string.IsNullOrWhiteSpace(customer.Email))
            return BadRequest("El usuario no tiene correo registrado.");

        // Líneas (muestra BasePrice si UnitPrice=0)
        var lineRows = quote.QuoteLines.Select(l =>
        {
            var price = l.UnitPrice > 0 ? l.UnitPrice : l.Product!.BasePrice;
            var total = price * l.Quantity;
            return $@"
            <tr>
              <td style=""padding:6px 8px;border-bottom:1px solid #eee"">{l.Product!.Name}</td>
              <td style=""padding:6px 8px;border-bottom:1px solid #eee;text-align:right"">{l.Quantity}</td>
              <td style=""padding:6px 8px;border-bottom:1px solid #eee;text-align:right"">{price:C}</td>
              <td style=""padding:6px 8px;border-bottom:1px solid #eee;text-align:right"">{total:C}</td>
            </tr>";
        });

        var products = quote.TotalProducts > 0
            ? quote.TotalProducts
            : quote.QuoteLines.Sum(l => (l.UnitPrice > 0 ? l.UnitPrice : l.Product!.BasePrice) * l.Quantity);

        var install = quote.InstallBase;
        var transport = quote.TransportCost;
        var shipping = quote.ShippingCost;
        var grand = quote.TotalAmount > 0 ? quote.TotalAmount : (products + install + transport + shipping);

        string fulfillmentLabel = quote.Fulfillment switch
        {
            "Installation" => "Instalación",
            "Shipping" => "Envío a domicilio",
            "DevicesOnly" => "Solo productos",
            _ => "—"
        };

        var details = $@"
      <div style=""font-family:Inter,Arial,sans-serif"">
        <h2>Tu cotización #{quote.QuoteId}</h2>
        <p>Estado: <b>{quote.Status}</b></p>
        {(quote.ValidUntil.HasValue ? $"<p>Válido hasta: <b>{quote.ValidUntil:yyyy-MM-dd}</b></p>" : "")}
        <p>Modalidad: <b>{fulfillmentLabel}</b>{(string.IsNullOrWhiteSpace(quote.StateCode) ? "" : $" — Estado: <b>{quote.StateCode}</b>")}</p>

        <table cellpadding=""0"" cellspacing=""0"" style=""width:100%;border-collapse:collapse;margin-top:12px"">
          <thead>
            <tr style=""background:#f7f7f7"">
              <th style=""text-align:left;padding:8px"">Producto</th>
              <th style=""text-align:right;padding:8px"">Cant.</th>
              <th style=""text-align:right;padding:8px"">Precio</th>
              <th style=""text-align:right;padding:8px"">Total</th>
            </tr>
          </thead>
          <tbody>{string.Join("", lineRows)}</tbody>
        </table>

        <table cellpadding=""0"" cellspacing=""0"" style=""width:100%;border-collapse:collapse;margin-top:12px"">
          <tbody>
            <tr><td style=""padding:6px 8px"">Subtotal productos</td><td style=""padding:6px 8px;text-align:right""><b>{products:C}</b></td></tr>
            <tr><td style=""padding:6px 8px"">Instalación (base)</td><td style=""padding:6px 8px;text-align:right"">{install:C}</td></tr>
            <tr><td style=""padding:6px 8px"">Transporte</td><td style=""padding:6px 8px;text-align:right"">{transport:C}</td></tr>
            <tr><td style=""padding:6px 8px"">Envío</td><td style=""padding:6px 8px;text-align:right"">{shipping:C}</td></tr>
            <tr style=""border-top:1px solid #ddd"">
              <td style=""padding:8px""><b>Total</b></td>
              <td style=""padding:8px;text-align:right""><b>{grand:C}</b></td>
            </tr>
          </tbody>
        </table>

        <p style=""margin-top:16px"">Gracias por tu interés en TLÁLOC. Responde este correo si tienes dudas.</p>
      </div>";

        await _email.SendAsync(customer.Email!, $"Tu cotización #{quote.QuoteId}", details);
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
    [HttpPost("{id:int}/price")]
    [Authorize(Roles = "Client,Admin")]
    public async Task<IActionResult> PreviewPrice(
    int id,
    QuoteOptionsDto dto,
    [FromServices] IQuotePricingService pricing)
    {
        // Verifica que la cotización exista
        var q = await _ctx.Quotes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.QuoteId == id);

        if (q is null)
            return NotFound("La cotización no existe.");

        // Seguridad: solo Admin o dueño de la cotización
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!User.IsInRole("Admin") && q.CustomerId != userId)
            return Forbid();

        try
        {
            var p = await pricing.CalculateAsync(id, dto);
            return Ok(p);
        }
        catch (ArgumentException ex)
        {
            // Datos inválidos (p. ej., fulfillment/estado faltantes)
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            // Estado no encontrado en StateRates
            return BadRequest(new { error = ex.Message });
        }
    }


    [HttpPut("{id:int}/options")]
    [Authorize(Roles = "Client,Admin")]
    public async Task<IActionResult> SetOptions(
    int id, QuoteOptionsDto dto, [FromServices] IQuotePricingService pricing)
    {
        var q = await _ctx.Quotes
            .Include(x => x.QuoteLines)
                .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(x => x.QuoteId == id);

        if (q is null)
            return NotFound("La cotización no existe.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!User.IsInRole("Admin") && q.CustomerId != userId)
            return Forbid();

        try
        {
            var p = await pricing.CalculateAsync(id, dto);

            q.Fulfillment = dto.Fulfillment;
            q.StateCode = dto.StateCode;
            q.TotalProducts = p.Products;
            q.InstallBase = p.InstallBase;
            q.TransportCost = p.Transport;
            q.ShippingCost = p.Shipping;
            q.TotalAmount = p.GrandTotal;
            q.UpdatedAt = DateTime.UtcNow;

            await _ctx.SaveChangesAsync();

            return Ok(p);
        }
        catch (ArgumentException ex)
        {
            // Error de datos enviados (estado vacío, fulfillment inválido, etc.)
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            // Estado no encontrado en StateRates
            return BadRequest(new { error = ex.Message });
        }
    }


    // En Approve (Admin), antes de guardar, valida que existan opciones y recalcula:
    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveQuote(int id, ApproveQuoteDto dto,
                                                 [FromServices] IQuotePricingService pricing)
    {
        var quote = await _ctx.Quotes
            .Include(q => q.QuoteLines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(q => q.QuoteId == id);

        if (quote is null) return NotFound();
        if (quote.Status != "Draft") return BadRequest("Solo se pueden aprobar borradores.");
        if (string.IsNullOrWhiteSpace(quote.Fulfillment))
            return BadRequest("Faltan opciones de cumplimiento.");

        var pr = await pricing.CalculateAsync(id, new QuoteOptionsDto(quote.Fulfillment!, quote.StateCode, null));

        foreach (var line in quote.QuoteLines)
            line.UnitPrice = line.Product!.BasePrice;

        quote.TotalProducts = pr.Products;
        quote.InstallBase = pr.InstallBase;
        quote.TransportCost = pr.Transport;
        quote.ShippingCost = pr.Shipping;
        quote.TotalAmount = pr.GrandTotal;

        quote.Status = "Approved";
        quote.ValidUntil = dto.ValidUntil ?? DateTime.UtcNow.AddDays(30);

        await _ctx.SaveChangesAsync();
        return Ok(await BuildDetailDtoAsync(id));
    }

    /* ───── Helper: construir DTO detalle ───── */
    private async Task<QuoteDetailDto> BuildDetailDtoAsync(int quoteId)
    {
        var data = await _ctx.Quotes
            .Include(q => q.QuoteLines)
                .ThenInclude(l => l.Product)
            .FirstAsync(q => q.QuoteId == quoteId);

        // Para líneas en borrador (UnitPrice=0), muestra BasePrice para que el usuario vea precios razonables
        var lines = data.QuoteLines.Select(l =>
        {
            var price = l.UnitPrice > 0 ? l.UnitPrice : l.Product!.BasePrice;
            return new QuoteLineDto(
                l.ProductId,
                l.Product!.Name,
                l.Quantity,
                price,
                l.Quantity * price
            );
        }).ToList();

        // Productos: si ya se guardó TotalProducts úsalo; si no, calcula desde líneas (con fallback BasePrice)
        var products = data.TotalProducts > 0
            ? data.TotalProducts
            : lines.Sum(x => x.LineTotal);

        var install = data.InstallBase;
        var transport = data.TransportCost;
        var shipping = data.ShippingCost;

        // Gran total: si ya está persistido úsalo; si no, compón desde el desglose
        var grand = data.TotalAmount > 0
            ? data.TotalAmount
            : products + install + transport + shipping;

        return new QuoteDetailDto
        {
            Id = data.QuoteId,
            Status = data.Status,
            QuoteDate = data.QuoteDate,
            ValidUntil = data.ValidUntil,

            Fulfillment = data.Fulfillment,
            StateCode = data.StateCode,

            ProductsSubtotal = Math.Round(products, 2),
            InstallBase = Math.Round(install, 2),
            TransportCost = Math.Round(transport, 2),
            ShippingCost = Math.Round(shipping, 2),
            GrandTotal = Math.Round(grand, 2),

            Lines = lines
        };
    }

}
