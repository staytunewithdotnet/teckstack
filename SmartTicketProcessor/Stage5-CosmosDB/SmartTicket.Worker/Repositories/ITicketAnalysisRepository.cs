using SmartTicket.Worker.Models;

namespace SmartTicket.Worker.Repositories;

/// <summary>
/// Repository interface for persisting ticket analysis results to Azure Cosmos DB.
/// </summary>
public interface ITicketAnalysisRepository
{
    /// <summary>
    /// Saves a ticket analysis document to Cosmos DB.
    /// </summary>
    Task SaveAsync(TicketAnalysisDocument document, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a ticket analysis by its ID.
    /// </summary>
    Task<TicketAnalysisDocument?> GetByIdAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all analyses for a specific customer.
    /// </summary>
    IAsyncEnumerable<TicketAnalysisDocument> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves analyses filtered by category.
    /// </summary>
    IAsyncEnumerable<TicketAnalysisDocument> GetByCategoryAsync(string category, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves analyses filtered by priority.
    /// </summary>
    IAsyncEnumerable<TicketAnalysisDocument> GetByPriorityAsync(string priority, CancellationToken cancellationToken);
}
