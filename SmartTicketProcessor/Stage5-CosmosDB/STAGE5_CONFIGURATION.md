# Stage 5 Configuration Guide

## Overview

This guide provides step-by-step instructions to configure and run Stage 5: Azure Cosmos DB Integration.

## Prerequisites

1. **Docker Desktop** - Installed and running
2. **Azure CLI** - Logged in (`az login`)
3. **.NET 10 SDK** - Installed
4. **Completed Stages 1-4** - Especially Stage 4 for AI integration

## Step 1: Start Emulators

Open a terminal in the `Stage5-CosmosDB` directory:

```bash
cd Stage5-CosmosDB
docker-compose up -d
```

Wait ~30 seconds for both emulators to initialize. Verify they're running:

```bash
docker ps
# Should see: sb-emulator-stage5 and cosmos-emulator-stage5
```

## Step 2: Configure Azure OpenAI

The Worker needs Azure OpenAI credentials. Use User Secrets (recommended) or environment variables.

### Option A: User Secrets (Recommended)

```bash
cd SmartTicket.Worker
dotnet user-secrets init
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini"
```

### Option B: Environment Variables

```bash
# Windows PowerShell
$env:AzureOpenAI__Endpoint="https://<your-resource>.openai.azure.com/"
$env:AzureOpenAI__DeploymentName="gpt-4o-mini"

# Linux/Mac
export AzureOpenAI__Endpoint="https://<your-resource>.openai.azure.com/"
export AzureOpenAI__DeploymentName="gpt-4o-mini"
```

**Note:** Ensure your Azure AD user has the "Cognitive Services OpenAI User" role assigned to the Azure OpenAI resource.

## Step 3: Configure Cosmos DB (Optional)

Cosmos DB configuration is already set in `appsettings.json` for local development:

```json
{
  "CosmosDb": {
    "AccountEndpoint": "https://localhost:8081",
    "DatabaseName": "SmartTicketDb",
    "ContainerName": "Tickets"
  }
}
```

If you want to use different values, override via User Secrets:

```bash
dotnet user-secrets set "CosmosDb:AccountEndpoint" "https://localhost:8081"
dotnet user-secrets set "CosmosDb:DatabaseName" "SmartTicketDb"
dotnet user-secrets set "CosmosDb:ContainerName" "Tickets"
```

## Step 4: Build and Run

### Terminal 1: API (Optional)

If you want to create tickets via HTTP API:

```bash
cd ../Stage2-AsyncProcessing/SmartTicket.Api
dotnet run
# API will be available at http://localhost:5232
```

### Terminal 2: Worker

```bash
cd SmartTicket.Worker
dotnet build
dotnet run
```

You should see logs like:

```
info: SmartTicket.Worker.TicketProcessingWorker[0]
      Starting Service Bus Processor...
```

## Step 5: Test the Flow

### Option A: Via API (if running)

```bash
curl -X POST http://localhost:5232/api/tickets \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "CUST-AI",
    "title": "Login Issue",
    "description": "Cannot access my account after password reset."
  }'
```

### Option B: Direct Message

Use Azure Service Bus Explorer or write a test script to send a `TicketSubmittedEvent` directly to the `ticket-processing` queue.

## Step 6: Verify Results

### Check Worker Logs

You should see:

```
info: SmartTicket.Worker.TicketProcessingWorker[0]
      Processing message for TicketId: TKT-XXXX

info: SmartTicket.Worker.Services.OpenAiTicketAnalyzer[0]
      AI Analysis successful. Category: Authentication, Priority: High

info: SmartTicket.Infrastructure.Repositories.CosmosDbTicketRepository[0]
      Successfully updated TicketId: TKT-XXXX

info: SmartTicket.Worker.TicketProcessingWorker[0]
      Successfully completed message for TicketId: TKT-XXXX
```

### Check Cosmos DB Data Explorer

