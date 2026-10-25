using Azure.Identity;
using SmartTicket.Infrastructure.Repositories;
using SmartTicket.Worker;
using SmartTicket.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Service Bus Configuration
var serviceBusConnectionString = builder.Configuration.GetValue<string>("ServiceBus:ConnectionString") 
    ?? "Endpoint=sb://127.0.0.1;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
var queueName = builder.Configuration.GetValue<string>("ServiceBus:QueueName") ?? "ticket-processing";

// Azure OpenAI Configuration
var aiEndpoint = builder.Configuration.GetValue<string>("AzureOpenAI:Endpoint") 
    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is required.");
var aiDeploymentName = builder.Configuration.GetValue<string>("AzureOpenAI:DeploymentName") 
    ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is required.");

// Azure Cosmos DB Configuration (Updated to use keyless auth)
var cosmosEndpoint = builder.Configuration.GetValue<string>("CosmosDb:AccountEndpoint") 
    ?? "https://localhost:8081"; // Default local emulator endpoint
var cosmosDatabase = builder.Configuration.GetValue<string>("CosmosDb:DatabaseName") ?? "SmartTicketDb";
var cosmosContainer = builder.Configuration.GetValue<string>("CosmosDb:ContainerName") ?? "Tickets";

// Register the AI Analyzer (uses DefaultAzureCredential internally)
builder.Services.AddSingleton<ITicketAnalyzer>(sp => 
    new OpenAiTicketAnalyzer(aiEndpoint, aiDeploymentName, sp.GetRequiredService<ILogger<OpenAiTicketAnalyzer>>()));

// Register the Cosmos DB Repository with DefaultAzureCredential (keyless auth)
builder.Services.AddSingleton<ITicketRepository>(sp => 
    new CosmosDbTicketRepository(
        cosmosEndpoint,
        cosmosDatabase,
        cosmosContainer,
        sp.GetRequiredService<ILogger<CosmosDbTicketRepository>>()
    ));

// Register the Worker with all dependencies
builder.Services.AddHostedService(sp => 
    new TicketProcessingWorker(
        serviceBusConnectionString, 
        queueName, 
        sp.GetRequiredService<ITicketAnalyzer>(),
        sp.GetRequiredService<ITicketRepository>(), // <-- Updated to use Infrastructure repository
        sp.GetRequiredService<ILogger<TicketProcessingWorker>>()
    ));

var host = builder.Build();
host.Run();
