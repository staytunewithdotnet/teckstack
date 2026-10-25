# MassTransit Saga Demo - Production-Ready Distributed Workflow

This project demonstrates a **production-ready implementation** of the MassTransit State Machine (Saga) pattern with Entity Framework Core Outbox/Inbox for building resilient, distributed workflows like Order Processing.

## 🎯 Architecture Overview

This solution consists of **3 projects** that prove the distributed nature of the saga pattern:

1. **Contracts** - Shared messages (Events/Commands) between services
2. **Ordering.Api** - The Saga Orchestrator (hosts State Machine + EF Core + RabbitMQ)
3. **Inventory.Worker** - The Participant (listens for commands, does "work", replies with events)

### Workflow Diagram

```
┌─────────────┐      SubmitOrder       ┌──────────────────┐
│   Client     │ ─────────────────────► │  Ordering.Api    │
│  (HTTP/API)  │                        │  (Saga Host)     │
└─────────────┘                        └────────┬─────────┘
                                                │
                                                │ ReserveInventoryCommand
                                                ▼
                                       ┌──────────────────┐
                                       │ Inventory.Worker │
                                       │  (Participant)   │
                                       └────────┬─────────┘
                                                │
                    ┌───────────────────────────┼───────────────────────────┐
                    │                           │                           │
        InventoryReservedEvent      InventoryRejectedEvent      Timeout (2 min)
                    │                           │                           │
                    ▼                           ▼                           ▼
          ┌─────────────────┐        ┌─────────────────┐        ┌─────────────────┐
          │   Completed     │        │    Failed       │        │    Failed       │
          │  (Success)      │        │ (Compensation)  │        │ (Timeout)       │
          └─────────────────┘        └─────────────────┘        └─────────────────┘
                    │                           │                           │
                    ▼                           ▼                           ▼
          OrderCompletedEvent         OrderFailedEvent           OrderFailedEvent
```

## 🚀 Key Features

### ✅ What Makes This Production-Ready

1. **Timeout Handling** - Automatic timeout after 2 minutes if inventory service doesn't respond
2. **Compensation Pattern** - Publishes `OrderFailedEvent` when inventory is rejected or times out
3. **EF Core Outbox/Inbox** - Guarantees message delivery and prevents duplicates
4. **Idempotency** - Handles duplicate messages gracefully in all states
5. **SQL Server Lock Provider** - Prevents deadlocks with concurrent saga updates
6. **Resilient Database Access** - Retry on failure for transient SQL errors

## 📋 Prerequisites

- **.NET 10 SDK** or later
- **RabbitMQ** running locally (or Docker)
- **SQL Server** (LocalDB, Express, or full instance)

## 🛠️ Setup Instructions

### Step 1: Start RabbitMQ

Using Docker (recommended):
```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

Or install RabbitMQ locally and ensure it's running on `localhost:5672`.

### Step 2: Configure SQL Server Connection

Update the connection string in `Ordering.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=OrderingSagaDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;"
  }
}
```

**For LocalDB:**
```
Server=(localdb)\\mssqllocaldb;Database=OrderingSagaDb;Trusted_Connection=True;
```

**For SQL Server Express:**
```
Server=localhost\\SQLEXPRESS;Database=OrderingSagaDb;Trusted_Connection=True;TrustServerCertificate=True;
```

### Step 3: Build the Solution

```bash
cd MassTransitSagaDemo
dotnet build
```

### Step 4: Apply Database Migrations

The migrations will be applied automatically when you start `Ordering.Api`, but you can also do it manually:

```bash
cd Ordering.Api
dotnet ef migrations add InitialSagaSetup --context SagaDbContext
dotnet ef database update --context SagaDbContext
```

### Step 5: Run the Services

Open **two separate terminals**:

**Terminal 1 - Ordering.Api (Saga Orchestrator):**
```bash
cd Ordering.Api
dotnet run
```

**Terminal 2 - Inventory.Worker (Participant):**
```bash
cd Inventory.Worker
dotnet run
```

Both services should start successfully and connect to RabbitMQ.

## 🧪 Testing the Workflow

### Option 1: Using Swagger UI

1. Navigate to: `https://localhost:5001/swagger` (or the port shown in console)
2. Find the `POST /api/orders` endpoint
3. Click "Try it out"
4. Enter test data:
   ```json
   {
     "productId": "123e4567-e89b-12d3-a456-426614174000",
     "quantity": 5
   }
   ```
5. Click "Execute"

### Option 2: Using curl

