namespace TLALOCSG.DTOs.Quotes;

public record QuoteOptionsDto(
    string Fulfillment,      // DevicesOnly | Shipping | Installation
    string? StateCode,       
    int? ManualDistanceKm 
);

public record QuotePricePreviewDto(
    decimal Products, decimal InstallBase, decimal Transport, decimal Shipping, decimal GrandTotal
);
