using SmartTicket.Api.Models;
using System.Collections.Concurrent;

namespace SmartTicket.Api.Services;

public sealed class InMemoryTicketRepository : ITicketRepository
{
    private readonly ConcurrentDictionary<string, Ticket> _store = new();

    public Task<Ticket?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id, out var ticket);
        return Task.FromResult(ticket);
    }

    public Task<Ticket> CreateAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        _store[ticket.Id] = ticket;
        return Task.FromResult(ticket);
    }
}
