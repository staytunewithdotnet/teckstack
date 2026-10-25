using System.Text.Json.Serialization;

namespace SmartTicket.Contracts.Models;

public sealed record TicketAnalysisResult(
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("priority")] string Priority,
    [property: JsonPropertyName("sentiment")] string Sentiment,
    [property: JsonPropertyName("suggestedResponse")] string SuggestedResponse
);
