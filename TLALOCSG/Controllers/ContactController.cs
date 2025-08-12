// Controllers/ContactController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;
using TLALOCSG.Data;
using TLALOCSG.DTOs.Common;
using TLALOCSG.DTOs.Support;
using TLALOCSG.Models;
using TLALOCSG.Services.Email;

namespace TLALOCSG.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly IoTIrrigationDbContext _ctx;
    private readonly IEmailSender _email;
    private readonly IConfiguration _cfg;

    public ContactController(IoTIrrigationDbContext ctx, IEmailSender email, IConfiguration cfg)
    {
        _ctx = ctx; _email = email; _cfg = cfg;
    }

    // Info pública (mostrar en la página /contact)
    [HttpGet("info")]
    [AllowAnonymous]
    public ActionResult<ContactInfoDto> GetInfo()
    {
        var s = _cfg.GetSection("Company");
        return new ContactInfoDto
        {
            Company = s["Name"] ?? "",
            Email = s["Email"] ?? "",
            Phone = s["Phone"] ?? "",
            Address = s["Address"] ?? "",
            Hours = s["Hours"] ?? "",
            Facebook = s["Facebook"],
            Instagram = s["Instagram"],
            X = s["X"],
            MapsUrl = s["MapsUrl"]
        };
    }

    // Recibe formulario de contacto
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Send([FromBody] CreateContactDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

        var entity = new ContactMessage
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
            Topic = dto.Topic,
            Message = dto.Message,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            Status = "New"
        };
        _ctx.ContactMessages.Add(entity);
        await _ctx.SaveChangesAsync();

        // correo a admins
        var to = _cfg["Company:NotifyTo"];
        if (!string.IsNullOrWhiteSpace(to))
        {
            var body = $@"
              <h3>Nuevo mensaje de contacto #{entity.ContactMessageId}</h3>
              <p><b>Nombre:</b> {WebUtility.HtmlEncode(entity.FullName)}</p>
              <p><b>Correo:</b> {WebUtility.HtmlEncode(entity.Email)}  <b>Tel:</b> {WebUtility.HtmlEncode(entity.Phone)}</p>
              <p><b>Tema:</b> {WebUtility.HtmlEncode(entity.Topic)}</p>
              <p><b>Mensaje:</b><br/>{WebUtility.HtmlEncode(entity.Message).Replace("\n", "<br/>")}</p>";
            await _email.SendAsync(to, $"Contacto #{entity.ContactMessageId} – {entity.Topic}", body);
        }

        // opcional: acuse al cliente
        await _email.SendAsync(entity.Email, "Recibimos tu mensaje", "<p>Gracias por contactarnos. En breve te respondemos.</p>");

        // Si desea Ticket y está logueado: crea un Ticket (reutilizamos SupportController/Tickets)
        if (dto.AsTicket && userId != null)
        {
            _ctx.Tickets.Add(new Ticket
            {
                CustomerId = userId,
                Subject = $"Contacto: {dto.Topic}",
                Message = dto.Message,
                Status = "Open"
            });
            await _ctx.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetById), new { id = entity.ContactMessageId }, new { id = entity.ContactMessageId });
    }

    // Admin: ver detalle
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ContactMessageDto>> GetById(int id)
    {
        var m = await _ctx.ContactMessages.AsNoTracking().FirstOrDefaultAsync(x => x.ContactMessageId == id);
        if (m is null) return NotFound();
        return new ContactMessageDto
        {
            Id = m.ContactMessageId,
            FullName = m.FullName,
            Email = m.Email,
            Phone = m.Phone,
            Topic = m.Topic,
            Message = m.Message,
            CreatedAt = m.CreatedAt,
            Status = m.Status
        };
    }

    // Admin: listado con paginación y filtros
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResult<ContactMessageDto>>> List([FromQuery] PagedQueryDto q, [FromQuery] string? status)
    {
        q.Page = q.Page <= 0 ? 1 : q.Page; q.PageSize = q.PageSize <= 0 ? 10 : q.PageSize;

        var query = _ctx.ContactMessages.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q.Q))
            query = query.Where(x => x.FullName.Contains(q.Q) || x.Email.Contains(q.Q) || x.Message.Contains(q.Q) || x.Topic.Contains(q.Q));
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
                               .Skip((q.Page - 1) * q.PageSize)
                               .Take(q.PageSize)
                               .Select(m => new ContactMessageDto
                               {
                                   Id = m.ContactMessageId,
                                   FullName = m.FullName,
                                   Email = m.Email,
                                   Phone = m.Phone,
                                   Topic = m.Topic,
                                   Message = m.Message,
                                   CreatedAt = m.CreatedAt,
                                   Status = m.Status
                               })
                               .ToListAsync();

        return new PagedResult<ContactMessageDto> { Total = total, Page = q.Page, PageSize = q.PageSize, Items = items };
    }

    // Admin: actualizar status
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] string status)
    {
        var valid = new[] { "New", "InProgress", "Closed" };
        if (!valid.Contains(status)) return BadRequest("Estado inválido.");

        var m = await _ctx.ContactMessages.FindAsync(id);
        if (m is null) return NotFound();
        m.Status = status;
        await _ctx.SaveChangesAsync();
        return NoContent();
    }
}
