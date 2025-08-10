public class QuoteDetailDto
{
    public int Id { get; set; }
    public string Status { get; set; } = null!;
    public DateTime QuoteDate { get; set; }
    public DateTime? ValidUntil { get; set; }

    public string? Fulfillment { get; set; }
    public string? StateCode { get; set; }

    public decimal ProductsSubtotal { get; set; }
    public decimal InstallBase { get; set; }
    public decimal TransportCost { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal GrandTotal { get; set; }

    public List<QuoteLineDto> Lines { get; set; } = new();
}

public record QuoteLineDto(int ProductId, string Name, decimal Quantity, decimal UnitPrice, decimal LineTotal);
