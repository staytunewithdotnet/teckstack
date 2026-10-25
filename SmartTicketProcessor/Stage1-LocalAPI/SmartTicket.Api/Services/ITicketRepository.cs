using SmartTicket.Api.Models;

namespace SmartTicket.Api.Services;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<Ticket> CreateAsync(Ticket ticket, CancellationToken cancellationToken);
}
