namespace TLALOCSG.DTOs.Quotes;
public record QuoteLineDto(int ProductId, string ProductName, int Quantity,
                           decimal UnitPrice, decimal LineTotal);
