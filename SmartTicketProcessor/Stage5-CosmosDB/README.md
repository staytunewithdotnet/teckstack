# Stage 5: Azure Cosmos DB Integration

## Overview

In this stage, we replace the in-memory store with **Azure Cosmos DB** for NoSQL persistence. We focus on production-grade patterns:
- **Keyless authentication** (DefaultAzureCredential)
- **Efficient Point Reads** (single-digit millisecond latency)
- **Optimistic Concurrency** (ETags) to safely handle Service Bus "at-least-once" message redelivery

To maintain clean architecture and demonstrate good separation of concerns, we create a dedicated `SmartTicket.Infrastructure` project to hold the Cosmos DB logic.

## 🎯 What's New in Stage 5

✅ **Azure Cosmos DB Integration** - Production-ready NoSQL database  
✅ **Point Reads** - Ultra-fast single-item retrieval by ID + Partition Key  
✅ **Optimistic Concurrency** - ETag-based conflict detection for duplicate messages  
✅ **Partition Key Design** - `/customerId` for efficient customer-specific queries  
✅ **Keyless Authentication** - DefaultAzureCredential (zero secrets)  
✅ **Auto-Provisioning** - Database and container auto-creation for local dev  

## 📁 Project Structure

```
Stage5-CosmosDB/
├── README.md                     # This file
├── SmartTicket.Contracts/        # Shared contracts
│   ├── Models/
│   │   ├── Ticket.cs             # Updated with ETag & Analysis
│   │   └── TicketAnalysisResult.cs
│   └── Events/
│       └── TicketSubmittedEvent.cs
├── SmartTicket.Worker/           # Background processor
│   ├── Worker.cs                 # Updated with Cosmos DB integration
│   ├── Program.cs                # DI configuration
│   └── Services/
│       └── OpenAiTicketAnalyzer.cs
├── ../SmartTicket.Infrastructure/ # NEW: Infrastructure layer
│   └── Repositories/
│       ├── ITicketRepository.cs
│       └── CosmosDbTicketRepository.cs
└── docker-compose.yml            # Service Bus + Cosmos DB emulators
```

##  Prerequisites

1. **Docker Desktop** installed and running
2. **Azure CLI** logged in (`az login`)
3. **.NET 10 SDK** installed
4. **Completed Stages 1-4** (especially Stage 4 for AI integration)

## ⚙️ Setup Instructions

### Step 1: Start Emulators

```bash
cd Stage5-CosmosDB
docker-compose up -d
```

Wait ~30 seconds for both emulators to initialize.

### Step 2: Configure User Secrets

The Worker needs Cosmos DB connection details:

```bash
cd SmartTicket.Worker
dotnet user-secrets set "CosmosDb:AccountEndpoint" "https://localhost:8081"
dotnet user-secrets set "CosmosDb:DatabaseName" "SmartTicketDb"
dotnet user-secrets set "CosmosDb:ContainerName" "Tickets"
```

**Note:** The Cosmos DB Emulator uses a self-signed certificate. The code includes a `#if DEBUG` block to accept it automatically.

### Step 3: Build and Run

#### Terminal 1: API (Optional - for creating tickets)
```bash
cd ../Stage2-AsyncProcessing/SmartTicket.Api
dotnet run
```

#### Terminal 2: Worker
```bash
cd SmartTicket.Worker
dotnet run
```

## 🧪 How to Test

### Option 1: Via API (if running)

```bash
curl -X POST http://localhost:5232/api/tickets \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "CUST-AI",
    "title": "Login Issue",
    "description": "Cannot access my account after password reset."
  }'
```

### Option 2: Direct Message to Service Bus

Use Azure Service Bus Explorer or write a test script to send a `TicketSubmittedEvent` directly to the queue.

### Verify Results

1. **Check Worker Logs:**
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

2. **Open Cosmos DB Data Explorer:**
   - URL: https://localhost:8081/_explorer/index.html
   - Accept the security warning (self-signed cert)
   - Navigate to: `SmartTicketDb` → `Tickets`
   - You'll see documents with:
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

## 🔍 Key Implementation Details

### 1. Point Reads vs Queries

**Point Read (Used):**
```csharp
var response = await _container.ReadItemAsync<Ticket>(
    id, 
    new PartitionKey(partitionKey), 
    cancellationToken);
```
- ✅ Single-digit millisecond latency
- ✅ Consumes only 1 Request Unit (RU)
- ✅ No index scan required

**SQL Query (NOT Used):**
```csharp
// DON'T DO THIS - Expensive!
var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
    .WithParameter("@id", id);
```
- ❌ Requires index scan
- ❌ Consumes more RUs
- ❌ Higher latency

### 2. Optimistic Concurrency with ETags

Service Bus guarantees **"at-least-once"** delivery, meaning a message might be processed twice if a pod crashes. We use Cosmos DB's `_etag` to detect duplicates:

```csharp
var requestOptions = new ItemRequestOptions { IfMatchEtag = expectedETag };

try
{
    var response = await _container.ReplaceItemAsync(
        updatedTicket, 
        id, 
        new PartitionKey(partitionKey), 
        requestOptions);
    
    return true; // Success
}
catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
{
    // ETag mismatch: another worker already processed this message
    _logger.LogWarning("Concurrency conflict. Message was already processed.");
    return false;
}
```

**Flow:**
1. Worker A reads ticket (ETag: "abc123")
2. Worker A processes AI analysis
3. Worker B reads same ticket (ETag: "abc123") - duplicate delivery!
4. Worker A updates ticket with `IfMatchEtag: "abc123"` ✅ Success, ETag becomes "def456"
5. Worker B tries to update with `IfMatchEtag: "abc123"` ❌ 412 Precondition Failed
6. Worker B recognizes duplicate and safely completes message

### 3. Partition Key Design

We use `/customerId` as the partition key because:
- ✅ Users typically check their own tickets
- ✅ All tickets for one customer are in the same logical partition
- ✅ Customer-specific queries are highly efficient
- ✅ Avoids expensive cross-partition queries

**Example Query:**
```csharp
// Fast - stays within one partition
var tickets = container.GetItemLinqQueryable<Ticket>()
    .Where(t => t.CustomerId == "CUST-AI")
    .ToList();
```

### 4. Keyless Authentication

No connection strings or account keys in code:

```csharp
var credential = new DefaultAzureCredential();
var client = new CosmosClient(accountEndpoint, credential, clientOptions);
```

**Environments:**
- **Local Dev:** Uses Azure CLI token (`az login`)
- **AKS:** Uses Workload Identity
- **Production:** Uses Managed Identity

## 🎯 Interview Gold: Stage 5 Talking Points

### 1. Point Reads over Queries
> "I used `ReadItemAsync` with both the ID and PartitionKey. This is a Point Read, which guarantees single-digit millisecond latency and consumes only 1 Request Unit (RU). Using a SQL `SELECT * FROM c WHERE c.id = @id` query would require an index scan and consume significantly more RUs."

### 2. Optimistic Concurrency (ETags)
> "Because Service Bus guarantees 'at-least-once' delivery, a message might be processed twice if a pod crashes. I use Cosmos DB's `_etag` and pass it via `IfMatchEtag` in the `ReplaceItemAsync` call. If a second worker tries to process the same message, it gets a 412 Precondition Failed, recognizes it's a duplicate, and safely completes the message without corrupting data."

### 3. Partition Key Design
> "I chose `/customerId` as the partition key. In a real system, users check the status of their own tickets. This ensures all tickets for a single customer are stored in the same logical partition, making customer-specific queries highly efficient and avoiding cross-partition queries."

### 4. Keyless Authentication
> "The repository uses `DefaultAzureCredential`. There are no connection strings or account keys in the code or configuration. Locally, it uses my Azure CLI token. In AKS, it will seamlessly use Workload Identity, adhering to the principle of least privilege."

### 5. Auto-Provisioning for Local Dev
> "The repository constructor calls `CreateDatabaseIfNotExistsAsync` and `CreateContainerIfNotExistsAsync` wrapped in `.Wait()`. This ensures the database and container exist before any operations, making local development seamless without manual setup."

## 🔧 Troubleshooting

### Issue: Cosmos DB Emulator Certificate Error

**Symptom:** `HttpRequestException: The SSL connection could not be established`

**Solution:** The `#if DEBUG` block in `CosmosDbTicketRepository.cs` handles this automatically. If you're still seeing errors:
1. Ensure you're running in Debug mode
2. Or manually trust the emulator certificate (Windows only):
   ```powershell
   # Run as Administrator
   cd "C:\Program Files\Azure Cosmos DB Emulator"
   .\Microsoft.Azure.Cosmos.Emulator.exe /GenCert=localhost
   ```

### Issue: Database Not Found

**Symptom:** `CosmosException: Database 'SmartTicketDb' does not exist`

**Solution:** The auto-provisioning should handle this. If it fails:
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

## 📊 Performance Characteristics

| Operation | Latency | RU Cost | Use Case |
|-----------|---------|---------|----------|
| Point Read | < 10ms | 1 RU | Get ticket by ID |
| Point Write | < 10ms | ~10 RU | Create/update ticket |
| SQL Query | 50-200ms | Variable | Cross-partition queries |

## 🚀 Next Steps

After completing Stage 5:
1. **Add indexing policy** for common queries (e.g., by category, priority)
2. **Implement change feed** for real-time analytics
3. **Add TTL (Time-To-Live)** for automatic cleanup of old tickets
4. **Deploy to Azure Cosmos DB** (replace emulator with production instance)
5. **Configure throughput** (RU/s) based on expected load

---

**Stage 5 Complete!** You now have a production-ready Cosmos DB integration with optimistic concurrency handling. 🎉
