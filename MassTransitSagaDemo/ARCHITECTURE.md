# Architecture Deep Dive

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Client Applications                              │
│  (Web App, Mobile App, API Consumers, Test Scripts)                     │
└──────────────────────┬──────────────────────────────────────────────────┘
                       │ HTTP POST /api/orders
                       ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                     Ordering.Api (Saga Orchestrator)                     │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                    ASP.NET Core Web API                         │    │
│  │  - Controllers (OrdersController)                              │    │
│  │  - Swagger/OpenAPI Documentation                               │    │
│  └────────────────────┬───────────────────────────────────────────┘    │
│                       │                                                 │
│  ┌────────────────────▼───────────────────────────────────────────┐    │
│  │                   MassTransit Bus                               │    │
│  │  - PublishEndpoint                                             │    │
│  │  - ReceiveEndpoint (order-saga)                                │    │
│  │  - EntityFramework Outbox/Inbox                                │    │
│  └────────────────────┬───────────────────────────────────────────┘    │
│                       │                                                 │
│  ┌────────────────────▼───────────────────────────────────────────┐    │
│  │              OrderInventoryStateMachine                         │    │
│  │                                                                 │    │
│  │  States:                                                        │    │
│  │  ├─ Initially                                                   │    │
│  │  ├─ AwaitingInventory                                           │    │
│  │  ├─ Completed ✅                                                │    │
│  │  └─ Failed ❌                                                   │    │
│  │                                                                 │    │
│  │  Events:                                                        │    │
│  │  ├─ SubmitOrder (correlate by OrderId)                         │    │
│  │  ├─ InventoryReservedEvent                                      │    │
│  │  ├─ InventoryRejectedEvent                                      │    │
│  │  └─ OrderTimeoutExpired (2 min timeout)                        │    │
│  └────────────────────┬───────────────────────────────────────────┘    │
│                       │                                                 │
│  ┌────────────────────▼───────────────────────────────────────────┐    │
│  │              Entity Framework Core DbContext                    │    │
│  │  - SagaDbContext                                               │    │
│  │  - Tables:                                                     │    │
│  │    ├─ OrderInventoryStates (saga state)                        │    │
│  │    ├─ InboxState (consumed messages)                           │    │
│  │    ├─ OutboxMessage (pending outgoing messages)                │    │
│  │    └─ OutboxState (outbox processing status)                   │    │
│  └────────────────────┬───────────────────────────────────────────┘    │
└───────────────────────┼───────────────────────────────────────────────┘
                        │ SQL Server (ADO.NET)
                        ▼
            ┌──────────────────────────┐
            │     SQL Server DB        │
            │  (OrderingSagaDb)        │
            └──────────────────────────┘

                        │ RabbitMQ (AMQP)
                        ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        RabbitMQ Broker                                   │
│                                                                          │
│  Queues:                                                                 │
│  ├─ order-saga (consumed by Ordering.Api saga)                          │
│  └─ inventory-reserve (consumed by Inventory.Worker)                    │
│                                                                          │
│  Exchanges:                                                              │
│  └─ masstransit (topic exchange for events)                             │
└───────────────────────┬─────────────────────────────────────────────────┘
                        │ AMQP Protocol
                        ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                  Inventory.Worker (Participant)                          │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                   MassTransit Consumer                         │    │
