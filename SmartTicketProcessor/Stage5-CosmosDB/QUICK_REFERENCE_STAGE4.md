# Quick Reference - Stage 4 Commands

## 🚀 Setup Azure OpenAI

### 1. Login to Azure
```bash
az login
az account set --subscription "<your-subscription-id>"
```

### 2. Configure User Secrets
```bash
cd SmartTicket.Worker
dotnet user-secrets init
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini"
```

### 3. Verify Role Assignment
```bash
# Get your user ID
USER_ID=$(az ad signed-in-user show --query id -o tsv)

# Check role
az role assignment list \
  --assignee $USER_ID \
  --scope /subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.CognitiveServices/accounts/<resource-name>
```

## 🧪 Test the Flow

### Start Everything
```bash
# Terminal 1: Service Bus Emulator
docker-compose up -d

# Terminal 2: API
cd SmartTicket.Api
dotnet run

# Terminal 3: Worker
cd SmartTicket.Worker
dotnet run
```

### Submit Test Ticket
```bash
curl -X POST http://localhost:5232/api/tickets \
  -H "Content-Type: application/json" \
  -d "{\"customerId\":\"CUST-AI\",\"title\":\"App crashes on startup\",\"description\":\"After Windows update, app crashes immediately. All users blocked.\"}"
```

### Expected Output

**Worker Terminal:**
```
info: Processing message for TicketId: TKT-A1B2C3D4
(wait 1-3 seconds for AI call)
info: AI Analysis successful. Category: Deployment, Priority: Critical
info: AI Analysis Complete for TKT-A1B2C3D4: Deployment / Critical
info: Successfully completed message for TicketId: TKT-A1B2C3D4
```

## 🔍 Troubleshooting

### "AzureOpenAI:Endpoint is required"
```bash
dotnet user-secrets list
# Should show both Endpoint and DeploymentName
```

### "DefaultAzureCredential failed"
```bash
az logout
az login
az account show  # Verify correct subscription
```

### "HTTP 429 Too Many Requests"
- SDK auto-retries 3 times
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

## 📁 Key Files

| File | Purpose |
|------|---------|
| `SmartTicket.Contracts/Models/TicketAnalysisResult.cs` | AI output schema |
| `SmartTicket.Worker/Services/OpenAiTicketAnalyzer.cs` | AI service implementation |
| `SmartTicket.Worker/Worker.cs` | Message consumer with AI integration |
| `SmartTicket.Worker/Program.cs` | DI configuration |
| `STAGE4_CONFIGURATION.md` | Complete setup guide |

## 🎯 Key Concepts

### Structured Outputs
Forces AI to return valid JSON matching schema - no parsing needed!

### DefaultAzureCredential
Zero secrets in code. Works locally (CLI) and in cloud (Managed Identity).

### Low Temperature (0.1)
Deterministic results for classification tasks.

### CancellationToken
Graceful shutdown - aborts HTTP calls when pod terminates.

## 🔗 Useful Links

- [Stage 4 Configuration Guide](STAGE4_CONFIGURATION.md)
- [Stage 4 Completion Summary](STAGE4_COMPLETION_SUMMARY.md)
- [Main README](README.md)

---

**Quick Tip**: Always verify `az login` before testing. Most errors are authentication-related!
