# Stage 4: Azure AI Integration

## 🎯 Learning Objectives

In this stage, you'll learn:
- ✅ Azure OpenAI SDK v2.x with ChatClient API
- ✅ Structured Outputs (guaranteed JSON schema compliance)
- ✅ DefaultAzureCredential for zero-secret authentication
- ✅ CancellationToken propagation for graceful shutdown
- ✅ Resilience policies for transient failures
- ✅ Low temperature for deterministic classification

## 📁 Project Structure

```
Stage4-AzureAI/
├── SmartTicket.Contracts/            # Shared contracts
│   ├── Events/
│   │   └── TicketSubmittedEvent.cs
│   └── Models/
│       └── TicketAnalysisResult.cs   # AI output schema
├── SmartTicket.Worker/               # Worker with AI integration
│   ├── Services/
│   │   └── OpenAiTicketAnalyzer.cs   # AI service
│   ├── Worker.cs                     # Updated with AI call
│   └── Program.cs                    # AI configuration
└── SmartTicketProcessor.Stage4.sln
```

## 🏗️ Architecture

```
Service Bus Queue
         ↓
Worker receives message
         ↓
Deserialize TicketSubmittedEvent
         ↓
Call Azure OpenAI (Structured Output)
         ↓
Receive guaranteed JSON matching schema
         ↓
Log analysis results
         ↓
Complete message
```

## 🔑 Prerequisites

### Required Before Starting

1. **Azure Subscription** with Azure OpenAI access
2. **Azure OpenAI Resource** created
3. **Model Deployment** (e.g., gpt-4o-mini)
4. **Azure CLI** installed and logged in
5. **Completed Stages 1 & 2**

### Step 1: Create Azure OpenAI Resource

```bash
# Login to Azure
az login
az account set --subscription "<your-subscription-id>"

# Create resource group
az group create --name rg-smartticket --location eastus

# Create Azure OpenAI resource
az cognitiveservices account create \
  --name openai-smartticket \
  --resource-group rg-smartticket \
  --kind OpenAI \
  --sku S0 \
  --location eastus

# Deploy gpt-4o-mini model
az cognitiveservices account deployment create \
  --name openai-smartticket \
  --resource-group rg-smartticket \
  --deployment-name gpt-4o-mini \
  --model-name gpt-4o-mini \
  --model-version "2024-07-18" \
  --model-format OpenAI \
  --sku-capacity 100 \
  --sku-name "Standard"
```

### Step 2: Assign Role to Your User

```bash
# Get your user ID
$USER_ID = az ad signed-in-user show --query id -o tsv

# Assign Cognitive Services OpenAI User role
az role assignment create \
  --role "Cognitive Services OpenAI User" \
  --assignee $USER_ID \
  --scope /subscriptions/<sub-id>/resourceGroups/rg-smartticket/providers/Microsoft.CognitiveServices/accounts/openai-smartticket
```

### Step 3: Configure Local Development

```bash
cd Stage4-AzureAI/SmartTicket.Worker

# Initialize user secrets
dotnet user-secrets init

# Set Azure OpenAI endpoint and deployment
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://openai-smartticket.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini"
```

## 🚀 Getting Started

### Step 1: Start Service Bus Emulator

```bash
cd ../Stage2-AsyncProcessing  # Use Stage 2's docker-compose
docker-compose up -d
```

### Step 2: Run the Worker

```bash
cd Stage4-AzureAI/SmartTicket.Worker
dotnet run
```

### Step 3: Submit a Test Ticket

Use Swagger from Stage 1 or Stage 2 API:

```bash
curl -X POST http://localhost:5232/api/tickets \
  -H "Content-Type: application/json" \
  -d "{\"customerId\":\"CUST-AI\",\"title\":\"App crashes on startup\",\"description\":\"After Windows update, app crashes immediately. All users blocked.\"}"
```

### Expected Worker Output

