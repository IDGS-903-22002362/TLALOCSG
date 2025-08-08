using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TLALOCSG.Data;
using TLALOCSG.DTOs.Auth;
using TLALOCSG.DTOs.Users;

using TLALOCSG.Models;

namespace TLALOCSG.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _config;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IoTIrrigationDbContext _ctx;          
    private const string REFRESH_PROVIDER = "TLALOCSG";
    private const string REFRESH_NAME = "RefreshToken";

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IoTIrrigationDbContext ctx,            
        IConfiguration config)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _ctx = ctx;                         
        _config = config;
    }
    //Obtener todos los usuarios + roles 
    [HttpGet("users")]
    //[Authorize(Roles = "Admin")]
    public async Task<IEnumerable<UserWithRolesDto>> GetAllUsers()
    {
        // Traemos usuarios; luego consultamos roles uno a uno
        var users = await _userManager.Users.ToListAsync();

        var result = new List<UserWithRolesDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserWithRolesDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                Roles = roles
            });
        }
        return result;
    }
    //Registro
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var role = string.IsNullOrWhiteSpace(dto.Role) ? "Client" : dto.Role;

        if (await _userManager.FindByEmailAsync(dto.Email) is not null)
            return BadRequest("El correo ya está registrado.");

        // Si el rol no existe lo creamos (opcional)
        if (!await _roleManager.RoleExistsAsync(role))
            await _roleManager.CreateAsync(new IdentityRole(role));

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded) return StatusCode(500, result.Errors);

        await _userManager.AddToRoleAsync(user, role);

        //Registro en Customers o Admins según rol 
        if (role.Equals("Client", StringComparison.OrdinalIgnoreCase))
        {
            _ctx.Customers.Add(new Customer
            {
                CustomerId = user.Id,
                FullName = dto.FullName,
                CreatedAt = DateTime.UtcNow
            });
        }
        else if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            _ctx.Admins.Add(new Admin
            {
                AdminId = user.Id,
                FullName = dto.FullName,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _ctx.SaveChangesAsync();   // guarda filas adicionales
        return Ok($"Usuario registrado con rol {role}.");
    }

    //Asignar / Cambiar rol 
    [HttpPatch("roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user is null) return NotFound("Usuario no encontrado.");

        // Si el rol no existe lo creamos (opcional: puedes exigir que exista)
        if (!await _roleManager.RoleExistsAsync(dto.Role))
            await _roleManager.CreateAsync(new IdentityRole(dto.Role));

        // Quitar roles actuales (excepto si quieres conservarlos)
        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded) return StatusCode(500, removeResult.Errors);

        // Asignar nuevo rol
        var addResult = await _userManager.AddToRoleAsync(user, dto.Role);
        if (!addResult.Succeeded) return StatusCode(500, addResult.Errors);

        return Ok($"Rol del usuario {user.Email} cambiado a {dto.Role}");
    }

    //Login 
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized("Credenciales inválidas.");

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = BuildJwt(user, roles);
        var refreshToken = Guid.NewGuid().ToString();

        await _userManager.RemoveAuthenticationTokenAsync(user, REFRESH_PROVIDER, REFRESH_NAME);
        await _userManager.SetAuthenticationTokenAsync(user, REFRESH_PROVIDER, REFRESH_NAME, refreshToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = int.Parse(_config["JWTSetting:expireInMinutes"]!) * 60,
            Roles = roles
        };
    }

    //Refresh 
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshTokenDto dto)
    {
        var principal = GetPrincipalFromExpiredToken(dto.AccessToken);
        if (principal is null) return BadRequest("Token inválido.");

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var storedRefresh = await _userManager.GetAuthenticationTokenAsync(user, REFRESH_PROVIDER, REFRESH_NAME);
        if (storedRefresh != dto.RefreshToken)
            return Unauthorized("Refresh token no coincide.");

        var roles = await _userManager.GetRolesAsync(user);
        var newAccess = BuildJwt(user, roles);
        var newRefreshTok = Guid.NewGuid().ToString();

        await _userManager.SetAuthenticationTokenAsync(user, REFRESH_PROVIDER, REFRESH_NAME, newRefreshTok);

        return new AuthResponseDto
        {
            AccessToken = newAccess,
            RefreshToken = newRefreshTok,
            ExpiresIn = int.Parse(_config["JWTSetting:expireInMinutes"]!) * 60,
            Roles = roles
        };
    }

    // GET /api/auth/me
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);
        var roles = await _userManager.GetRolesAsync(user);

        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Roles = roles,
            PhoneNumber = user.PhoneNumber    // 👈
        };
    }

    // PUT /api/auth/me  (editar nombre/teléfono)
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;

        var res = await _userManager.UpdateAsync(user);
        if (!res.Succeeded) return StatusCode(500, res.Errors);

        return NoContent();
    }

    // POST /api/auth/change-password
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var res = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!res.Succeeded) return BadRequest(res.Errors); // devuelve errores de política/credencial

        return NoContent();
    }


    //Logout 
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);
        await _userManager.RemoveAuthenticationTokenAsync(user, REFRESH_PROVIDER, REFRESH_NAME);
        await _signInManager.SignOutAsync();
        return Ok("Sesión cerrada.");
    }


    //Helpers 
    private string BuildJwt(ApplicationUser user, IList<string> roles)
    {
        var jwtCfg = _config.GetSection("JWTSetting");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtCfg["securityKey"]!));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: jwtCfg["ValidIssuer"],
            audience: jwtCfg["ValidAudience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtCfg["expireInMinutes"]!)),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var jwtCfg = _config.GetSection("JWTSetting");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtCfg["securityKey"]!));

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = false,
                    ValidIssuer = jwtCfg["ValidIssuer"],
                    ValidAudience = jwtCfg["ValidAudience"],
                    IssuerSigningKey = key
                },
                out var securityToken);

            if (securityToken is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                return null;

            return principal;
        }
        catch
        {
            return null;
        }

    }


}

