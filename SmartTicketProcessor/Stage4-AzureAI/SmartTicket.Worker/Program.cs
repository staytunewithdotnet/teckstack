using Azure.Identity;
using SmartTicket.Worker;
using SmartTicket.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

var serviceBusConnectionString = builder.Configuration.GetValue<string>("ServiceBus:ConnectionString") 
    ?? "Endpoint=sb://127.0.0.1;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
var queueName = builder.Configuration.GetValue<string>("ServiceBus:QueueName") ?? "ticket-processing";

// Azure OpenAI Configuration
// In production, these come from App Configuration / Key Vault. 
// Locally, they can be set via User Secrets or Environment Variables.
var aiEndpoint = builder.Configuration.GetValue<string>("AzureOpenAI:Endpoint") 
    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is required.");
var aiDeploymentName = builder.Configuration.GetValue<string>("AzureOpenAI:DeploymentName") 
    ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is required.");

// Register the AI Analyzer (uses DefaultAzureCredential internally)
builder.Services.AddSingleton<ITicketAnalyzer>(sp => 
    new OpenAiTicketAnalyzer(aiEndpoint, aiDeploymentName, sp.GetRequiredService<ILogger<OpenAiTicketAnalyzer>>()));

builder.Services.AddHostedService(sp => 
    new TicketProcessingWorker(serviceBusConnectionString, queueName, sp.GetRequiredService<ITicketAnalyzer>(), sp.GetRequiredService<ILogger<TicketProcessingWorker>>()));

var host = builder.Build();
host.Run();
