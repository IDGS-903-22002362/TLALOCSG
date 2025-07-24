using System.ComponentModel.DataAnnotations;

namespace TLALOCSG.DTOs.Auth;

public class RefreshTokenDto
{
    [Required] public string AccessToken { get; set; } = null!;
    [Required] public string RefreshToken { get; set; } = null!;
}
