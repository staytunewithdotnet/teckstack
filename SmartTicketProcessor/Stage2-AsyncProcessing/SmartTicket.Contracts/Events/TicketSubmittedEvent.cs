namespace SmartTicket.Contracts.Events;

public sealed record TicketSubmittedEvent(
    string TicketId,
    string CustomerId,
    string Title,
    string Description,
    DateTimeOffset SubmittedAt);
