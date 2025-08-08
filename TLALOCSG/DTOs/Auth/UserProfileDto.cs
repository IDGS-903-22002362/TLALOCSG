namespace TLALOCSG.DTOs.Auth;

public class UserProfileDto
{
    public string Id { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
    public string? PhoneNumber { get; set; }   // 👈 nuevo
}
