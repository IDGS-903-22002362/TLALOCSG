namespace TLALOCSG.DTOs.Auth;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public int ExpiresIn { get; set; }          // en segundos
    public IEnumerable<string>? Roles { get; set; }
}