1. Open browser: https://localhost:8081/_explorer/index.html
2. Accept the security warning (self-signed certificate)
3. Navigate to: `SmartTicketDb` → `Tickets`
4. Click on a document to see:

```json
{
  "id": "TKT-XXXX",
  "customerId": "CUST-AI",
  "title": "Login Issue",
  "description": "Cannot access...",
  "status": "Completed",
  "createdAt": "2026-10-25T...",
  "processedAt": "2026-10-25T...",
  "analysis": {
    "summary": "User unable to login...",
    "category": "Authentication",
    "priority": "High",
    "sentiment": "Frustrated",
    "suggestedResponse": "We apologize..."
  },
  "_etag": "\"0x8D...\""
}
```

## Troubleshooting

### Issue: Cosmos DB Emulator Certificate Error

**Symptom:** `HttpRequestException: The SSL connection could not be established`

**Solution:** The code includes a `#if DEBUG` block to accept the self-signed certificate automatically. If you're still seeing errors:

1. Ensure you're running in Debug mode
2. Or manually trust the emulator certificate (Windows only):

```powershell
# Run as Administrator
cd "C:\Program Files\Azure Cosmos DB Emulator"
.\Microsoft.Azure.Cosmos.Emulator.exe /GenCert=localhost
```

### Issue: Database Not Found

**Symptom:** `CosmosException: Database 'SmartTicketDb' does not exist`

**Solution:** The repository auto-creates the database and container on startup. If it fails:

1. Check that the emulator is running: `docker ps | findstr cosmos`
2. Manually create via Data Explorer at https://localhost:8081/_explorer/index.html

### Issue: ETag Mismatch on Every Update

**Symptom:** Always getting 412 Precondition Failed

**Solution:** Ensure you're passing the correct ETag from the read operation:

```csharp
var ticket = await _repository.GetByIdAsync(id, partitionKey, cancellationToken);
var success = await _repository.TryUpdateAnalysisAsync(
    id, 
    partitionKey, 
    ticket.ETag,  // ← Must use the ETag from the read
    analysis, 
    cancellationToken);
```

### Issue: Azure OpenAI Authentication Error

**Symptom:** `ClientResultException: Status code 401, Unauthorized`

**Solution:**

1. Ensure you're logged into Azure CLI: `az login`
2. Set the correct subscription: `az account set --subscription "<your-subscription-id>"`
3. Verify your user has the "Cognitive Services OpenAI User" role on the Azure OpenAI resource

## Production Deployment

When deploying to production:

### 1. Replace Emulators with Azure Services

Update `appsettings.json` or User Secrets:

```json
{
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=<key>"
  },
  "CosmosDb": {
    "AccountEndpoint": "https://<account>.documents.azure.com:443/",
    "DatabaseName": "SmartTicketDb",
    "ContainerName": "Tickets"
  }
}
```

### 2. Use Managed Identity

In AKS or Azure App Service, the `DefaultAzureCredential` will automatically use Workload Identity or Managed Identity. No code changes needed!

### 3. Configure Throughput

Set appropriate RU/s for your Cosmos DB container based on expected load:

- **Low traffic:** 400-1000 RU/s
- **Medium traffic:** 1000-5000 RU/s
- **High traffic:** 5000+ RU/s or autoscale

### 4. Add Indexing Policy

For efficient queries by category/priority, add an indexing policy:

```json
{
  "indexingPolicy": {
    "includedPaths": [
      { "path": "/analysis/category" },
      { "path": "/analysis/priority" },
      { "path": "/status" }
    ]
  }
}
```

## Next Steps

After completing Stage 5:

1. ✅ Add TTL (Time-To-Live) for automatic cleanup of old tickets
2. ✅ Implement change feed for real-time analytics
3. ✅ Add monitoring and alerting (Application Insights)
4. ✅ Load test with realistic traffic patterns
5. ✅ Deploy to Azure (AKS or App Service)

---

**Configuration Complete!** You're ready to run Stage 5 with Azure Cosmos DB integration. 🎉
