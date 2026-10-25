using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using SmartTicket.Contracts.Models;

namespace SmartTicket.Worker.Services;

public interface ITicketAnalyzer
{
    Task<TicketAnalysisResult> AnalyzeAsync(string title, string description, CancellationToken cancellationToken);
}

public sealed class OpenAiTicketAnalyzer : ITicketAnalyzer
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenAiTicketAnalyzer> _logger;

    // Define the strict JSON schema for Structured Outputs
    private static readonly string JsonSchema = """
    {
        "type": "object",
        "properties": {
            "summary": { "type": "string", "description": "A concise 1-sentence summary of the issue" },
            "category": { "type": "string", "enum": ["Authentication", "Performance", "Billing", "General", "Deployment"] },
            "priority": { "type": "string", "enum": ["Low", "Medium", "High", "Critical"] },
            "sentiment": { "type": "string", "enum": ["Positive", "Neutral", "Negative", "Frustrated"] },
            "suggestedResponse": { "type": "string", "description": "A professional, empathetic initial response to the user" }
        },
        "required": ["summary", "category", "priority", "sentiment", "suggestedResponse"],
        "additionalProperties": false
    }
    """;

    public OpenAiTicketAnalyzer(string endpoint, string deploymentName, ILogger<OpenAiTicketAnalyzer> logger)
    {
        _logger = logger;

        // DefaultAzureCredential supports both Local Dev (Azure CLI login) and AKS (Workload Identity)
        var credential = new DefaultAzureCredential();
        
        // Create OpenAI client with Azure endpoint and credentials
        var client = new AzureOpenAIClient(new Uri(endpoint), credential);
        
        // Get the ChatClient for the specific deployment
        _chatClient = client.GetChatClient(deploymentName);
    }

    public async Task<TicketAnalysisResult> AnalyzeAsync(string title, string description, CancellationToken cancellationToken)
    {
        var systemPrompt = "You are an expert IT support triage assistant. Analyze the provided ticket title and description. Return ONLY valid JSON matching the provided schema. Do not include markdown formatting or explanatory text.";

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage($"Title: {title}\n\nDescription: {description}")
        };

        // Enforce Structured Output
        var responseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "ticket_analysis",
            jsonSchema: BinaryData.FromString(JsonSchema),
            jsonSchemaIsStrict: true
        );

        var completionOptions = new ChatCompletionOptions
        {
            ResponseFormat = responseFormat,
            Temperature = 0.1f // Low temperature for deterministic, predictable classification
        };

        try
        {
            var result = await _chatClient.CompleteChatAsync(messages, completionOptions, cancellationToken);
            
            var responseContent = result.Value.Content[0].Text;
            
            // Deserialize the guaranteed JSON into our strongly-typed record
            var analysis = JsonSerializer.Deserialize<TicketAnalysisResult>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (analysis is null)
            {
                throw new InvalidOperationException("AI returned a null or unparseable response despite schema enforcement.");
            }

            _logger.LogInformation("AI Analysis successful. Category: {Category}, Priority: {Priority}", analysis.Category, analysis.Priority);
            return analysis;
        }
        catch (ClientResultException ex) when (ex.Status == 429)
        {
            _logger.LogError(ex, "Azure OpenAI rate limit exceeded (HTTP 429). Message will be abandoned for retry.");
            throw; // Let the Service Bus processor handle the retry/abandon logic
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze ticket with Azure OpenAI.");
            throw;
        }
    }
}