```bash
curl -X POST https://localhost:5001/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "productId": "123e4567-e89b-12d3-a456-426614174000",
    "quantity": 5
  }'
```

### Option 3: Using PowerShell

```powershell
Invoke-RestMethod -Uri "https://localhost:5001/api/orders" \
  -Method Post \
  -ContentType "application/json" \
  -Body '{"productId":"123e4567-e89b-12d3-a456-426614174000","quantity":5}'
```

### Expected Behavior

**Success Path (odd-numbered ProductId hash):**
```
[Ordering.Api] [SAGA] Starting Order <guid>
[Inventory.Worker] [INVENTORY] Processing reservation for Order: <guid>
[Inventory.Worker] [INVENTORY] ✅ Success! Reserving inventory...
[Ordering.Api] [SAGA] Inventory Reserved for Order <guid>, ReservationId: <guid>
```

**Failure Path (even-numbered ProductId hash):**
```
[Ordering.Api] [SAGA] Starting Order <guid>
[Inventory.Worker] [INVENTORY] Processing reservation for Order: <guid>
[Inventory.Worker] [INVENTORY] ❌ Failed! Insufficient stock...
[Ordering.Api] [SAGA] Inventory Rejected for Order <guid>: Insufficient stock...
```

**Timeout Path (stop Inventory.Worker before submitting order):**
```
[Ordering.Api] [SAGA] Starting Order <guid>
[Ordering.Api] [SAGA] TIMEOUT! Order <guid> took too long.
```

## 🔍 Understanding the Code

### 1. Contracts (Shared Messages)

Located in `Contracts/Class1.cs`:

- **SubmitOrder** - Initial event that starts the saga
- **ReserveInventoryCommand** - Command sent from saga to inventory service
- **InventoryReservedEvent** - Success response from inventory
- **InventoryRejectedEvent** - Failure response from inventory
- **OrderCompletedEvent** - Published by saga on success
- **OrderFailedEvent** - Published by saga on failure/timeout

### 2. Saga State

Located in `Ordering.Api/Sagas/OrderInventoryState.cs`:

Implements `ISaga` and `SagaStateMachineInstance` for persistence. Contains:
- Business data (OrderId, ProductId, Quantity)
- State-specific data (ReservationId, FailureReason)
- Timestamps for auditing

### 3. State Machine

Located in `Ordering.Api/Sagas/OrderInventoryStateMachine.cs`:

Defines the workflow logic with:
- **States**: Initially, AwaitingInventory, Completed, Failed
- **Events**: OrderSubmitted, InventoryReserved, InventoryRejected, OrderTimeoutExpired
- **Transitions**: How the saga moves between states
- **Timeout**: 2-minute timer that auto-fails if no response
- **Compensation**: Publishes failure events for upstream services
- **Idempotency**: Handles duplicate messages in all states

### 4. EF Core DbContext

Located in `Ordering.Api/Data/SagaDbContext.cs`:

Configures:
- Saga state table mapping
- Inbox table (tracks consumed messages)
- OutboxMessage table (stores outgoing messages)
- OutboxState table (tracks outbox processing)

### 5. MassTransit Configuration

Located in `Ordering.Api/Program.cs`:

Key features:
- **EntityFrameworkRepository** - Persists saga state to SQL Server
- **SqlServerLockStatementProvider** - Prevents deadlocks with UPDLOCK, ROWLOCK
- **UseEntityFrameworkOutbox** - Enables transactional message processing
- **DuplicateDetectionWindow** - 5-minute window for duplicate detection

### 6. Inventory Consumer

Located in `Inventory.Worker/Consumers/ReserveInventoryConsumer.cs`:

Processes `ReserveInventoryCommand` and publishes:
- `InventoryReservedEvent` on success
- `InventoryRejectedEvent` on failure

Simulates business logic with random success/failure based on ProductId hash.

## 🛡️ Why This Architecture is Bulletproof

### The Inbox Pattern
When `SubmitOrder` arrives:
1. MassTransit starts a DB transaction
2. Marks the message as "consumed" (Inbox)
3. Updates the Saga state to AwaitingInventory
4. Writes the outgoing `ReserveInventoryCommand` to OutboxMessage table
5. **All in one transaction** - if the app crashes, nothing is lost

### The Outbox Pattern
A background MassTransit thread:
- Continuously polls the OutboxMessage table
- Pushes messages to RabbitMQ
- Even if RabbitMQ is down, messages are safely stored in SQL Server
- Guaranteed delivery when RabbitMQ comes back online

