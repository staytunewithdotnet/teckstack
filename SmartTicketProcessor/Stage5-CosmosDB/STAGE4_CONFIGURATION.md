# Stage 4: Azure AI Integration - Configuration Guide

## 🔑 Azure OpenAI Setup

### Prerequisites

1. **Azure Subscription** with access to Azure OpenAI
2. **Azure OpenAI Resource** created in your subscription
3. **Model Deployment** (e.g., gpt-4o-mini, gpt-4o)
4. **Azure CLI** installed and logged in

### Step 1: Create Azure OpenAI Resource

```bash
# Login to Azure
az login

# Set your subscription
az account set --subscription "<your-subscription-id>"

# Create resource group (if not exists)
az group create --name rg-smartticket --location eastus

# Create Azure OpenAI resource
az cognitiveservices account create \
  --name openai-smartticket \
  --resource-group rg-smartticket \
  --kind OpenAI \
  --sku S0 \
  --location eastus

# Deploy a model (e.g., gpt-4o-mini)
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

You need the **"Cognitive Services OpenAI User"** role:

```bash
# Get your user object ID
USER_ID=$(az ad signed-in-user show --query id -o tsv)

# Assign role
az role assignment create \
  --role "Cognitive Services OpenAI User" \
  --assignee $USER_ID \
  --scope /subscriptions/<your-subscription-id>/resourceGroups/rg-smartticket/providers/Microsoft.CognitiveServices/accounts/openai-smartticket
```

### Step 3: Configure Local Development

#### Option A: User Secrets (Recommended for Local Dev)

```bash
cd SmartTicket.Worker
dotnet user-secrets init

# Set your Azure OpenAI endpoint and deployment name
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://openai-smartticket.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini"
```

The secrets are stored in: `%APPDATA%\Microsoft\UserSecrets\<user-secrets-id>\secrets.json`

#### Option B: Environment Variables

```powershell
# PowerShell
$env:AzureOpenAI__Endpoint = "https://openai-smartticket.openai.azure.com/"
$env:AzureOpenAI__DeploymentName = "gpt-4o-mini"
```

```bash
# Bash
export AzureOpenAI__Endpoint="https://openai-smartticket.openai.azure.com/"
export AzureOpenAI__DeploymentName="gpt-4o-mini"
```

#### Option C: appsettings.Development.json

Create `SmartTicket.Worker/appsettings.Development.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://openai-smartticket.openai.azure.com/",
    "DeploymentName": "gpt-4o-mini"
  }
}
```

⚠️ **Never commit this file to source control!** Add it to `.gitignore`.

### Step 4: Verify Azure CLI Login

```bash
# Check current login
az account show

# If not logged in
az login

# Verify you can get a token for Cognitive Services
az account get-access-token --resource https://cognitiveservices.azure.com/
```

## 🧪 Testing Stage 4

### 1. Ensure Service Bus Emulator is Running

```bash
cd SmartTicketProcessor
docker-compose up -d
```

### 2. Run the API

```bash
cd SmartTicket.Api
dotnet run
```

### 3. Run the Worker

```bash
cd SmartTicket.Worker
dotnet run
```

### 4. Submit a Ticket

**Via Swagger:**
- Open http://localhost:5232/swagger
- POST `/api/tickets` with:

```json
{
  "customerId": "CUST-AI-TEST",
  "title": "Application crashes on startup after Windows update",
  "description": "After installing the latest Windows update yesterday, our application crashes immediately on startup with an exception. This is blocking all users in the production environment and needs urgent attention."
}
```

**Via cURL:**
```bash
curl -X POST http://localhost:5232/api/tickets \
  -H "Content-Type: application/json" \
  -d "{\"customerId\":\"CUST-AI-TEST\",\"title\":\"Application crashes on startup\",\"description\":\"After Windows update, app crashes immediately. Blocking all production users.\"}"
```

### 5. Expected Output

**API Terminal:**
```
info: SmartTicket.Api.Services.ServiceBusMessagePublisher[0]
      Published message for TicketId: TKT-A1B2C3D4
```

**Worker Terminal:**
```
info: SmartTicket.Worker.TicketProcessingWorker[0]
      Processing message for TicketId: TKT-A1B2C3D4

info: SmartTicket.Worker.Services.OpenAiTicketAnalyzer[0]
      AI Analysis successful. Category: Deployment, Priority: Critical

info: SmartTicket.Worker.TicketProcessingWorker[0]
      AI Analysis Complete for TKT-A1B2C3D4: Deployment / Critical

info: SmartTicket.Worker.TicketProcessingWorker[0]
      Successfully completed message for TicketId: TKT-A1B2C3D4
```

## 🔍 Troubleshooting

### Error: "AzureOpenAI:Endpoint is required"

**Cause:** Configuration not loaded  
**Solution:**
```bash
# Verify secrets are set
dotnet user-secrets list

# Or check environment variables
echo $env:AzureOpenAI__Endpoint  # PowerShell
echo $AzureOpenAI__Endpoint      # Bash
```

### Error: "DefaultAzureCredential failed to acquire a token"

**Cause:** Not logged into Azure CLI or missing permissions  
**Solution:**
```bash
# Re-login
az logout
az login

# Verify role assignment
az role assignment list \
  --assignee $(az ad signed-in-user show --query id -o tsv) \
  --scope /subscriptions/<sub-id>/resourceGroups/rg-smartticket/providers/Microsoft.CognitiveServices/accounts/openai-smartticket
