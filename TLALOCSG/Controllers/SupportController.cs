using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TLALOCSG.Data;
using TLALOCSG.DTOs.Support;
using TLALOCSG.Models;
using System.Security.Claims;

namespace TLALOCSG.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SupportController : ControllerBase
{
    private readonly IoTIrrigationDbContext _ctx;
    private readonly UserManager<ApplicationUser> _userManager;

    public SupportController(IoTIrrigationDbContext ctx,
                             UserManager<ApplicationUser> userManager)
    {
        _ctx = ctx;
        _userManager = userManager;
    }

    //SECTION  FAQ  

    /* GET  /faqs  (público) */
    [HttpGet("faqs")]
    [AllowAnonymous]
    public async Task<IEnumerable<FaqDto>> GetFaqs()
        => await _ctx.FAQs
           .OrderBy(f => f.FaqId)
           .Select(f => new FaqDto(f.FaqId, f.Question, f.Answer))
           .ToListAsync();

    /* POST /faqs  (Admin) */
    [HttpPost("faqs")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FaqDto>> CreateFaq(CreateFaqDto dto)
    {
        var faq = new FAQ { Question = dto.Question, Answer = dto.Answer };
        _ctx.FAQs.Add(faq);
        await _ctx.SaveChangesAsync();
        return CreatedAtAction(nameof(GetFaqs), new { id = faq.FaqId },
               new FaqDto(faq.FaqId, faq.Question, faq.Answer));
    }

    /* PUT /faqs/{id} */
    [HttpPut("faqs/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateFaq(int id, UpdateFaqDto dto)
    {
        var faq = await _ctx.FAQs.FindAsync(id);
        if (faq is null) return NotFound();

        faq.Question = dto.Question;
        faq.Answer = dto.Answer;
        await _ctx.SaveChangesAsync();
        return NoContent();
    }

    /* DELETE /faqs/{id} */
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

    //SECTION  TICKETS  

    /* POST /tickets  (Cliente) */
    [HttpPost("tickets")]
    [Authorize(Roles = "Client,Admin")]
    public async Task<ActionResult<TicketDto>> CreateTicket(CreateTicketDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var ticket = new Ticket
        {
            CustomerId = userId,
            Subject = dto.Subject,
            Message = dto.Message,
            Status = "Open"
        };
        _ctx.Tickets.Add(ticket);
        await _ctx.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTicketById), new { id = ticket.TicketId },
               Map(ticket));
    }

    /* GET /tickets  (Admin)  ?status=Open */
    [HttpGet("tickets")]
    [Authorize(Roles = "Admin")]
    public async Task<IEnumerable<TicketDto>> GetTickets([FromQuery] string? status)
    {
        var query = _ctx.Tickets.AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status);
        return await query
     .OrderByDescending(t => t.CreatedAt)
     .Select(t => new TicketDto(
         t.TicketId,
         t.Subject,
         t.Message,
         t.Status,
         t.CreatedAt,
         t.ClosedAt,
         t.CustomerId))
     .ToListAsync();
    }

    /* GET /tickets/{id}  (Admin o dueño) */
    [HttpGet("tickets/{id:int}")]
    [Authorize]
    public async Task<ActionResult<TicketDto>> GetTicketById(int id)
    {
        var ticket = await _ctx.Tickets.FindAsync(id);
        if (ticket is null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && ticket.CustomerId != userId)
            return Forbid();

        return Map(ticket);
    }

    /* PUT /tickets/{id}/status  (Admin) */
    [HttpPut("tickets/{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateTicketStatus(int id, UpdateTicketStatusDto dto)
    {
        var ticket = await _ctx.Tickets.FindAsync(id);
        if (ticket is null) return NotFound();

        if (!new[] { "Open", "In Progress", "Closed" }.Contains(dto.Status))
            return BadRequest("Status inválido.");

        ticket.Status = dto.Status;
        ticket.ClosedAt = dto.Status == "Closed" ? DateTime.UtcNow : null;
        await _ctx.SaveChangesAsync();
        return NoContent();
    }

    //Helper mapping 
    private static TicketDto Map(Ticket t) => new(
        t.TicketId, t.Subject, t.Message, t.Status,
        t.CreatedAt, t.ClosedAt, t.CustomerId);
}