│  │  - ReserveInventoryConsumer                                    │    │
│  │  - Listens on: queue:inventory-reserve                         │    │
│  └────────────────────┬───────────────────────────────────────────┘    │
│                       │                                                 │
│  ┌────────────────────▼───────────────────────────────────────────┐    │
│  │                 Business Logic                                  │    │
│  │  - Check inventory availability                                 │    │
│  │  - Reserve stock                                                │    │
│  │  - Publish response events:                                     │    │
│  │    ├─ InventoryReservedEvent (success)                         │    │
│  │    └─ InventoryRejectedEvent (failure)                         │    │
│  └────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────┘
```

## Message Flow Sequence

### Success Path

```
Client          Ordering.Api         RabbitMQ        Inventory.Worker      SQL Server
  │                  │                   │                  │                  │
  │──POST /orders──▶│                   │                  │                  │
  │                  │                   │                  │                  │
  │                  │──Publish────────▶│                  │                  │
  │                  │  SubmitOrder      │                  │                  │
  │                  │                   │                  │                  │
  │                  │◀──Consume────────│                  │                  │
  │                  │  (order-saga)     │                  │                  │
  │                  │                   │                  │                  │
  │                  │──BEGIN TXN───────┼──────────────────┼─────────────────▶│
  │                  │                   │                  │                  │
  │                  │──Mark Inbox──────┼──────────────────┼─────────────────▶│
  │                  │  (message ID)     │                  │                  │
  │                  │                   │                  │                  │
  │                  │──Update Saga─────┼──────────────────┼─────────────────▶│
  │                  │  State:           │                  │                  │
  │                  │  AwaitingInventory│                  │                  │
  │                  │                   │                  │                  │
  │                  │──Write Outbox────┼──────────────────┼─────────────────▶│
  │                  │  (ReserveInv Cmd) │                  │                  │
  │                  │                   │                  │                  │
  │                  │──COMMIT TXN──────┼──────────────────┼─────────────────▶│
  │                  │                   │                  │                  │
  │                  │──Send───────────▶│                  │                  │
  │                  │  (from outbox)    │                  │                  │
  │                  │                   │                  │                  │
  │                  │                   │──Deliver────────▶│                  │
  │                  │                   │  ReserveInv Cmd   │                  │
  │                  │                   │                  │                  │
  │                  │                   │                  │──Process────────│
  │                  │                   │                  │  (check stock)   │
  │                  │                   │                  │                  │
  │                  │                   │◀──Publish────────│                  │
  │                  │                   │  InventoryReserved│                  │
  │                  │                   │                  │                  │
  │                  │◀──Consume────────│                  │                  │
  │                  │  (order-saga)     │                  │                  │
  │                  │                   │                  │                  │
  │                  │──BEGIN TXN───────┼──────────────────┼─────────────────▶│
  │                  │                   │                  │                  │
  │                  │──Update Saga─────┼──────────────────┼─────────────────▶│
  │                  │  State: Completed │                  │                  │
  │                  │  ReservationId    │                  │                  │
  │                  │                   │                  │                  │
  │                  │──COMMIT TXN──────┼──────────────────┼─────────────────▶│
  │                  │                   │                  │                  │
  │                  │──Publish────────▶│                  │                  │
  │                  │  OrderCompleted   │                  │                  │
  │                  │                   │                  │                  │
  │◀──202 Accepted───│                   │                  │                  │
  │                  │                   │                  │                  │
```

### Timeout Path (Failure)

```
Client          Ordering.Api         RabbitMQ        Inventory.Worker      SQL Server
  │                  │                   │                  │                  │
  │──POST /orders──▶│                   │                  │                  │
  │                  │                   │                  │                  │
  │                  │──Publish────────▶│                  │                  │
  │                  │  SubmitOrder      │                  │                  │
  │                  │                   │                  │                  │
  │                  │◀──Consume────────│                  │                  │
  │                  │  (order-saga)     │                  │                  │
  │                  │                   │                  │                  │
  │                  │──Update Saga─────┼──────────────────┼─────────────────▶│
  │                  │  State:           │                  │                  │
  │                  │  AwaitingInventory│                  │                  │
  │                  │  Start Timer (2m) │                  │                  │
  │                  │                   │                  │                  │
  │                  │──Send───────────▶│                  │                  │
  │                  │  ReserveInv Cmd   │                  │                  │
  │                  │                   │                  │                  │
  │                  │                   │  [Worker Down]   │                  │
  │                  │                   │  or Network Issue│                  │
  │                  │                   │                  │                  │
  │                  │                   │                  │                  │
  │                  │◀──Timer Expires──│                  │                  │
  │                  │  (2 minutes)      │                  │                  │
  │                  │                   │                  │                  │
  │                  │──Update Saga─────┼──────────────────┼─────────────────▶│
  │                  │  State: Failed    │                  │                  │
  │                  │  Reason: Timeout  │                  │                  │
  │                  │                   │                  │                  │
  │                  │──Publish────────▶│                  │                  │
  │                  │  OrderFailed      │                  │                  │
  │                  │                   │                  │                  │
  │◀──202 Accepted───│                   │                  │                  │
```

## Key Design Patterns

### 1. Saga Orchestration Pattern

The `OrderInventoryStateMachine` acts as the **orchestrator** that coordinates the distributed workflow:

- **Centralized Control**: The saga knows the entire workflow and manages state transitions
- **Compensation**: On failure, it publishes events to trigger compensating actions
- **Timeout Handling**: Automatically fails if participants don't respond in time

**vs. Choreography:**
- Choreography: Each service reacts to events independently (no central coordinator)
- Orchestration: Saga coordinates the workflow (what we're using)

### 2. Outbox Pattern

**Problem:** How to ensure atomicity between database updates and message publishing?

**Solution:** Write messages to an Outbox table in the same transaction as business data.

```csharp
// In the same DB transaction:
// 1. Update saga state
// 2. Write outgoing message to OutboxMessage table
// 3. Commit transaction

