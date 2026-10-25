using System.Text.Json.Serialization;
using SmartTicket.Contracts.Models;

namespace SmartTicket.Worker.Models;

/// <summary>
/// Represents a persisted ticket analysis result in Azure Cosmos DB.
/// This extends the AI analysis result with metadata for database storage.
/// </summary>
public sealed record TicketAnalysisDocument(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("ticketId")] string TicketId,
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("analysis")] TicketAnalysisResult Analysis,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt,
    [property: JsonPropertyName("partitionKey")] string PartitionKey
)
{
    /// <summary>
    /// Factory method to create a document from a TicketSubmittedEvent and AI analysis.
    /// </summary>
    public static TicketAnalysisDocument FromEventAndAnalysis(
        string eventId,
        string ticketId,
        string customerId,
        string title,
        string description,
        TicketAnalysisResult analysis)
    {
        return new TicketAnalysisDocument(
            Id: eventId, // Use event ID as the document ID
            TicketId: ticketId,
            CustomerId: customerId,
            Title: title,
            Description: description,
            Analysis: analysis,
            CreatedAt: DateTime.UtcNow,
            PartitionKey: customerId // Partition by customer for efficient queries
        );
    }
}
