# Stage 5 Quick Reference

## 🚀 Quick Start

### 1. Start Emulators
```bash
cd Stage5-CosmosDB
docker-compose up -d
# Wait 30 seconds
```

### 2. Configure Azure OpenAI
```bash
cd SmartTicket.Worker
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini"
```

### 3. Run Worker
```bash
dotnet run
```

### 4. Test
```bash
curl -X POST http://localhost:5232/api/tickets \
  -H "Content-Type: application/json" \
  -d '{"customerId":"CUST-AI","title":"Test","description":"Testing."}'
```

### 5. Verify
- **Logs:** Look for "Successfully updated TicketId"
- **Cosmos DB:** https://localhost:8081/_explorer/index.html

## 🔑 Key Concepts

### Point Read
```csharp
// ✅ Fast (< 10ms, 1 RU)
await _container.ReadItemAsync<Ticket>(id, new PartitionKey(partitionKey));

// ❌ Slow (index scan, more RUs)
var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id");
```

### Optimistic Concurrency
```csharp
// Read ticket with ETag
var ticket = await _repository.GetByIdAsync(id, partitionKey, cancellationToken);

// Update with IfMatchEtag
var requestOptions = new ItemRequestOptions { IfMatchEtag = ticket.ETag };
await _container.ReplaceItemAsync(updatedTicket, id, new PartitionKey(partitionKey), requestOptions);

// If ETag mismatch → 412 Precondition Failed → duplicate message detected
```

### Partition Key
```csharp
// ✅ Good: /customerId (efficient customer queries)
new PartitionKey(ticket.CustomerId)

// ❌ Bad: /id (scattered across partitions)
new PartitionKey(ticket.Id)
```

### Keyless Auth
```csharp
// ✅ No secrets in code
var credential = new DefaultAzureCredential();
var client = new CosmosClient(endpoint, credential);

// ❌ Don't use connection strings
var client = new CosmosClient(connectionString); // Avoid this!
```

## 📁 Project Structure

```
Stage5-CosmosDB/
├── docker-compose.yml              # SB + Cosmos DB emulators
├── deploy/emulator-config.json     # Queue configuration
├── README.md                       # Full guide
├── STAGE5_CONFIGURATION.md         # Setup instructions
└── SmartTicket.Worker/
    ├── Worker.cs                   # Message processor
    ├── Program.cs                  # DI configuration
    └── appsettings.json            # Configuration

SmartTicket.Infrastructure/
└── Repositories/
    ├── ITicketRepository.cs        # Interface
    └── CosmosDbTicketRepository.cs # Implementation
```

##  Common Commands

### Build
```bash
# Infrastructure
cd SmartTicket.Infrastructure
dotnet build

# Stage 5 Worker
cd Stage5-CosmosDB/SmartTicket.Worker
dotnet build
```

### Run
```bash
# Start emulators
cd Stage5-CosmosDB
docker-compose up -d

# Run worker
cd SmartTicket.Worker
dotnet run
```

### Check Emulators
```bash
# List running containers
docker ps

# View logs
docker logs sb-emulator-stage5
docker logs cosmos-emulator-stage5

# Stop emulators
docker-compose down
```

##  Troubleshooting

### Certificate Error
**Fix:** Code has `#if DEBUG` block to accept self-signed cert automatically.

### Database Not Found
**Fix:** Auto-provisioning creates DB/container on startup. If fails, create manually via Data Explorer.

### ETag Mismatch
**Fix:** Ensure you're passing the ETag from the read operation:
```csharp
var ticket = await _repository.GetByIdAsync(...);
await _repository.TryUpdateAnalysisAsync(..., ticket.ETag, ...);
```

### Azure OpenAI Auth Error
**Fix:** 
1. `az login`
2. `az account set --subscription "<id>"`
3. Verify "Cognitive Services OpenAI User" role

## 📊 Performance

| Operation | Latency | RU Cost |
|-----------|---------|---------|
| Point Read | < 10ms | 1 RU |
| Point Write | < 10ms | ~10 RU |
| SQL Query | 50-200ms | Variable |

##  Interview Points

1. **Point Reads** - Single-digit ms latency, 1 RU cost
2. **Optimistic Concurrency** - ETags handle duplicate messages safely
3. **Partition Key** - `/customerId` for efficient customer queries
4. **Keyless Auth** - DefaultAzureCredential, zero secrets
5. **Separation of Concerns** - SmartTicket.Infrastructure project

##  Useful Links

- **Cosmos DB Data Explorer:** https://localhost:8081/_explorer/index.html
- **Service Bus Explorer:** Use Azure Portal or Service Bus Explorer tool
- **Cosmos DB Docs:** https://docs.microsoft.com/azure/cosmos-db/
- **DefaultAzureCredential:** https://docs.microsoft.com/dotnet/api/azure.identity.defaultazurecredential

---

**Quick Reference Complete!** Use this for fast lookups while working on Stage 5. 
