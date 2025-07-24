namespace TLALOCSG.DTOs.Support;
public record TicketDto(
    int Id,
    string Subject,
    string Message,
    string Status,
    DateTime CreatedAt,
    DateTime? ClosedAt,
    string CustomerId);
