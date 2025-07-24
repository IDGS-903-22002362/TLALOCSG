namespace TLALOCSG.DTOs.Quotes;
public class ApproveQuoteDto
{
    /// <summary> Fecha límite de vigencia; si null se aplica +30 días. </summary>
    public DateTime? ValidUntil { get; set; }
}
