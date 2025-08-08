using System.ComponentModel.DataAnnotations;

namespace TLALOCSG.DTOs.Auth
{
    public class UpdateProfileDto
    {
        [Required, StringLength(150)]
        public string FullName { get; set; } = null!;

        [Phone, StringLength(30)]
        public string? PhoneNumber { get; set; }
    }
}
