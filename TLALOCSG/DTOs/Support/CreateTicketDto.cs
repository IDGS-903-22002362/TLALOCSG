using System.ComponentModel.DataAnnotations;
namespace TLALOCSG.DTOs.Support;
public class CreateTicketDto
{
    [Required, StringLength(200)]
    public string Subject { get; set; } = null!;
    [Required]
    public string Message { get; set; } = null!;
}
