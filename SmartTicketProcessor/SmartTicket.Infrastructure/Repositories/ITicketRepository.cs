using SmartTicket.Contracts.Models;

namespace SmartTicket.Infrastructure.Repositories;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(string id, string partitionKey, CancellationToken cancellationToken);
    Task<Ticket> CreateAsync(Ticket ticket, CancellationToken cancellationToken);
    
    /// <summary>
    /// Updates the ticket with AI analysis. Returns true if successful, false if ETag mismatch (concurrency conflict).
    /// </summary>
    Task<bool> TryUpdateAnalysisAsync(string id, string partitionKey, string expectedETag, TicketAnalysisResult analysis, CancellationToken cancellationToken);
}
