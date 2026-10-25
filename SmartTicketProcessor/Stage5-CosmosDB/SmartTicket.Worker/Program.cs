using Azure.Identity;
using SmartTicket.Worker;
using SmartTicket.Worker.Repositories;
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

// Azure Cosmos DB Configuration
var cosmosConnectionString = builder.Configuration.GetValue<string>("CosmosDB:ConnectionString") 
    ?? throw new InvalidOperationException("CosmosDB:ConnectionString is required.");
var cosmosDatabaseName = builder.Configuration.GetValue<string>("CosmosDB:DatabaseName") ?? "SmartTicketDB";
var cosmosContainerName = builder.Configuration.GetValue<string>("CosmosDB:ContainerName") ?? "TicketAnalyses";

// Register the AI Analyzer (uses DefaultAzureCredential internally)
builder.Services.AddSingleton<ITicketAnalyzer>(sp => 
    new OpenAiTicketAnalyzer(aiEndpoint, aiDeploymentName, sp.GetRequiredService<ILogger<OpenAiTicketAnalyzer>>()));

// Register the Cosmos DB Repository
builder.Services.AddSingleton<ITicketAnalysisRepository>(sp => 
    new CosmosDbTicketAnalysisRepository(
        cosmosConnectionString,
        cosmosDatabaseName,
        cosmosContainerName,
        sp.GetRequiredService<ILogger<CosmosDbTicketAnalysisRepository>>()
    ));

// Register the Worker with all dependencies
builder.Services.AddHostedService(sp => 
    new TicketProcessingWorker(
        serviceBusConnectionString, 
        queueName, 
        sp.GetRequiredService<ITicketAnalyzer>(),
        sp.GetRequiredService<ITicketAnalysisRepository>(), // <-- Injected
        sp.GetRequiredService<ILogger<TicketProcessingWorker>>()
    ));

var host = builder.Build();
host.Run();
