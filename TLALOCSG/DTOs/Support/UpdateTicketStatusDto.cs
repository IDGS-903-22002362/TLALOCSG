using System.ComponentModel.DataAnnotations;
namespace TLALOCSG.DTOs.Support;
public class UpdateTicketStatusDto
{
    [Required]
    public string Status { get; set; } = null!;
}
