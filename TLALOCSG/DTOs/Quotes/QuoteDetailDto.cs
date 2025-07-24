namespace TLALOCSG.DTOs.Quotes;
public class QuoteDetailDto
{
    public int Id { get; set; }
    public string Status { get; set; } = null!;
    public DateTime QuoteDate { get; set; }
    public DateTime? ValidUntil { get; set; }
    public decimal TotalAmount { get; set; }
    public IEnumerable<QuoteLineDto> Lines { get; set; } = Enumerable.Empty<QuoteLineDto>();
}
