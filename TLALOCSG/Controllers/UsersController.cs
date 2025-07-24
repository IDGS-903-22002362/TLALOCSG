using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TLALOCSG.DTOs.Auth;
using TLALOCSG.Data;
using TLALOCSG.DTOs.Users;
using TLALOCSG.Models;

namespace TLALOCSG.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IoTIrrigationDbContext _ctx;

    public UsersController(UserManager<ApplicationUser> userManager,
                           RoleManager<IdentityRole> roleManager,
                           IoTIrrigationDbContext ctx)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _ctx = ctx;
    }

    //GET lista paginada
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserWithRolesDto>>> GetUsers(
        [FromQuery] UserQueryDto q)
    {
        var query = _userManager.Users.AsQueryable();

        if (q.Active.HasValue)
            query = query.Where(u => u.IsActive == q.Active.Value);

        if (!string.IsNullOrEmpty(q.Role))
        {
            var roleIds = await _roleManager.Roles
                                 .Where(r => r.Name == q.Role)
                                 .Select(r => r.Id).ToListAsync();

            query = from u in query
                    join ur in _ctx.UserRoles on u.Id equals ur.UserId
                    where roleIds.Contains(ur.RoleId)
                    select u;
        }

        var skip = (q.Page - 1) * q.PageSize;
        var users = await query.Skip(skip).Take(q.PageSize).ToListAsync();

        var list = new List<UserWithRolesDto>(users.Count);
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            list.Add(new UserWithRolesDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email!,
                IsActive = u.IsActive,
                Roles = roles
            });
        }
        return list;
    }

    //GET detalle
    [HttpGet("{id}")]
    public async Task<ActionResult<UserWithRolesDto>> GetUser(string id)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return new UserWithRolesDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            IsActive = user.IsActive,
            Roles = roles
        };
    }

    //PUT actualizar 
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, UserUpdateDto dto)
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

    //PATCH rol
    [HttpPatch("{id}/role")]
    public async Task<IActionResult> ChangeRole(string id, [FromBody] string role)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (!await _roleManager.RoleExistsAsync(role))
            return BadRequest($"El rol {role} no existe.");

        var current = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, current);
        await _userManager.AddToRoleAsync(user, role);

        return Ok($"Rol actualizado a {role}.");
    }

    //DELETE (soft) 
    [HttpDelete("{id}")]
    public async Task<IActionResult> DisableUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        user.IsActive = false;
        await _userManager.UpdateAsync(user);
        return NoContent();
    }
}
