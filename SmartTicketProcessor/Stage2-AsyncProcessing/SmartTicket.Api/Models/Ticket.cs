namespace SmartTicket.Api.Models;

public sealed record Ticket(
    string Id,
    string CustomerId,
    string Title,
    string Description,
    TicketStatus Status,
    DateTimeOffset CreatedAt);

public enum TicketStatus
{
    Submitted,
    Processing,
    Completed,
    Failed
}