// Background thread:
// 4. Poll OutboxMessage table
// 5. Publish messages to RabbitMQ
// 6. Mark as sent
```

**Benefits:**
- ✅ Guaranteed delivery (messages survive crashes)
- ✅ Exactly-once semantics (with deduplication)
- ✅ Transactional consistency

### 3. Inbox Pattern

**Problem:** How to prevent duplicate message processing?

**Solution:** Track consumed message IDs in an Inbox table.

```csharp
// Before processing:
// 1. Check if MessageId exists in InboxState table
// 2. If exists, skip (already processed)
// 3. If not, process and insert into InboxState
```

**Benefits:**
- ✅ Idempotency (duplicate messages ignored)
- ✅ Crash recovery (can safely reprocess after restart)

### 4. Correlation by ID

All messages for the same order use `OrderId` as the correlation ID:

```csharp
Event(() => OrderSubmitted, e =>
{
    e.CorrelateById(m => m.Message.OrderId);  // ← This line!
});
```

**How it works:**
1. First `SubmitOrder` with OrderId=123 → Creates new saga instance
2. Subsequent messages with OrderId=123 → Route to same saga instance
3. Messages with OrderId=456 → Create different saga instance

### 5. State Machine Transitions

```
Initially
    │
    ├──When(SubmitOrder)
    │   ├── Send ReserveInventoryCommand
    │   ├── Schedule Timeout (2 min)
    │   └── TransitionTo(AwaitingInventory)
    │
AwaitingInventory
    │
    ├──When(InventoryReserved)
    │   ├── Unschedule Timeout
    │   ├── Publish OrderCompletedEvent
    │   └── TransitionTo(Completed)
    │
    ├──When(InventoryRejected)
    │   ├── Unschedule Timeout
    │   ├── Publish OrderFailedEvent
    │   └── TransitionTo(Failed)
    │
    └──When(OrderTimeoutExpired)
        ├── Publish OrderFailedEvent
        └── TransitionTo(Failed)
```

## Database Schema

### OrderInventoryStates Table

```sql
CREATE TABLE OrderInventoryStates (
    CorrelationId UNIQUEIDENTIFIER PRIMARY KEY,
    CurrentState NVARCHAR(50) NOT NULL,
    OrderId UNIQUEIDENTIFIER NOT NULL,
    ProductId UNIQUEIDENTIFIER NOT NULL,
    Quantity INT NOT NULL,
    ReservationId UNIQUEIDENTIFIER NULL,
    FailureReason NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NULL,
    
    -- Indexes for performance
    INDEX IX_OrderId NONCLUSTERED (OrderId),
    INDEX IX_CurrentState NONCLUSTERED (CurrentState)
);
```

### InboxState Table

```sql
CREATE TABLE InboxState (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    MessageId UNIQUEIDENTIFIER NOT NULL,
    ConsumerId UNIQUEIDENTIFIER NOT NULL,
    LockId UNIQUEIDENTIFIER NULL,
    RowVersion ROWVERSION,
    Created DATETIME2 NOT NULL,
    
    UNIQUE INDEX IX_MessageId_ConsumerId (MessageId, ConsumerId)
);
```

### OutboxMessage Table

```sql
CREATE TABLE OutboxMessage (
    SequenceNumber BIGINT IDENTITY(1,1) PRIMARY KEY,
    MessageId UNIQUEIDENTIFIER NOT NULL,
    DestinationAddress NVARCHAR(500) NOT NULL,
    Headers NVARCHAR(MAX) NULL,
    Body NVARCHAR(MAX) NOT NULL,
    ContentType NVARCHAR(200) NOT NULL,
    ConversationId UNIQUEIDENTIFIER NULL,
    CorrelationId UNIQUEIDENTIFIER NULL,
    InitiatorId UNIQUEIDENTIFIER NULL,
    RequestId UNIQUEIDENTIFIER NULL,
    SourceAddress NVARCHAR(500) NULL,
    CreationTime DATETIME2 NOT NULL,
    ExpirationTime DATETIME2 NULL,
    LockId UNIQUEIDENTIFIER NULL,
    RowVersion ROWVERSION,
    
    INDEX IX_CreationTime NONCLUSTERED (CreationTime)
);
```

## Concurrency Control

### SQL Server Lock Provider

MassTransit uses `SqlServerLockStatementProvider` which generates SQL like:

```sql
SELECT CorrelationId, CurrentState, ...
FROM OrderInventoryStates WITH (UPDLOCK, ROWLOCK)
WHERE CorrelationId = @CorrelationId;
```

**Lock Types:**
- **UPDLOCK**: Update lock - prevents other transactions from acquiring update/exclusive locks
- **ROWLOCK**: Row-level lock - doesn't lock the entire table

**Why this matters:**
- Prevents two threads from updating the same saga simultaneously
- Avoids race conditions and state corruption
- Maintains data integrity under high concurrency

## Performance Considerations

### 1. Indexing Strategy

```sql
-- Critical indexes for saga performance
CREATE INDEX IX_CorrelationId ON OrderInventoryStates(CorrelationId);
CREATE INDEX IX_CurrentState ON OrderInventoryStates(CurrentState);
CREATE INDEX IX_CreatedAt ON OrderInventoryStates(CreatedAt);
```

### 2. Connection Pooling

EF Core automatically pools SQL connections. Ensure your connection string includes:

```
Max Pool Size=100;Min Pool Size=5;
```

### 3. Message Prefetch Count

RabbitMQ prefetch count controls how many messages are fetched at once:

```csharp
e.PrefetchCount = 16; // Default is 16
```

Adjust based on:
- Number of concurrent consumers
- Message processing time
- Memory constraints

### 4. Batch Processing

For high-volume scenarios, consider batching:

```csharp
// Process multiple sagas in parallel
Parallel.ForEach(sagas, saga => {
    // Process saga
});
```

## Security Considerations

### 1. Connection Strings

**Never hardcode credentials!** Use:
- Environment variables
- Azure Key Vault
- HashiCorp Vault
- Docker secrets

```csharp
// Bad ❌
"Server=localhost;Database=OrderingSagaDb;User=sa;Password=Secret123;"

