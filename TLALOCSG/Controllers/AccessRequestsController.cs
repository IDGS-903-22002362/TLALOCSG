using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using TLALOCSG.Data;
using TLALOCSG.Models;
using TLALOCSG.Services.Email;

namespace TLALOCSG.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessRequestsController : ControllerBase
{
    private readonly IoTIrrigationDbContext _ctx;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;
    private readonly IEmailSender _email;

    public AccessRequestsController(
        IoTIrrigationDbContext ctx,
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole> roles,
        IEmailSender email)
    {
        _ctx = ctx; _users = users; _roles = roles; _email = email;
    }

    /* 1) Crear solicitud (público) */
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> RequestAccess([FromBody] RequestAccessDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // ya es usuario?
        if (await _users.FindByEmailAsync(dto.Email) is not null)
            return Conflict("Este correo ya tiene una cuenta.");

        // idempotente si hay una pendiente
        var pending = await _ctx.AccessRequests
            .FirstOrDefaultAsync(x => x.Email == dto.Email && x.Status == "Pending");
        if (pending is not null) return Accepted(); // 202

        _ctx.AccessRequests.Add(new AccessRequest
        {
            Email = dto.Email.Trim(),
            FullName = string.IsNullOrWhiteSpace(dto.FullName) ? null : dto.FullName!.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        });
        await _ctx.SaveChangesAsync();
        return Accepted();
    }

    /* 2) Listar (solo Admin) */
    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<AccessRequestDto>>> List([FromQuery] string status = "Pending")
    {
        var data = await _ctx.AccessRequests
            .Where(a => a.Status == status)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new AccessRequestDto
            {
                Id = a.Id,
                Email = a.Email,
                FullName = a.FullName,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Ok(data);
    }

    /* 3) Aprobar (crea usuario Client + correo) */
    [HttpPost("{id:int}/approve")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        var req = await _ctx.AccessRequests.FindAsync(id);
        if (req is null) return NotFound();
        if (req.Status != "Pending") return BadRequest("La solicitud ya fue procesada.");

        const string role = "Client";
        if (!await _roles.RoleExistsAsync(role))
            await _roles.CreateAsync(new IdentityRole(role));

        // Seguridad: si alguien creó el user mientras tanto, evitar duplicado
        if (await _users.FindByEmailAsync(req.Email) is not null)
        {
            req.Status = "Approved";
            req.ProcessedAt = DateTime.UtcNow;
            req.ProcessedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        var tempPass = NewTempPassword();
        var user = new ApplicationUser
        {
            UserName = req.Email,
            Email = req.Email,
            FullName = string.IsNullOrWhiteSpace(req.FullName) ? req.Email : req.FullName!,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var create = await _users.CreateAsync(user, tempPass);
        if (!create.Succeeded) return StatusCode(500, create.Errors);
        await _users.AddToRoleAsync(user, role);

        // fila Customer
        _ctx.Customers.Add(new Customer
        {
            CustomerId = user.Id,
            FullName = user.FullName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // actualizar solicitud
        req.Status = "Approved";
        req.ProcessedAt = DateTime.UtcNow;
        req.ProcessedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _ctx.SaveChangesAsync();

        // correo
        var html = $@"
        <div style='font-family:Inter,Arial'>
         <h3>Acceso aprobado</h3>
         <p>Tu cuenta ha sido creada con rol <b>Client</b>.</p>
         <p><b>Usuario:</b> {user.Email}<br/><b>Contraseña temporal:</b> {tempPass}</p>
         <p>Inicia sesión y cambia tu contraseña.</p>
        </div>";
        await _email.SendAsync(user.Email!, "Tus credenciales de acceso", html);

        return NoContent();
    }

    /* 4) Rechazar */
    [HttpPost("{id:int}/reject")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectReqDto dto)
    {
        var req = await _ctx.AccessRequests.FindAsync(id);
        if (req is null) return NotFound();
        if (req.Status != "Pending") return BadRequest("La solicitud ya fue procesada.");

        req.Status = "Rejected";
        req.Note = dto.Reason;
        req.ProcessedAt = DateTime.UtcNow;
        req.ProcessedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _ctx.SaveChangesAsync();

        if (dto.Notify)
        {
            var html = $@"<div style='font-family:Inter,Arial'>
                <h3>Solicitud rechazada</h3><p>{dto.Reason}</p></div>";
            await _email.SendAsync(req.Email, "Solicitud de acceso rechazada", html);
        }
        return NoContent();
    }

    private static string NewTempPassword() => $"Tmp-{Guid.NewGuid():N}aA1!";
}

/* DTOs */
public class RequestAccessDto
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = null!;
    [StringLength(150)]
    public string? FullName { get; set; }
}
public class AccessRequestDto
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string? FullName { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
public class RejectReqDto
{
    [Required, StringLength(500)]
    public string Reason { get; set; } = "No aprobado";
    public bool Notify { get; set; } = true;
}
