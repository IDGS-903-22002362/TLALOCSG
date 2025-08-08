using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TLALOCSG.Data;
using TLALOCSG.Models;
using TLALOCSG.Services.Email;
using TLALOCSG.DTOs.Common;   // ← PagedResult<T> compartido
using TLALOCSG.DTOs.Support;  // ← DTOs de soporte (Faq/Ticket/Message)

namespace TLALOCSG.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SupportController : ControllerBase
{
    private static readonly string[] AllowedStatuses = new[] { "Open", "In Progress", "Closed" };

    private readonly IoTIrrigationDbContext _ctx;
    private readonly IEmailSender _email;
    private readonly IConfiguration _cfg;

    public SupportController(IoTIrrigationDbContext ctx, IEmailSender email, IConfiguration cfg)
    {
        _ctx = ctx;
        _email = email;
        _cfg = cfg;
    }

    // ─────────────────────────── FAQ ───────────────────────────

    /* GET  /api/support/faqs  (público) */
    [HttpGet("faqs")]
    [AllowAnonymous]
    public async Task<IEnumerable<FaqDto>> GetFaqs()
      => await _ctx.FAQs.AsNoTracking()
            .OrderBy(f => f.FaqId)
            .Select(f => new FaqDto(f.FaqId, f.Question, f.Answer))
            .ToListAsync();

