using System.ComponentModel.DataAnnotations;

namespace TLALOCSG.DTOs.Quotes;
public class CreateQuoteDto
{
    [Required, MinLength(1)]
    public List<CreateQuoteLineDto> Lines { get; set; } = new();
}
