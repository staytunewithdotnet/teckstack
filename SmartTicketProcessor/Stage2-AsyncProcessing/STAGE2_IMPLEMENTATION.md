# Stage 2: Local Asynchronous Processing - Implementation Guide

## 🎯 Overview

Stage 2 introduces asynchronous message processing using Azure Service Bus. The API now publishes events to a Service Bus queue, and a separate Worker service consumes and processes these messages asynchronously.

## 📦 What Was Added

### New Projects

1. **SmartTicket.Contracts** - Shared class library for event contracts
2. **SmartTicket.Worker** - Background worker service that processes Service Bus messages

### New Dependencies

- `Azure.Messaging.ServiceBus` (v7.21.0) - Added to both API and Worker
- Project references between API/Worker and Contracts

## 🏗️ Architecture

```
┌─────────────────┐         Publish         ┌──────────────────────┐
│   SmartTicket   │  ─────────────────────►  │  Azure Service Bus   │
│      API        │   TicketSubmittedEvent   │      Emulator        │
│                 │                          │  (Local Docker)      │
└─────────────────┘                          └──────────┬───────────┘
                                                        │
                                              Consume (Peek-Lock)
                                                        │
                                                        ▼
                                               ┌──────────────────────┐
                                               │  SmartTicket.Worker  │
                                               │                      │
                                               │  - Deserialize       │
                                               │  - Simulate AI (2s)  │
                                               │  - Complete Message  │
                                               └──────────────────────┘
```

## 📁 Project Structure

```
SmartTicketProcessor/
├── SmartTicket.Contracts/
│   └── Events/
│       └── TicketSubmittedEvent.cs    # ✓ Shared event contract
├── SmartTicket.Api/
│   ├── Services/
│   │   ├── IMessagePublisher.cs       # ✓ Publisher interface
│   │   └── ServiceBusMessagePublisher.cs  # ✓ Service Bus implementation
│   └── Program.cs                     # ✓ Updated with Service Bus integration
├── SmartTicket.Worker/
│   ├── Worker.cs                      # ✓ TicketProcessingWorker
│   └── Program.cs                     # ✓ Worker host configuration
├── deploy/
│   └── emulator-config.json           # ✓ Service Bus emulator config
├── docker-compose.yml                 # ✓ Docker Compose for emulator
└── ...
```

## 🔧 Implementation Details

### 1. Event Contract (SmartTicket.Contracts)

**File:** [`Events/TicketSubmittedEvent.cs`](C:\AppCodeStore\AI-Model-Code\teckstack\SmartTicketProcessor\SmartTicket.Contracts\Events\TicketSubmittedEvent.cs)

```csharp
public sealed record TicketSubmittedEvent(
    string TicketId,
    string CustomerId,
    string Title,
    string Description,
    DateTimeOffset SubmittedAt);
```

**Why a shared library?**
- Ensures API and Worker agree on the schema
- Prevents deserialization errors
- Single source of truth for event structure
- Easy versioning in the future

### 2. Message Publisher (SmartTicket.Api)

**Files:** 
- [`Services/IMessagePublisher.cs`](C:\AppCodeStore\AI-Model-Code\teckstack\SmartTicketProcessor\SmartTicket.Api\Services\ServiceBusMessagePublisher.cs)
- [`Services/ServiceBusMessagePublisher.cs`](C:\AppCodeStore\AI-Model-Code\teckstack\SmartTicketProcessor\SmartTicket.Api\Services\ServiceBusMessagePublisher.cs)

**Key Features:**
- Abstracted behind interface for testability
- Adds correlation ID for distributed tracing
- Includes custom application properties for routing
- Implements `IAsyncDisposable` for proper cleanup

**Message Properties:**
```csharp
CorrelationId = ticketId          // For tracing & idempotency
MessageId = Guid.NewGuid()        // Unique message identifier
Subject = "TicketSubmitted"       // Message type identifier
ApplicationProperties:
  - MessageType: "TicketSubmitted"
  - SchemaVersion: "1.0"
```

### 3. API Integration (Program.cs)

The POST endpoint now:
1. Saves ticket to repository
2. **Publishes event to Service Bus**
3. Returns 202 Accepted immediately

