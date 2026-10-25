# Stage 2: Asynchronous Processing with Azure Service Bus

## 🎯 Learning Objectives

In this stage, you'll learn:
- ✅ Azure Service Bus message queues
- ✅ Event-driven architecture
- ✅ Background worker services
- ✅ Message publishing and consumption
- ✅ Peek-Lock processing pattern
- ✅ Dead-letter queues and retry logic
- ✅ Docker-based Service Bus emulator

## 📁 Project Structure

```
Stage2-AsyncProcessing/
├── SmartTicket.Api/                  # API (Producer)
│   ├── Services/
│   │   └── ServiceBusMessagePublisher.cs  # Publishes messages
│   └── Program.cs                    # Updated with SB integration
├── SmartTicket.Contracts/            # Shared contracts
│   └── Events/
│       └── TicketSubmittedEvent.cs   # Event schema
├── SmartTicket.Worker/               # Worker (Consumer)
│   └── Worker.cs                     # Message processor
├── docker-compose.yml                # Service Bus emulator
└── SmartTicketProcessor.Stage2.sln
```

## 🏗️ Architecture

```
Client → API (POST /api/tickets)
         ↓
    Save to Repository
         ↓
    Publish to Service Bus Queue
         ↓
    Return 202 Accepted immediately
    
Service Bus Queue
         ↓
Worker (Background Service)
         ↓
    Receive message (Peek-Lock)
         ↓
    Process (simulate AI - 2 seconds)
         ↓
    Complete message (remove from queue)
```

## 🚀 Getting Started

### Prerequisites
- Docker Desktop (for Service Bus emulator)
- .NET 10 SDK
- Completed Stage 1

### Step 1: Start Service Bus Emulator

```bash
cd Stage2-AsyncProcessing
docker-compose up -d
```

Wait ~30 seconds for the emulator to initialize.

### Step 2: Run the API (Terminal 1)

```bash
cd SmartTicket.Api
dotnet run
```

### Step 3: Run the Worker (Terminal 2)

```bash
cd SmartTicket.Worker
dotnet run
```

## 🧪 Testing the Async Flow

### 1. Submit a Ticket

**Via Swagger:** http://localhost:5232/swagger

```json
{
  "customerId": "CUST-ASYNC-TEST",
  "title": "Application crashes on startup",
  "description": "After Windows update, app crashes immediately."
}
```

**Expected API Response (immediate < 100ms):**
```json
{
  "ticketId": "TKT-A1B2C3D4",
  "status": "Submitted",
  "statusUrl": "/api/tickets/TKT-A1B2C3D4"
}
```

### 2. Observe Worker Processing

**Worker Terminal Output:**
```
info: Starting Service Bus Processor...
info: Processing message for TicketId: TKT-A1B2C3D4
(wait 2 seconds - simulating AI processing)
info: AI Processing simulated for TicketId: TKT-A1B2C3D4. Priority: High
info: Successfully processed and completed message for TicketId: TKT-A1B2C3D4
```

### 3. Verify Message Removed from Queue

The message is automatically removed after successful processing (CompleteMessageAsync).

## 🎓 Key Concepts to Learn

### 1. Event-Driven Architecture

**Shared Contract:** [`SmartTicket.Contracts/Events/TicketSubmittedEvent.cs`](SmartTicket.Contracts/Events/TicketSubmittedEvent.cs)

```csharp
public sealed record TicketSubmittedEvent(
    string TicketId,
    string CustomerId,
    string Title,
    string Description,
    DateTimeOffset SubmittedAt);
```

**Why shared library?**
- Ensures API and Worker agree on schema
- Prevents deserialization errors
- Single source of truth

### 2. Message Publisher

[`SmartTicket.Api/Services/ServiceBusMessagePublisher.cs`](SmartTicket.Api/Services/ServiceBusMessagePublisher.cs)

```csharp
public async Task PublishAsync(TicketSubmittedEvent @event, CancellationToken ct)
{
    var message = new ServiceBusMessage(JsonSerializer.Serialize(@event))
    {
        CorrelationId = @event.TicketId,  // For distributed tracing
        MessageId = Guid.NewGuid().ToString(),
        Subject = "TicketSubmitted"
    };
    
    await _sender.SendMessageAsync(message, ct);
}
```

**Key features:**
- Correlation ID for tracing
- Custom properties for routing
- Async publishing

### 3. Message Consumer (Worker)

