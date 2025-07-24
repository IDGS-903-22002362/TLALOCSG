using System.ComponentModel.DataAnnotations;

namespace TLALOCSG.DTOs.Auth;

public class RegisterDto
{
    [Required, StringLength(150)]
    public string FullName { get; set; } = null!;

    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required, MinLength(6)]
    public string Password { get; set; } = null!;

    /// <summary>Rol requerido: "Client" (default) o "Admin"</summary>
    public string Role { get; set; } = "Client";
}
