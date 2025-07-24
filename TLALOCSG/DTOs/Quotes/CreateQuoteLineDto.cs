using System.ComponentModel.DataAnnotations;

namespace TLALOCSG.DTOs.Quotes;
public class CreateQuoteLineDto
{
    [Required] public int ProductId { get; set; }
    [Required, Range(1, int.MaxValue)] public int Quantity { get; set; }
}
