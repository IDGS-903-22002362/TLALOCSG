namespace TLALOCSG.DTOs.Auth;

public class UserWithRolesDto
{
    public string Id { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool IsActive { get; set; }
    public IEnumerable<string>? Roles { get; set; }
}
