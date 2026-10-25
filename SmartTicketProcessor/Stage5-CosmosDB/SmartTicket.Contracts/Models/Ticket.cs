using System.Text.Json.Serialization;

namespace SmartTicket.Contracts.Models;

public sealed record Ticket(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("customerId")] string CustomerId, // Partition Key
    string Title,
    string Description,
    TicketStatus Status,
    DateTimeOffset CreatedAt,
    TicketAnalysisResult? Analysis = null,
    DateTimeOffset? ProcessedAt = null)
{
    // Cosmos DB automatically manages this. We expose it for optimistic concurrency.
    [JsonPropertyName("_etag")]
    public string ETag { get; init; } = string.Empty;
}

public enum TicketStatus
{
    Submitted,
    Processing,
    Completed,
    Failed
}
