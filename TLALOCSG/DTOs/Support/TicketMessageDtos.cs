namespace TLALOCSG.DTOs.Support;

public record TicketMessageDto(int Id, int TicketId, string SenderId, string Body, DateTime CreatedAt, string? SenderName);
public record CreateMessageDto(string Body);