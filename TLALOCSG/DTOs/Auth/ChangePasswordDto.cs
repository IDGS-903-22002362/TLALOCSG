using System.ComponentModel.DataAnnotations;

namespace TLALOCSG.DTOs.Auth
{

    public class ChangePasswordDto
    {
        [Required] public string CurrentPassword { get; set; } = null!;
        [Required] public string NewPassword { get; set; } = null!; // Identity valida la política
    }
}
