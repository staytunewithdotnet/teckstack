using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using SmartTicket.Contracts.Models;

namespace SmartTicket.Infrastructure.Repositories;

public sealed class CosmosDbTicketRepository : ITicketRepository
{
    private readonly Container _container;
    private readonly ILogger<CosmosDbTicketRepository> _logger;

    public CosmosDbTicketRepository(string accountEndpoint, string databaseName, string containerName, ILogger<CosmosDbTicketRepository> logger)
    {
        _logger = logger;

        // DefaultAzureCredential supports Local Dev (Azure CLI) and AKS (Workload Identity)
        var credential = new DefaultAzureCredential();
        
        var clientOptions = new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase },
            // Enable direct TCP mode for lowest latency in production
            ConnectionMode = ConnectionMode.Direct 
        };

#if DEBUG
        // For local development with Cosmos DB Emulator (self-signed certificate)
        clientOptions.HttpClientFactory = () => new HttpClient(new HttpClientHandler 
        { 
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator 
        });
#endif

        var client = new CosmosClient(accountEndpoint, credential, clientOptions);
        
        // Auto-create database and container if they don't exist (for local dev)
        client.CreateDatabaseIfNotExistsAsync(databaseName).Wait();
        client.GetDatabase(databaseName).CreateContainerIfNotExistsAsync(containerName, "/customerId").Wait();
        
        _container = client.GetContainer(databaseName, containerName);
    }

    public async Task<Ticket?> GetByIdAsync(string id, string partitionKey, CancellationToken cancellationToken)
    {
        try
        {
            // POINT READ: The most efficient and cheapest way to get a single item by ID and Partition Key
            var response = await _container.ReadItemAsync<Ticket>(
                id, 
                new PartitionKey(partitionKey), 
                cancellationToken: cancellationToken);
            
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Ticket> CreateAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        var response = await _container.CreateItemAsync(ticket, new PartitionKey(ticket.CustomerId), cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task<bool> TryUpdateAnalysisAsync(string id, string partitionKey, string expectedETag, TicketAnalysisResult analysis, CancellationToken cancellationToken)
    {
        try
        {
            // We must read the current item to update it, preserving other fields
            var currentTicket = await GetByIdAsync(id, partitionKey, cancellationToken);
            if (currentTicket is null) return false;

            var updatedTicket = currentTicket with 
            { 
                Analysis = analysis, 
                Status = TicketStatus.Completed,
                ProcessedAt = DateTimeOffset.UtcNow
            };

            var requestOptions = new ItemRequestOptions { IfMatchEtag = expectedETag };

            var response = await _container.ReplaceItemAsync(
                updatedTicket, 
                id, 
                new PartitionKey(partitionKey), 
                requestOptions, 
                cancellationToken);

            // Update the returned ticket with the new ETag
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
        {
            _logger.LogWarning("Optimistic concurrency conflict for TicketId: {Id}. Message was likely redelivered.", id);
            return false; // ETag mismatch: another process updated this ticket
        }
    }
}
