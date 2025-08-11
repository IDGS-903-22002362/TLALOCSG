namespace TLALOCSG.DTOs.Dashboard;

public record KpiDto(
    decimal Sales,            // total $ en el rango
    int Orders,               // # órdenes en el rango
    int PendingQuotes,        // # cotizaciones Draft
    int OpenTickets,          // # tickets != Closed
    int LowStock              // # materiales con stock < threshold
);

public record SeriesPointDto(DateTime Date, decimal Value);

public record TopProductDto(int ProductId, string Name, decimal Units, decimal Total);

public record LowStockDto(int MaterialId, string Name, decimal OnHand);

public class AdminDashboardDto
{
    public KpiDto Kpis { get; set; } = default!;
    public List<SeriesPointDto> SalesByDay { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<LowStockDto> LowStock { get; set; } = new();
}