```
info: Starting Service Bus Processor...
info: Processing message for TicketId: TKT-A1B2C3D4

(wait 1-3 seconds for Azure OpenAI call)

info: AI Analysis successful. Category: Deployment, Priority: Critical
info: AI Analysis Complete for TKT-A1B2C3D4: Deployment / Critical
info: Successfully completed message for TicketId: TKT-A1B2C3D4
```

## 🎓 Key Concepts to Learn

### 1. Structured Outputs (Interview Gold ⭐)

[`SmartTicket.Worker/Services/OpenAiTicketAnalyzer.cs`](SmartTicket.Worker/Services/OpenAiTicketAnalyzer.cs)

```csharp
// Define strict JSON schema
private static readonly string JsonSchema = """
{
    "type": "object",
    "properties": {
        "category": { "type": "string", "enum": ["Authentication", "Performance", ...] },
        "priority": { "type": "string", "enum": ["Low", "Medium", "High", "Critical"] }
    },
    "required": ["category", "priority"],
    "additionalProperties": false
}
""";

// Enforce schema compliance
var responseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
    jsonSchemaFormatName: "ticket_analysis",
    jsonSchema: BinaryData.FromString(JsonSchema),
    jsonSchemaIsStrict: true  // Guarantees valid JSON!
);
```

**Why is this amazing?**
- ✅ No prompt engineering needed
- ✅ Guaranteed valid JSON
- ✅ No regex parsing or retry logic
- ✅ Eliminates hallucinated keys

### 2. DefaultAzureCredential (Security Best Practice 🔐)

```csharp
// No API keys in code!
var credential = new DefaultAzureCredential();
var client = new AzureOpenAIClient(new Uri(endpoint), credential);
_chatClient = client.GetChatClient(deploymentName);
```

**Works everywhere:**
- **Local Dev:** Uses `az login` credentials
- **AKS:** Uses Workload Identity
- **Azure VM:** Uses Managed Identity
- **Zero secrets** in code or config files

### 3. Low Temperature (0.1)

```csharp
var completionOptions = new ChatCompletionOptions
{
    ResponseFormat = responseFormat,
    Temperature = 0.1f  // Deterministic results
};
```

**Why low temperature?**
- For classification, creativity is a bug
- Ensures consistent categorization
- Predictable, repeatable results

### 4. CancellationToken Propagation

```csharp
public async Task<TicketAnalysisResult> AnalyzeAsync(
    string title, 
    string description, 
    CancellationToken cancellationToken)  // <-- Passed through
{
    var result = await _chatClient.CompleteChatAsync(
        messages, 
        options, 
        cancellationToken);  // <-- Aborts if cancelled
}
```

**Benefits:**
- Graceful shutdown support
- Prevents orphaned HTTP calls
- Kubernetes-friendly

### 5. AI Output Schema

[`SmartTicket.Contracts/Models/TicketAnalysisResult.cs`](SmartTicket.Contracts/Models/TicketAnalysisResult.cs)

```csharp
public sealed record TicketAnalysisResult(
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("priority")] string Priority,
    [property: JsonPropertyName("sentiment")] string Sentiment,
    [property: JsonPropertyName("suggestedResponse")] string SuggestedResponse
);
```

**Guaranteed by Azure OpenAI:**
- All fields present
- Correct types
- Valid enum values
- No extra properties

## 💡 Exercises

1. **Change Categories**: Modify the enum in JsonSchema
2. **Adjust Temperature**: Try 0.0 vs 0.5 vs 1.0
3. **Add Logging**: Log token usage and latency
4. **Test Rate Limits**: Send multiple tickets rapidly
5. **Handle Errors**: Add custom error responses

## 🔍 Code Walkthrough

### Step 1: Study the AI Analyzer

Open [`SmartTicket.Worker/Services/OpenAiTicketAnalyzer.cs`](SmartTicket.Worker/Services/OpenAiTicketAnalyzer.cs)
- Notice the JSON schema definition
- See ChatResponseFormat.CreateJsonSchemaFormat
- Observe DefaultAzureCredential usage
- Check CancellationToken propagation