### Saga State Idempotency
Your `During(Completed, When(OrderSubmitted))` logic ensures:
- If RabbitMQ redelivers messages (network blip), the saga ignores them
- No duplicate orders created
- No state corruption

### SQL Server Lock Provider
`SqlServerLockStatementProvider` uses:
- `UPDLOCK` - Update lock prevents other transactions from modifying the row
- `ROWLOCK` - Row-level locking (not table-level)
- Prevents two threads from corrupting saga state simultaneously

## 📊 Monitoring & Observability

### Check Saga State in Database

```sql
-- View all saga instances
SELECT CorrelationId, CurrentState, OrderId, ProductId, Quantity, 
       ReservationId, FailureReason, CreatedAt, UpdatedAt
FROM OrderInventoryStates
ORDER BY CreatedAt DESC;

-- View failed sagas
SELECT * FROM OrderInventoryStates 
WHERE CurrentState = 'Failed';

-- View completed sagas
SELECT * FROM OrderInventoryStates 
WHERE CurrentState = 'Completed';
```

### Check Outbox/Inbox Tables

```sql
-- View pending outbox messages (should be empty after processing)
SELECT * FROM OutboxMessage 
ORDER BY SequenceNumber DESC;

-- View inbox state (consumed messages)
SELECT * FROM InboxState 
ORDER BY Created DESC;
```

### RabbitMQ Management UI

Navigate to `http://localhost:15672` (guest/guest):
- View queues: `order-saga`, `inventory-reserve`
- Monitor message rates
- Check for dead-letter messages

## 🔧 Troubleshooting

### Issue: Cannot connect to RabbitMQ

**Solution:**
```bash
# Check if RabbitMQ is running
docker ps | grep rabbitmq

# Start RabbitMQ if not running
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

### Issue: Cannot connect to SQL Server

**Solution:**
- Verify SQL Server is running
- Check connection string in `appsettings.json`
- Try connecting with SSMS or Azure Data Studio
- For LocalDB: `sqllocaldb info` to verify instance exists

### Issue: Database migrations fail

**Solution:**
```bash
# Delete existing database and recreate
cd Ordering.Api
dotnet ef database drop --context SagaDbContext
dotnet ef database update --context SagaDbContext
```

### Issue: Messages not being processed

**Check:**
1. Both services are running
2. RabbitMQ is accessible
3. Queue names match (`order-saga`, `inventory-reserve`)
4. Check logs for errors

### Issue: Timeout not triggering

**Solution:**
- Wait at least 2 minutes after submitting order
- Check that `Inventory.Worker` is NOT running (to simulate failure)
- Verify timeout configuration in state machine

## 🎓 Learning Resources

### MassTransit Documentation
- [State Machines](https://masstransit.io/documentation/patterns/state-machine)
- [Entity Framework Core Integration](https://masstransit.io/documentation/persistence/efcore)
- [Outbox Pattern](https://masstransit.io/documentation/patterns/outbox)
- [Saga Patterns](https://masstransit.io/documentation/patterns/saga)

### Related Patterns
- **Choreography vs Orchestration**: This demo uses orchestration (saga coordinates the workflow)
- **CQRS**: Commands and queries are separated
- **Event-Driven Architecture**: Services communicate via events

## 📝 Next Steps

### Enhancements to Consider

1. **Add Payment Service** - Extend saga to include payment processing
2. **Add Shipping Service** - Complete the order fulfillment workflow
3. **Add Retry Policies** - Implement exponential backoff for transient failures
4. **Add Dead-Letter Queue** - Handle poison messages
5. **Add Metrics** - Integrate Application Insights or Prometheus
6. **Add Distributed Tracing** - Use OpenTelemetry for end-to-end tracing
7. **Add Unit Tests** - Test state machine transitions
8. **Add Integration Tests** - Test the full workflow with TestContainersend

### Production Deployment

1. **Containerize** - Create Dockerfiles for each service
2. **Kubernetes** - Deploy to K8s with Helm charts
3. **Configuration** - Use environment variables or Azure App Configuration
4. **Secrets Management** - Use Azure Key Vault or HashiCorp Vault
5. **Health Checks** - Add proper health check endpoints
6. **Logging** - Centralized logging with Serilog + Seq/Elasticsearch
7. **Monitoring** - Application Insights, Datadog, or New Relic

## 🤝 Contributing

Feel free to submit issues, fork the repository, and create pull requests for any improvements.

## 📄 License

This project is for educational purposes. Feel free to use it as a template for your own projects.

---

**Happy Coding! 🚀**