// Good ✅
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
```

### 2. RabbitMQ Credentials

Change default credentials in production:

```yaml
# docker-compose.yml
environment:
  RABBITMQ_DEFAULT_USER: masstransit_user
  RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASSWORD}  # From .env file
```

### 3. HTTPS/TLS

Always use HTTPS in production:

```csharp
app.UseHttpsRedirection();
```

Configure TLS for RabbitMQ:

```yaml
rabbitmq:
  ports:
    - "5671:5671"  # TLS port
```

## Monitoring & Observability

### 1. Logging

MassTransit provides detailed logs:

```json
{
  "Logging": {
    "LogLevel": {
      "MassTransit": "Debug",
      "MassTransit.Saga": "Information",
      "MassTransit.Outbox": "Warning"
    }
  }
}
```

### 2. Metrics

Track key metrics:
- Saga creation rate
- Saga completion rate
- Saga failure rate
- Average processing time
- Timeout rate
- Outbox message lag

### 3. Distributed Tracing

Enable OpenTelemetry:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("MassTransit")
        .AddAspNetCoreInstrumentation());
```

### 4. Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString)
    .AddRabbitMQ(rabbitMqUri);
```

## Testing Strategies

### 1. Unit Testing State Machine

```csharp
[Test]
public async Task Should_transition_to_awaiting_inventory_on_submit()
{
    var harness = new InMemoryTestHarness();
    var sagaHarness = harness.StateMachineSaga<OrderInventoryState, OrderInventoryStateMachine>();
    
    await harness.Start();
    try
    {
        await harness.Bus.Publish(new SubmitOrder(orderId, productId, quantity));
        
        Assert.That(await sagaHarness.Created.Any(), Is.True);
        Assert.That(await sagaHarness.Completed.Any(), Is.False);
        Assert.That(sagaHarness.Sagas.First().CurrentState, 
            Is.EqualTo("AwaitingInventory"));
    }
    finally
    {
        await harness.Stop();
    }
}
```

### 2. Integration Testing

Use Test Containers:

```csharp
[Fact]
public async Task Full_workflow_should_complete_successfully()
{
    await using var rabbitMqContainer = new RabbitMqContainer();
    await using var sqlContainer = new SqlServerContainer();
    
    await rabbitMqContainer.StartAsync();
    await sqlContainer.StartAsync();
    
    // Configure services with container connection strings
    // Run full workflow
    // Assert final state
}
```

### 3. Load Testing

Use tools like:
- k6
- Apache JMeter
- NBomber

Test scenarios:
- 100 orders/second
- 1000 concurrent sagas
- Timeout handling under load

---

**This architecture is designed for production-scale distributed systems! 🚀**
