using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TLALOCSG.DTOs.Auth;
using TLALOCSG.Data;
using TLALOCSG.DTOs.Users;
using TLALOCSG.Models;

namespace TLALOCSG.Controllers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TLALOCSG.DTOs.Users;
using TLALOCSG.DTOs.Common;

using TLALOCSG.Data;
using TLALOCSG.Models;
using TLALOCSG.Services.Email;


[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IoTIrrigationDbContext _ctx;
    private readonly IEmailSender _email;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IoTIrrigationDbContext ctx,
        IEmailSender email)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _ctx = ctx;
        _email = email;
    }

    /*──────────────────── LISTA PAGINADA ────────────────────*/
    // GET: /api/users?Page=1&PageSize=10&Role=Client&Active=true&Search=ana
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserWithRolesDto>>> GetUsers([FromQuery] UserQueryDto q)
    {
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize < 1 ? 10 : q.PageSize;
        if (pageSize > 100) pageSize = 100;

        IQueryable<ApplicationUser> query = _userManager.Users.AsNoTracking();

        if (q.Active.HasValue)
            query = query.Where(u => u.IsActive == q.Active.Value);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(u =>
                (u.FullName ?? "").Contains(s) || (u.Email ?? "").Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(q.Role))
        {
            var roleIds = await _roleManager.Roles
                .Where(r => r.Name == q.Role)
                .Select(r => r.Id)
                .ToListAsync();

            query =
                from u in query
                join ur in _ctx.UserRoles on u.Id equals ur.UserId
                where roleIds.Contains(ur.RoleId)
                select u;
        }

        var total = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<UserWithRolesDto>(users.Count);
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            items.Add(new UserWithRolesDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email!,
                Phone = u.PhoneNumber,
                IsActive = u.IsActive,
                Roles = roles
            });
        }

        return Ok(new PagedResult<UserWithRolesDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    /*──────────────────── DETALLE ────────────────────*/
    // GET: /api/users/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<UserWithRolesDto>> GetUser(string id)
    {
        var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return new UserWithRolesDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Phone = user.PhoneNumber,
            IsActive = user.IsActive,
            Roles = roles
        };
    }

    /*──────────────────── ALTA (CREAR + EMAIL) ────────────────────*/
    // POST: /api/users
    [HttpPost]
    public async Task<ActionResult<UserWithRolesDto>> CreateUser([FromBody] UserCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var role = string.IsNullOrWhiteSpace(dto.Role) ? "Client" : dto.Role.Trim();

        if (!await _roleManager.RoleExistsAsync(role))
            return BadRequest($"El rol {role} no existe.");

        if (await _userManager.FindByEmailAsync(dto.Email) is not null)
            return BadRequest("El correo ya está registrado.");

        var tempPassword = string.IsNullOrWhiteSpace(dto.Password) ? NewTempPassword() : dto.Password!;

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, tempPassword);
        if (!result.Succeeded) return StatusCode(500, result.Errors);

        await _userManager.AddToRoleAsync(user, role);

        // Crear fila en Customers/Admins según rol (consistente con AuthController)
        if (role.Equals("Client", StringComparison.OrdinalIgnoreCase))
        {
            _ctx.Customers.Add(new Customer
            {
                CustomerId = user.Id,
                FullName = user.FullName,
                CreatedAt = DateTime.UtcNow
            });
        }
        else if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            _ctx.Admins.Add(new Admin
            {
                AdminId = user.Id,
                FullName = user.FullName,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _ctx.SaveChangesAsync();

        // Email de bienvenida con credenciales
        var html = $@"
        <div style=""font-family:Inter,Arial,sans-serif"">
          <h3>Bienvenido(a) a TLÁLOC</h3>
          <p>Se creó tu cuenta con el rol <b>{role}</b>.</p>
          <p><b>Usuario:</b> {dto.Email}<br><b>Contraseña temporal:</b> {tempPassword}</p>
          <p>Por favor inicia sesión y cambia tu contraseña.</p>
        </div>";

        await _email.SendAsync(dto.Email, "Tu acceso a TLÁLOC", html);

        var outRoles = await _userManager.GetRolesAsync(user);
        var payload = new UserWithRolesDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Phone = user.PhoneNumber,
            IsActive = user.IsActive,
            Roles = outRoles
        };

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, payload);
    }

    /*──────────────────── EDITAR ────────────────────*/
    // PUT: /api/users/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UserUpdateDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;
        user.IsActive = dto.IsActive;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) return StatusCode(500, result.Errors);

        return NoContent();
    }

    /*──────────────────── CAMBIAR ROL ────────────────────*/
    // PATCH: /api/users/{id}/role
    [HttpPatch("{id}/role")]
    public async Task<IActionResult> ChangeRole(string id, [FromBody] string role)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (!await _roleManager.RoleExistsAsync(role))
            return BadRequest($"El rol {role} no existe.");

        var current = await _userManager.GetRolesAsync(user);
        if (current.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, current);

        await _userManager.AddToRoleAsync(user, role);
        return Ok($"Rol actualizado a {role}.");
    }

    /*──────────────────── DESACTIVAR (soft) ────────────────────*/
    // DELETE: /api/users/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DisableUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        user.IsActive = false;
        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    /*──────────────────── REACTIVAR ────────────────────*/
    // PATCH: /api/users/{id}/activate
    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> ActivateUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        user.IsActive = true;
        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    /*──────────────────── RESET PASSWORD ────────────────────*/
    // POST: /api/users/{id}/reset-password
    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var newPass = NewTempPassword();
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPass);
        if (!result.Succeeded) return StatusCode(500, result.Errors);

        var html = $@"
        <div style=""font-family:Inter,Arial,sans-serif"">
          <p>Se restableció tu contraseña.</p>
          <p><b>Nueva temporal:</b> {newPass}</p>
          <p>Inicia sesión y cámbiala desde tu perfil.</p>
        </div>";
        await _email.SendAsync(user.Email!, "Restablecimiento de contraseña", html);

        return NoContent();
    }

    /*──────────────────── LISTA DE ROLES ────────────────────*/
    // GET: /api/users/roles
    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<string>>> GetRoles()
    {
        var roles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        return Ok(roles);
    }

    /*──────────────────── Helpers ────────────────────*/
    private static string NewTempPassword()
    {
        // Cumple reglas por defecto de Identity (mayúscula, minúscula, dígito, símbolo)
        return $"Tmp-{Guid.NewGuid():N}aA1!";
    }
}