### Step 2: Review the Worker Integration

Open [`SmartTicket.Worker/Worker.cs`](SmartTicket.Worker/Worker.cs)
- See ITicketAnalyzer injection
- Observe AI call replacing Task.Delay
- Check error handling (AbandonMessageAsync)

### Step 3: Examine Configuration

Open [`SmartTicket.Worker/Program.cs`](SmartTicket.Worker/Program.cs)
- Azure OpenAI endpoint validation
- Deployment name validation
- DI registration

## 🐛 Troubleshooting

### "AzureOpenAI:Endpoint is required"

```bash
# Verify secrets are set
dotnet user-secrets list

# Should show both Endpoint and DeploymentName
```

### "DefaultAzureCredential failed"

```bash
# Re-login to Azure
az logout
az login

# Verify correct subscription
az account show

# Check role assignment
az role assignment list --assignee $(az ad signed-in-user show --query id -o tsv)
```

### "HTTP 429 Too Many Requests"

- SDK auto-retries 3 times (2-second delay)
- Reduce MaxConcurrentCalls to 1-2 in Worker.cs
- Request higher quota in Azure Portal

### "Access denied"

```bash
# Assign role
az role assignment create \
  --role "Cognitive Services OpenAI User" \
  --assignee $(az ad signed-in-user show --query id -o tsv) \
  --scope /subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.CognitiveServices/accounts/<resource>
```

## 🎯 What Makes This Production-Ready?

✅ **Structured Outputs** - Guaranteed JSON schema compliance  
✅ **DefaultAzureCredential** - Zero secrets in code  
✅ **Low Temperature** - Deterministic classification  
✅ **CancellationToken** - Graceful shutdown  
✅ **Resilience Policy** - Automatic retry on 429 errors  
✅ **Interface Design** - Easy to mock for tests  

## 🎓 Interview Gold: Stage 4 Talking Points

### 1. Structured Outputs over Prompt Engineering

> "Instead of begging the LLM to 'please return JSON', I used ChatResponseFormat.CreateJsonSchemaFormat. This forces the model to output strictly valid JSON matching my C# record schema."

### 2. DefaultAzureCredential

> "I did not hardcode an API key. The analyzer uses DefaultAzureCredential. Locally it uses my Azure CLI login. In AKS, it seamlessly transitions to Workload Identity."

### 3. Low Temperature

> "For classification tasks, creativity is a bug. I set temperature to 0.1 to ensure deterministic, predictable categorization."

### 4. CancellationToken Propagation

> "I pass CancellationToken from Service Bus all the way to CompleteChatAsync. If the pod terminates, the HTTP call aborts immediately."

## 📊 Performance Characteristics

| Metric | Value | Notes |
|--------|-------|-------|
| AI Call Duration | 1-3 seconds | Depends on model |
| Temperature | 0.1 | Low for consistency |
| Concurrent Calls | 3 | Avoid rate limits |
| Retry Policy | 3 attempts | On transient failures |
| Retry Delay | 2 seconds | Between attempts |

## 🎯 What's Next?

After mastering Stage 4, you can:
- **Stage 5:** Save AI results to Cosmos DB
- **Stage 6:** Deploy to Azure Kubernetes Service (AKS)
- **Monitoring:** Add Application Insights
- **Cost Optimization:** Track token usage

## 📚 Additional Resources

- [Azure OpenAI Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [Structured Outputs](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/structured-outputs)
- [DefaultAzureCredential](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential)
- [ChatClient API](https://learn.microsoft.com/en-us/dotnet/api/azure.ai.openai.chatclient)

---

**Status:** ✅ Ready to Learn (requires Azure setup)  
**Complexity:** Advanced  
**Prerequisites:** Stages 1 & 2 complete, Azure OpenAI resource  
**Estimated Time:** 4-5 hours (including Azure setup)