    /* POST /api/support/faqs  (Admin) */
    [HttpPost("faqs")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FaqDto>> CreateFaq([FromBody] CreateFaqDto dto)
    {
        var faq = new FAQ { Question = dto.Question, Answer = dto.Answer };
        _ctx.FAQs.Add(faq);
        await _ctx.SaveChangesAsync();
        return CreatedAtAction(nameof(GetFaqs), new { id = faq.FaqId },
            new FaqDto(faq.FaqId, faq.Question, faq.Answer));
    }

    /* PUT /api/support/faqs/{id} (Admin) */
    [HttpPut("faqs/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateFaq(int id, [FromBody] UpdateFaqDto dto)
    {
        var faq = await _ctx.FAQs.FindAsync(id);
        if (faq is null) return NotFound();

        faq.Question = dto.Question;
        faq.Answer = dto.Answer;
        await _ctx.SaveChangesAsync();
        return NoContent();
    }

    /* DELETE /api/support/faqs/{id} (Admin) */
    [HttpDelete("faqs/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteFaq(int id)
    {
        var faq = await _ctx.FAQs.FindAsync(id);
        if (faq is null) return NotFound();
        _ctx.FAQs.Remove(faq);
        await _ctx.SaveChangesAsync();
        return NoContent();
    }

    // ─────────────────────── Tickets ──────────────────────────

    /* POST /api/support/tickets  (Client|Admin)  + correo a inbox soporte */
    [HttpPost("tickets")]
    [Authorize(Roles = "Client,Admin")]
    public async Task<ActionResult<TicketDto>> CreateTicket([FromBody] CreateTicketDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var ticket = new Ticket
        {
            CustomerId = userId,
            Subject = dto.Subject,
            Message = dto.Message,
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };

        _ctx.Tickets.Add(ticket);
        await _ctx.SaveChangesAsync();

        // Notificar a inbox de soporte (si está configurado)
        var inbox = _cfg["Support:Inbox"];
        if (!string.IsNullOrWhiteSpace(inbox))
        {
            var html = $@"<div style='font-family:Inter,Arial'>
              <h3>Nuevo ticket #{ticket.TicketId}</h3>
              <p><b>Asunto:</b> {ticket.Subject}</p>
              <p><b>Mensaje:</b> {ticket.Message}</p>
              <p><b>ClienteId:</b> {ticket.CustomerId}</p>
              <p><b>Fecha:</b> {ticket.CreatedAt:yyyy-MM-dd HH:mm} UTC</p>
            </div>";
            await _email.SendAsync(inbox!, $"Nuevo ticket #{ticket.TicketId}", html);
        }

        return CreatedAtAction(nameof(GetTicketById), new { id = ticket.TicketId }, Map(ticket));
    }

    /* GET /api/support/tickets (Admin) ?status=&q=&page=&pageSize= */
    [HttpGet("tickets")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResult<TicketDto>>> GetTickets(
        [FromQuery] string? status, [FromQuery] string? q,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize;
        if (pageSize > 100) pageSize = 100;

        var query = _ctx.Tickets.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(t => t.Subject.Contains(q) || t.Message.Contains(q));

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => Map(t))
            .ToListAsync();

        return Ok(new PagedResult<TicketDto> { Items = items, Total = total, Page = page, PageSize = pageSize });
    }

    /* GET /api/support/tickets/my (Client|Admin) */
    [HttpGet("tickets/my")]
    [Authorize(Roles = "Client,Admin")]
    public async Task<IEnumerable<TicketDto>> GetMyTickets([FromQuery] string? status, [FromQuery] string? q)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var query = _ctx.Tickets.AsNoTracking().Where(t => t.CustomerId == userId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(t => t.Subject.Contains(q) || t.Message.Contains(q));

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => Map(t))
            .ToListAsync();
    }

    /* GET /api/support/tickets/{id}  (Admin o dueño) */
    [HttpGet("tickets/{id:int}")]
    public async Task<ActionResult<TicketDto>> GetTicketById(int id)
    {
        var ticket = await _ctx.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.TicketId == id);
        if (ticket is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && ticket.CustomerId != userId) return Forbid();

        return Map(ticket);
    }

    /* PUT /api/support/tickets/{id}/status  (Admin) + correo a cliente */
    [HttpPut("tickets/{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateTicketStatus(int id, [FromBody] UpdateTicketStatusDto dto)
    {
        if (!AllowedStatuses.Contains(dto.Status)) return BadRequest("Status inválido.");

        var ticket = await _ctx.Tickets.FindAsync(id);
        if (ticket is null) return NotFound();

        ticket.Status = dto.Status;
        ticket.ClosedAt = dto.Status == "Closed" ? DateTime.UtcNow : null;

        await _ctx.SaveChangesAsync();

        // Correo al cliente
        var user = await _ctx.Users.FindAsync(ticket.CustomerId);
        if (user?.Email is not null)
        {
            var html = $@"<div style='font-family:Inter,Arial'>
              <h3>Actualización de ticket #{ticket.TicketId}</h3>
              <p>Estado: <b>{ticket.Status}</b></p>
              <p>Asunto: {ticket.Subject}</p>
            </div>";
            await _email.SendAsync(user.Email, $"Ticket #{ticket.TicketId} actualizado", html);
        }

        return NoContent();
    }

    // ──────────────── Mensajes del ticket ─────────────────

    /* GET /api/support/tickets/{id}/messages  (Admin o dueño) */
    [HttpGet("tickets/{id:int}/messages")]
    public async Task<ActionResult<IEnumerable<TicketMessageDto>>> GetMessages(int id)
    {
        var ticket = await _ctx.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.TicketId == id);
        if (ticket is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && ticket.CustomerId != userId) return Forbid();

        var msgs = await _ctx.TicketMessages
            .Where(m => m.TicketId == id)
            .OrderBy(m => m.CreatedAt)
            .Join(_ctx.Users, m => m.SenderId, u => u.Id,
                (m, u) => new TicketMessageDto(m.MessageId, m.TicketId, m.SenderId, m.Body, m.CreatedAt, u.FullName))
            .ToListAsync();

        return Ok(msgs);
    }
    [HttpPost("tickets/{id:int}/to-faq")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FaqDto>> PublishTicketAsFaq(int id, [FromBody] PublishFaqDto? dto)
    {
        var ticket = await _ctx.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.TicketId == id);
        if (ticket is null) return NotFound();

            var lastAdminMsg = await _ctx.TicketMessages
            .Where(m => m.TicketId == id)
            .OrderByDescending(m => m.CreatedAt)
            .Join(_ctx.Users, m => m.SenderId, u => u.Id, (m, u) => new { m, u })
            .Where(x => _ctx.UserRoles.Any(ur => ur.UserId == x.u.Id && _ctx.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin")))
            .Select(x => x.m.Body)
            .FirstOrDefaultAsync();

        var question = (dto?.Question ?? ticket.Subject)?.Trim();
        var answer = (dto?.Answer ?? lastAdminMsg ?? ticket.Message)?.Trim();

        if (string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(answer))
            return BadRequest("Pregunta y respuesta no pueden estar vacías.");

        var faq = new FAQ { Question = question!, Answer = answer! };
        _ctx.FAQs.Add(faq);
        await _ctx.SaveChangesAsync();

        return CreatedAtAction(nameof(GetFaqs), new { id = faq.FaqId }, new FaqDto(faq.FaqId, faq.Question, faq.Answer));
    }

    /* POST /api/support/tickets/{id}/messages  (Admin o dueño) + correos */
    [HttpPost("tickets/{id:int}/messages")]
    public async Task<ActionResult<TicketMessageDto>> PostMessage(int id, [FromBody] CreateMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Body)) return BadRequest("Mensaje vacío.");

        var ticket = await _ctx.Tickets.FindAsync(id);
        if (ticket is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && ticket.CustomerId != userId) return Forbid();

        if (ticket.Status == "Closed")
            return BadRequest("No se pueden agregar mensajes a un ticket cerrado.");

        var msg = new TicketMessage
        {
            TicketId = id,
            SenderId = userId,
            Body = dto.Body.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _ctx.TicketMessages.Add(msg);

        // Si un Admin responde por primera vez y está "Open" => "In Progress"
        if (isAdmin && ticket.Status == "Open")
            ticket.Status = "In Progress";

        await _ctx.SaveChangesAsync();

        // Notificaciones:
        if (isAdmin)
        {
            // Admin → correo al cliente
            var client = await _ctx.Users.FindAsync(ticket.CustomerId);
            if (client?.Email is not null)
            {
                var html = $@"<div style='font-family:Inter,Arial'>
                  <h3>Respuesta a tu ticket #{ticket.TicketId}</h3>
                  <p><b>Asunto:</b> {ticket.Subject}</p>
                  <p>{msg.Body}</p>
                </div>";
                await _email.SendAsync(client.Email, $"Respuesta ticket #{ticket.TicketId}", html);
            }
        }
        else
        {
            // Cliente → correo al inbox de soporte
            var inbox = _cfg["Support:Inbox"];
            if (!string.IsNullOrWhiteSpace(inbox))
            {
                var html = $@"<div style='font-family:Inter,Arial'>
                  <h3>Nuevo mensaje en ticket #{ticket.TicketId}</h3>
                  <p><b>Asunto:</b> {ticket.Subject}</p>
                  <p>{msg.Body}</p>
                  <p><b>ClienteId:</b> {ticket.CustomerId}</p>
                </div>";
                await _email.SendAsync(inbox!, $"Ticket #{ticket.TicketId}: nuevo mensaje", html);
            }
        }

        var sender = await _ctx.Users.FindAsync(userId);
        var dtoOut = new TicketMessageDto(
            msg.MessageId, msg.TicketId, msg.SenderId, msg.Body, msg.CreatedAt, sender?.FullName);

        return CreatedAtAction(nameof(GetMessages), new { id }, dtoOut);
    }

    // ─────────────── Helpers ───────────────
    private static TicketDto Map(Ticket t) => new(
        t.TicketId, t.Subject, t.Message, t.Status, t.CreatedAt, t.ClosedAt, t.CustomerId);
}