[`SmartTicket.Worker/Worker.cs`](SmartTicket.Worker/Worker.cs)

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    await _processor.StartProcessingAsync(stoppingToken);
    await Task.Delay(Timeout.Infinite, stoppingToken);
}

private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
{
    // 1. Deserialize message
    var @event = JsonSerializer.Deserialize<TicketSubmittedEvent>(body);
    
    // 2. Process (simulate AI)
    await Task.Delay(2000);
    
    // 3. Complete message (remove from queue)
    await args.CompleteMessageAsync(args.Message);
}
```

### 4. Peek-Lock Pattern

**How it works:**
1. Worker receives message (locked)
2. Processes message
3. Calls `CompleteMessageAsync()` → removes from queue
4. If fails, calls `AbandonMessageAsync()` → releases lock for retry

**Benefits:**
- No message loss
- Automatic retry on failure
- Dead-letter after max retries

### 5. Error Handling

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to process message");
    
    // Abandon releases the lock for retry
    await args.AbandonMessageAsync(args.Message);
}
```

**Retry behavior:**
- MaxDeliveryCount = 3 (set in emulator config)
- After 3 failures → dead-letter queue
- Manual inspection needed

## 💡 Exercises

1. **Add Logging**: Add structured logging with message IDs
2. **Change Concurrency**: Modify `MaxConcurrentCalls` and observe behavior
3. **Simulate Failure**: Throw exception randomly to test retry logic
4. **Monitor Queue**: Use Service Bus Explorer to view messages
5. **Add Metrics**: Track processing time per message

## 🔍 Code Walkthrough

### Step 1: Understand the Event Contract

Open [`SmartTicket.Contracts/Events/TicketSubmittedEvent.cs`](SmartTicket.Contracts/Events/TicketSubmittedEvent.cs)
- Sealed record for immutability
- All fields required
- Shared between API and Worker

### Step 2: Study the Message Publisher

Open [`SmartTicket.Api/Services/ServiceBusMessagePublisher.cs`](SmartTicket.Api/Services/ServiceBusMessagePublisher.cs)
- Interface-based design (easy to mock)
- Correlation ID for tracing
- IAsyncDisposable for cleanup

### Step 3: Examine the Worker

Open [`SmartTicket.Worker/Worker.cs`](SmartTicket.Worker/Worker.cs)
- BackgroundService base class
- ProcessMessageAsync handler
- Complete vs Abandon logic

### Step 4: Review Docker Compose

Open [`docker-compose.yml`](./docker-compose.yml)
- Service Bus emulator configuration
- Port mapping
- Volume mounts for config

## 🐛 Troubleshooting

### Emulator Won't Start

```bash
# Check Docker is running
docker ps

# View logs
docker logs sb-emulator

# Restart
docker-compose down
docker-compose up -d
```

### Worker Can't Connect

```bash
# Verify emulator is running
docker ps | findstr sb-emulator

# Check connection string
# Should be: Endpoint=sb://127.0.0.1;...;UseDevelopmentEmulator=true;
```

### Messages Not Processing

```bash
# Check Worker terminal for errors
# Verify queue name matches: "ticket-processing"
# Check emulator config: deploy/emulator-config.json
```

## 🎯 What Makes This Production-Ready?

✅ **Peek-Lock Pattern** - No message loss  
✅ **Dead-Letter Queue** - Failed messages preserved  
✅ **Correlation IDs** - Distributed tracing  
✅ **Graceful Shutdown** - CancellationToken support  
✅ **Concurrent Processing** - Configurable parallelism  
✅ **Local Emulator** - Cost-free development  

## 🎯 What's Next?

Once you're comfortable with Stage 2, move to **[Stage 4: Azure AI Integration](../Stage4-AzureAI)** where you'll learn:
- Azure OpenAI with Structured Outputs
- DefaultAzureCredential authentication
- Replacing simulated delay with real AI calls
- JSON schema enforcement

## 📚 Additional Resources

- [Azure Service Bus Documentation](https://learn.microsoft.com/en-us/azure/service-bus-messaging/)
- [Background Services](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)
- [Service Bus Emulator](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-emulator-overview)
- [Peek-Lock Pattern](https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement)

---

**Status:** ✅ Ready to Learn  
**Complexity:** Intermediate  
**Prerequisites:** Stage 1 complete  
**Estimated Time:** 3-4 hours