```

### Error: "HTTP 429 Too Many Requests"

**Cause:** Rate limit exceeded  
**Solution:**
- The SDK automatically retries (3 times with 2-second delay)
- Reduce `MaxConcurrentCalls` in Worker to 1-2
- Request higher quota in Azure Portal

### Error: "The specified deployment does not exist"

**Cause:** Wrong deployment name  
**Solution:**
```bash
# List deployments
az cognitiveservices account deployment list \
  --name openai-smartticket \
  --resource-group rg-smartticket \
  --output table
```

### Error: "Access denied"

**Cause:** Missing "Cognitive Services OpenAI User" role  
**Solution:** See Step 2 above to assign the role

## 🎯 Key Features Implemented

### ✅ Structured Outputs

Instead of fragile prompt engineering, we use **ChatResponseFormat.CreateJsonSchemaFormat**:

```csharp
var responseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
    jsonSchemaFormatName: "ticket_analysis",
    jsonSchema: BinaryData.FromString(JsonSchema),
    jsonSchemaIsStrict: true  // Guarantees schema compliance
);
```

**Benefits:**
- ✅ Guaranteed valid JSON matching our C# record
- ✅ No regex parsing or retry logic needed
- ✅ Eliminates hallucinated keys or malformed responses
- ✅ Compile-time type safety

### ✅ DefaultAzureCredential

No API keys in code or config files:

```csharp
var credential = new DefaultAzureCredential();
_chatClient = new ChatClient(deploymentName, endpoint, credential);
```

**Works seamlessly across environments:**
- **Local Dev:** Uses Azure CLI login (`az login`)
- **AKS:** Uses Workload Identity
- **Azure VM:** Uses Managed Identity
- **Zero secrets** stored in containers or manifests

### ✅ Low Temperature (0.1)

For classification tasks, creativity is a bug:

```csharp
var completionOptions = new ChatCompletionOptions
{
    ResponseFormat = responseFormat,
    Temperature = 0.1f  // Deterministic, predictable results
};
```

### ✅ CancellationToken Propagation

Graceful shutdown support:

```csharp
var analysis = await _analyzer.AnalyzeAsync(title, description, args.CancellationToken);
```

If the pod is terminating, the HTTP call is immediately aborted.

### ✅ Resilience Policy

Automatic retry on transient failures:

```csharp
var clientOptions = new OpenAIClientOptions
{
    RetryPolicy = new ClientRetryPolicy(maxRetries: 3, delay: TimeSpan.FromSeconds(2))
};
```

Handles HTTP 429 (rate limits) and network issues automatically.

## 📊 Performance Characteristics

| Metric | Value | Notes |
|--------|-------|-------|
| AI Call Duration | 1-3 seconds | Depends on model and input length |
| Temperature | 0.1 | Low for deterministic classification |
| Max Retries | 3 | On transient failures (429, timeouts) |
| Retry Delay | 2 seconds | Between retry attempts |
| Concurrent Calls | 3 | Reduced to avoid rate limits |

## 🔐 Security Best Practices

✅ **No API Keys in Code** - Uses DefaultAzureCredential  
✅ **No Secrets in Config Files** - User Secrets for local dev only  
✅ **Role-Based Access** - Cognitive Services OpenAI User role  
✅ **Managed Identity Ready** - Seamless transition to AKS  
✅ **CancellationToken** - Prevents orphaned background tasks  

## 🎓 Interview Gold: Stage 4 Talking Points

When discussing this implementation, emphasize these production-grade decisions:

### 1. Structured Outputs over Prompt Engineering

> "Instead of begging the LLM to 'please return JSON' in the prompt, I used the `ChatResponseFormat.CreateJsonSchemaFormat` feature. This forces the model to output strictly valid JSON matching my C# record schema. This eliminates the need for fragile retry logic or regex parsing when the model hallucinates."

### 2. Low Temperature (0.1)

> "For classification and triage tasks, creativity is a bug, not a feature. I set the temperature to 0.1 to ensure deterministic, predictable, and consistent categorization."

### 3. DefaultAzureCredential

> "I did not hardcode an API key or connection string. The OpenAiTicketAnalyzer uses DefaultAzureCredential. Locally, this uses my Azure CLI login. In AKS, this seamlessly transitions to Workload Identity, ensuring zero secrets are stored in the container or Kubernetes manifests."

### 4. CancellationToken Propagation

> "I pass the CancellationToken from the Service Bus ProcessMessageEventArgs all the way down to the CompleteChatAsync call. If the AKS pod is terminating, the HTTP call to Azure OpenAI is immediately aborted, preventing orphaned background tasks and ensuring graceful shutdown."

### 5. Resilience Policy

> "I configured ClientRetryPolicy on the OpenAIClientOptions. If Azure OpenAI returns a transient HTTP 429 (Rate Limit), the SDK automatically waits and retries up to 3 times before throwing an exception, which the Service Bus processor then catches and abandons for a later retry."

## 📚 Next Steps

- **Stage 5:** Save AI analysis results to Cosmos DB
- **Stage 6:** Deploy to Azure Kubernetes Service (AKS)
- **Monitoring:** Add Application Insights for AI call telemetry
- **Cost Optimization:** Implement token usage tracking and budget alerts

---

**Status:** ✅ Stage 4 Implementation Complete  
**Configuration:** Requires Azure OpenAI resource and CLI login  
**Ready for Testing:** Once Azure credentials are configured
