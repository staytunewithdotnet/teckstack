using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using SmartTicket.Worker.Models;

namespace SmartTicket.Worker.Repositories;

/// <summary>
/// Azure Cosmos DB implementation of ITicketAnalysisRepository.
/// Uses partition key strategy based on CustomerId for efficient queries.
/// </summary>
public sealed class CosmosDbTicketAnalysisRepository : ITicketAnalysisRepository, IAsyncDisposable
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;
    private readonly ILogger<CosmosDbTicketAnalysisRepository> _logger;
    private bool _disposed;

    public CosmosDbTicketAnalysisRepository(
        string connectionString,
        string databaseName,
        string containerName,
        ILogger<CosmosDbTicketAnalysisRepository> logger)
    {
        _logger = logger;

        // Configure Cosmos DB client with retry policies
        var cosmosClientOptions = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            RequestTimeout = TimeSpan.FromSeconds(30),
            OpenTcpConnectionTimeout = TimeSpan.FromSeconds(10),
            MaxRetryAttemptsOnRateLimitedRequests = 3,
            MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(60)
        };

        _cosmosClient = new CosmosClient(connectionString, cosmosClientOptions);

        // Get reference to the container (will be created if it doesn't exist during initialization)
        var database = _cosmosClient.GetDatabase(databaseName);
        _container = database.GetContainer(containerName);

        _logger.LogInformation("Cosmos DB repository initialized. Database: {Database}, Container: {Container}", 
            databaseName, containerName);
    }

    /// <summary>
    /// Initializes the Cosmos DB database and container if they don't exist.
    /// Call this once at application startup.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Create database if it doesn't exist
            await _cosmosClient.CreateDatabaseIfNotExistsAsync(
                _container.Database.Id,
                throughput: 400, // Minimum RU/s for development
                cancellationToken: cancellationToken);

            // Define container properties
            var containerProperties = new ContainerProperties(_container.Id, "/partitionKey")
            {
                PartitionKeyDefinitionVersion = PartitionKeyDefinitionVersion.V2,
                DefaultTimeToLive = -1 // No TTL (documents don't expire)
            };

            // Create container if it doesn't exist
            await _container.Database.CreateContainerIfNotExistsAsync(
                containerProperties,
                throughput: 400,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Cosmos DB database and container initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Cosmos DB database and container");
            throw;
        }
    }

    public async Task SaveAsync(TicketAnalysisDocument document, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _container.CreateItemAsync(
                document,
                new PartitionKey(document.PartitionKey),
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Saved ticket analysis to Cosmos DB. TicketId: {TicketId}, CustomerId: {CustomerId}, Category: {Category}, Priority: {Priority}, RU: {RequestCharge}",
                document.TicketId,
                document.CustomerId,
                document.Analysis.Category,
                document.Analysis.Priority,
                response.RequestCharge);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // Document already exists - update instead
            _logger.LogWarning(
                "Document already exists for TicketId: {TicketId}. Updating instead.",
                document.TicketId);

            var response = await _container.ReplaceItemAsync(
                document,
                document.Id,
                new PartitionKey(document.PartitionKey),
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Updated ticket analysis in Cosmos DB. TicketId: {TicketId}, RU: {RequestCharge}",
                document.TicketId,
                response.RequestCharge);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save ticket analysis to Cosmos DB. TicketId: {TicketId}", document.TicketId);
            throw;
        }
    }

    public async Task<TicketAnalysisDocument?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            // Note: We need the partition key to read by ID. For production, you might want to use a query instead.
            // This is a simplified version - in production, you'd likely query by ID without needing partition key.
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", id);

            using var iterator = _container.GetItemQueryIterator<TicketAnalysisDocument>(query);

            if (await iterator.HasMoreResultsAsync(cancellationToken))
            {
                var results = await iterator.ReadNextAsync(cancellationToken);
                return results.FirstOrDefault();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve ticket analysis by ID. Id: {Id}", id);
            throw;
        }
    }

    public async IAsyncEnumerable<TicketAnalysisDocument> GetByCustomerIdAsync(
        string customerId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.partitionKey = @customerId ORDER BY c.createdAt DESC")
                .WithParameter("@customerId", customerId);

            using var iterator = _container.GetItemQueryIterator<TicketAnalysisDocument>(
                query,
                requestOptions: new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey(customerId)
                });

            while (await iterator.HasMoreResultsAsync(cancellationToken))
            {
                var results = await iterator.ReadNextAsync(cancellationToken);
                foreach (var document in results)
                {
                    yield return document;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve analyses by CustomerId. CustomerId: {CustomerId}", customerId);
            throw;
        }
    }

    public async IAsyncEnumerable<TicketAnalysisDocument> GetByCategoryAsync(
        string category,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.analysis.category = @category ORDER BY c.createdAt DESC")
                .WithParameter("@category", category);

            using var iterator = _container.GetItemQueryIterator<TicketAnalysisDocument>(query);

            while (await iterator.HasMoreResultsAsync(cancellationToken))
            {
                var results = await iterator.ReadNextAsync(cancellationToken);
                foreach (var document in results)
                {
                    yield return document;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve analyses by Category. Category: {Category}", category);
            throw;
        }
    }

    public async IAsyncEnumerable<TicketAnalysisDocument> GetByPriorityAsync(
        string priority,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.analysis.priority = @priority ORDER BY c.createdAt DESC")
                .WithParameter("@priority", priority);

            using var iterator = _container.GetItemQueryIterator<TicketAnalysisDocument>(query);

            while (await iterator.HasMoreResultsAsync(cancellationToken))
            {
                var results = await iterator.ReadNextAsync(cancellationToken);
                foreach (var document in results)
                {
                    yield return document;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve analyses by Priority. Priority: {Priority}", priority);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _cosmosClient?.Dispose();
            _disposed = true;
        }
    }
}
