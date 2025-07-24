using System.ComponentModel.DataAnnotations;
namespace TLALOCSG.DTOs.Users;

public class UserUpdateDto
{
    [Required, StringLength(150)]
    public string FullName { get; set; } = null!;

    [Phone]
    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;
}
