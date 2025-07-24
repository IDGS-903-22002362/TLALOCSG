using System.ComponentModel.DataAnnotations;
namespace TLALOCSG.DTOs.Support;
public class UpdateFaqDto
{
    [Required, StringLength(400)]
    public string Question { get; set; } = null!;
    [Required]
    public string Answer { get; set; } = null!;
}
