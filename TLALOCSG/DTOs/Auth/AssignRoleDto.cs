using System.ComponentModel.DataAnnotations;

namespace TLALOCSG.DTOs.Auth;

public class AssignRoleDto
{
    [Required] public string UserId { get; set; } = null!;
    [Required] public string Role { get; set; } = null!;
}
