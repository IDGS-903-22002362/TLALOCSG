namespace TLALOCSG.DTOs.Dashboard;

public record ClientKpis(
    int MyDraftQuotes,
    int MyApprovedQuotes,
    int MyOpenTickets,
    decimal MyOrdersTotalLast30d
);

public class ClientDashboardDto
{
    public ClientKpis Kpis { get; set; } = default!;
    public List<SeriesPointDto> MyOrdersByDay { get; set; } = new();
}