```csharp
// 1. Save to database
await repository.CreateAsync(ticket, ct);

// 2. Publish to Service Bus
var @event = new TicketSubmittedEvent(...);
await publisher.PublishAsync(@event, ct);

// 3. Return immediately
return Results.Accepted(...);
```

### 4. Message Consumer (SmartTicket.Worker)

**File:** [`Worker.cs`](C:\AppCodeStore\AI-Model-Code\teckstack\SmartTicketProcessor\SmartTicket.Worker\Worker.cs)

**Processing Flow:**
```
1. Receive message (Peek-Lock mode)
2. Deserialize TicketSubmittedEvent
3. Validate message format
4. Simulate AI processing (2 second delay)
5. Complete message (removes from queue)
```

**Error Handling:**
- **Invalid format** → Dead-letter the message
- **Processing error** → Abandon (retry)
- **Cancellation** → Graceful shutdown
- **Max delivery count** → Auto dead-letter after 3 attempts

**Processor Configuration:**
```csharp
MaxConcurrentCalls = 5              // Parallel processing
AutoCompleteMessages = false        // Manual completion (Peek-Lock)
MaxAutoLockRenewalDuration = 5 min  // Prevent lock timeout
TransportType = AmqpWebSockets      // Better for Docker/emulator
```

### 5. Docker Infrastructure

**File:** [`docker-compose.yml`](C:\AppCodeStore\AI-Model-Code\teckstack\SmartTicketProcessor\docker-compose.yml)

Runs the official Azure Service Bus Emulator:
```yaml
services:
  servicebus-emulator:
    image: mcr.microsoft.com/azure-messaging/servicebus-emulator:latest
    ports:
      - "5672:5672"
    volumes:
      - ./deploy/emulator-config.json:/ServiceBus_Emulator/ConfigFiles/Config.json
```

**File:** [`deploy/emulator-config.json`](C:\AppCodeStore\AI-Model-Code\teckstack\SmartTicketProcessor\deploy\emulator-config.json)

Creates the `ticket-processing` queue automatically:
```json
{
  "UserConfig": {
    "Namespaces": [
      {
        "Name": "sb-emulator-ns",
        "Queues": [
          {
            "Name": "ticket-processing",
            "Properties": {
              "DeadLetteringOnMessageExpiration": true,
              "MaxDeliveryCount": 3
            }
          }
        ]
      }
    ]
  }
}
```

## 🚀 How to Test Stage 2

### Step 1: Start the Service Bus Emulator

```bash
cd C:\AppCodeStore\AI-Model-Code\teckstack\SmartTicketProcessor
docker-compose up -d
```

**Wait ~30 seconds** for the emulator to initialize. You can check status:
```bash
docker logs sb-emulator
```

Look for: `"Service Bus Emulator is ready"`

### Step 2: Run the API (Terminal 1)

```bash
cd SmartTicket.Api
dotnet run
```

Expected output:
```
Now listening on: http://localhost:5232
Application started. Press Ctrl+C to shut down.
```

### Step 3: Run the Worker (Terminal 2)

```bash
cd SmartTicket.Worker
dotnet run
```

Expected output:
```
info: SmartTicket.Worker.TicketProcessingWorker[0]
      Starting Service Bus Processor...
```

### Step 4: Trigger the Flow

**Option A: Using Swagger**
1. Open http://localhost:5232/swagger
2. POST `/api/tickets` with:
```json
{
  "customerId": "CUST-999",
  "title": "Unable to log in after deployment",
  "description": "Users cannot log in after yesterday's deployment. This is affecting the production environment."
}
```

**Option B: Using cURL**
```bash
curl -X POST http://localhost:5232/api/tickets \
  -H "Content-Type: application/json" \
  -d "{\"customerId\":\"CUST-999\",\"title\":\"Login Issue\",\"description\":\"Users cannot log in after deployment.\"}"
```

### Step 5: Observe the Processing

**API Terminal** (immediate response):
```
info: SmartTicket.Api.Services.ServiceBusMessagePublisher[0]
      Published message for TicketId: TKT-A1B2C3D4
```

**Worker Terminal** (2-second delay):
```
info: SmartTicket.Worker.TicketProcessingWorker[0]
      Processing message for TicketId: TKT-A1B2C3D4

(wait 2 seconds...)

info: SmartTicket.Worker.TicketProcessingWorker[0]
      AI Processing simulated for TicketId: TKT-A1B2C3D4. Priority: High, Sentiment: Negative

info: SmartTicket.Worker.TicketProcessingWorker[0]
      Successfully processed and completed message for TicketId: TKT-A1B2C3D4
```

## ✅ Expected Behavior

### Success Scenario
1. ✅ API returns 202 Accepted **immediately** (< 100ms)
2. ✅ Worker receives message within 1-2 seconds
3. ✅ Worker processes for 2 seconds (simulated AI)
4. ✅ Worker completes message (removes from queue)
5. ✅ Both terminals show success logs

### Error Scenarios

**Invalid Message Format:**
- Worker dead-letters the message
- Logs warning: "Invalid message format. Dead-lettering."

**Processing Failure:**
- Worker abandons the message
- Message becomes available for retry
- After 3 attempts → auto dead-letter

**Worker Shutdown:**
- Graceful stop via Ctrl+C
- In-flight messages are abandoned (not lost)
- Logs: "Stopping Service Bus Processor..."

## 🔍 Verification Checklist

- [ ] Docker container running (`docker ps | findstr sb-emulator`)
- [ ] API starts without errors
- [ ] Worker starts and connects to Service Bus
- [ ] POST returns 202 Accepted immediately
- [ ] Worker logs show message processing
- [ ] 2-second delay observed in Worker
- [ ] Message completed successfully
- [ ] No messages stuck in queue

## 🐛 Troubleshooting

### Emulator Won't Start
```bash
# Check logs
docker logs sb-emulator

# Restart
docker-compose down
docker-compose up -d
```

### Worker Can't Connect
1. Verify emulator is running: `docker ps`
2. Check connection string in Program.cs
3. Ensure port 5672 is not blocked
4. Try WebSocket transport (already configured)

### Messages Not Processing
1. Check Worker logs for errors
2. Verify queue name matches ("ticket-processing")
3. Check emulator config JSON is valid
4. Restart both API and Worker

### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet build
```

## 📊 Performance Characteristics

| Metric | Value | Notes |
|--------|-------|-------|
| API Response Time | < 100ms | Returns before processing |
| Message Publish | ~50ms | Service Bus latency |
| Worker Processing | 2s + overhead | Simulated AI delay |
| Concurrent Messages | Up to 5 | Per Worker instance |
| Max Retries | 3 | Then dead-letter |

## 🎓 Interview Talking Points

### Why Asynchronous Processing?
- **Decoupling**: API doesn't wait for slow AI processing
- **Scalability**: Worker can scale independently
- **Resilience**: Failed messages retry automatically
- **User Experience**: Instant feedback (202 Accepted)

### Why Peek-Lock Mode?
- **Safety**: Message isn't deleted until processing succeeds
- **Retry**: Failed messages can be retried
- **Concurrency**: Multiple workers can process safely
- **Dead-letter**: Poison messages don't block the queue

### Why Separate Worker Service?
- **Single Responsibility**: API handles HTTP, Worker handles processing
- **Independent Scaling**: Scale Workers based on queue depth
- **Technology Choice**: Worker can use different tech stack
- **Deployment**: Update Worker without affecting API

### Why Service Bus over Queue?
- **Enterprise Features**: Dead-letter, sessions, transactions
- **Reliability**: Guaranteed delivery
- **Monitoring**: Built-in metrics and diagnostics
- **Azure Integration**: Native support for cloud deployment

## 🔄 Next Steps (Stage 3-5)

- **Stage 3**: Add real Azure OpenAI integration
- **Stage 4**: Implement Cosmos DB for persistence
- **Stage 5**: Deploy to Azure Kubernetes Service

---

**Status**: ✅ Stage 2 Complete  
**Test Status**: Ready for local testing with Docker  
**Next Action**: Start emulator and test the async flow
