# 🎓 Saga Pattern Implementation - Complete Beginner's Guide

> **Project**: WPF Prism EventAggregator Demo  
> **Pattern**: Choreography-based Saga Pattern with Compensating Transactions  
> **Purpose**: Learning resource for understanding distributed transaction management

---

## 📋 Table of Contents

1. [What is the Saga Pattern?](#1-what-is-the-saga-pattern)
2. [Architecture Overview](#2-architecture-overview)
3. [Implementation Walkthrough](#3-implementation-walkthrough)
4. [Event Flow & Sequence](#4-event-flow--sequence)
5. [Success vs Failure Paths](#5-success-vs-failure-paths)
6. [Interview Preparation](#6-interview-preparation)
7. [Key Design Patterns](#7-key-design-patterns)
8. [Code Reference](#8-code-reference)

---

## 1. What is the Saga Pattern?

The **Saga Pattern** is a design pattern used to manage distributed transactions across multiple services. Instead of using traditional database transactions (which lock resources), it uses a sequence of **local transactions** where each service publishes events to trigger the next step.

### Key Concepts:

- **Forward Flow**: Each step completes successfully and triggers the next
- **Compensating Transactions**: If any step fails, previous steps are "undone" through rollback actions
- **Event-Driven**: Services communicate via events, not direct calls
- **Eventually Consistent**: Data consistency is achieved over time, not immediately

### Why Use Saga Pattern?

Traditional ACID transactions work within a single database, but in microservices or distributed systems:
- Each service has its own database
- Distributed locks hurt performance and scalability
- Network failures can leave data in inconsistent states

The Saga Pattern solves these problems by breaking large transactions into smaller, manageable steps with built-in rollback mechanisms.

---

## 2. Architecture Overview

This application simulates an **Order Processing System** with the following workflow:

```
┌─────────────┐
│ User Places │
│   Order     │
└──────┬──────┘
       │
       ▼
┌──────────────────┐
│ Inventory Service│ ← Step 1: Reserve Stock
│  (Reserves Item) │
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│ Payment Service  │ ← Step 2: Process Payment
│ (Charges Card)   │
└──────┬───────────┘
       │
       ├────────────────┐
       │                │
       ▼                ▼
  ┌────────┐      ┌──────────┐
  │SUCCESS │      │ FAILURE  │
  │   ✅   │      │    ❌    │
  └────────┘      └────┬─────┘
                       │
                       ▼
              ┌─────────────────┐
              │Inventory Service│ ← Compensating Transaction
              │ (Release Stock) │    (Rollback Step 1)
              └─────────────────┘
```

### Components:

| Component | Role | Location |
|-----------|------|----------|
| **OrderViewModel** | Saga Initiator | `ViewModels/OrderViewModel.cs` |
| **InventoryService** | Step 1 + Rollback Handler | `Services/InventoryService.cs` |
| **PaymentService** | Step 2 Processor | `Services/PaymentService.cs` |
| **AuditService** | Legacy observer (not part of saga) | `Services/AuditService.cs` |
| **EventAggregator** | Message bus for event communication | Provided by Prism |

---

## 3. Implementation Walkthrough

### A. Event Chain (The Backbone)

The saga is orchestrated through a chain of events defined in the `Events/` folder:

| Event | Purpose | Payload Properties | Triggered By |
|-------|---------|-------------------|--------------|
| `OrderCreatedEvent` | Triggers the saga | OrderId, OrderName | OrderViewModel |
| `InventoryReservedEvent` | Inventory reserved successfully | OrderId, ReservedItemsCount | InventoryService |
| `PaymentProcessedEvent` | Payment successful (END) | OrderId, TransactionId | PaymentService |
| `PaymentFailedEvent` | Payment failed (TRIGGER ROLLBACK) | OrderId, Reason | PaymentService |
| `OrderFailedEvent` | Order completely failed (END) | OrderId, Reason | InventoryService |

### B. The Three Main Actors

#### 1. OrderViewModel (Saga Initiator)

**File**: `ViewModels/OrderViewModel.cs`

**Responsibility**: Starts the saga when user places an order

```csharp
private void PlaceOrder()
{
    // 1. Generate unique order ID
    int id = _orderCounter++;
    
    // 2. Publish OrderCreatedEvent - THIS STARTS THE SAGA!
    _eventAggregator.GetEvent<OrderCreatedEvent>().Publish(new OrderCreatedPayload
    {
        OrderId = id,
        OrderName = OrderName
    });
}
```

**Key Points**:
- Creates a unique order ID
- Publishes the initial event to kick off the workflow
- Subscribes to `TransactionLogEvent` to display logs in UI

---

#### 2. InventoryService (Step 1 + Rollback Handler)

**File**: `Services/InventoryService.cs`

**Responsibilities**:
1. Handle Step 1: Reserve inventory when order is created
2. Handle Rollback: Release inventory when payment fails

```csharp
public InventoryService(IEventAggregator eventAggregator)
{
    // Subscribe to Step 1: Listen for new orders
    _eventAggregator.GetEvent<OrderCreatedEvent>().Subscribe(OnOrderCreated);
    
    // Subscribe to Rollback: Listen for payment failures
    _eventAggregator.GetEvent<PaymentFailedEvent>().Subscribe(OnPaymentFailed);
}

// FORWARD TRANSACTION - Step 1
private void OnOrderCreated(OrderCreatedPayload payload)
{
    Log($"Order {payload.OrderId}: Received. Attempting to reserve stock...", "INFO");
    
    // Simulate stock reservation (always succeeds in demo)
    Log($"Order {payload.OrderId}: Stock successfully RESERVED", "SUCCESS");
    
    // Trigger next step in saga
    _eventAggregator.GetEvent<InventoryReservedEvent>().Publish(new InventoryReservedPayload
    {
        OrderId = payload.OrderId,
        ReservedItemsCount = 1
    });
}

// COMPENSATING TRANSACTION - Rollback
private void OnPaymentFailed(PaymentFailedPayload payload)
{
    Log($"Order {payload.OrderId}: Rollback signal received (Reason: {payload.Reason})", "WARNING");
    
    // Execute Compensating Transaction: Release reserved inventory
    Log($"Order {payload.OrderId}: [COMPENSATING TRANSACTION] Releasing stock...", "ROLLBACK");
    Log($"Order {payload.OrderId}: Stock successfully RELEASED. Rollback complete.", "ROLLBACK");

    // Notify that order has completely failed
    _eventAggregator.GetEvent<OrderFailedEvent>().Publish(new OrderFailedPayload
    {
        OrderId = payload.OrderId,
        Reason = payload.Reason
    });
}
```

**Key Points**:
- Subscribes to TWO events: one for forward flow, one for rollback
- Executes compensating transaction when payment fails
- Ensures no "orphaned" inventory reservations

---

#### 3. PaymentService (Step 2)

**File**: `Services/PaymentService.cs`

**Responsibility**: Process payment and determine success/failure path

```csharp
// Static flag controlled by UI to simulate failure scenario
public static bool SimulateFailure { get; set; } = false;

public PaymentService(IEventAggregator eventAggregator)
{
    // Subscribe to Step 2: Listen for inventory reservation
    _eventAggregator.GetEvent<InventoryReservedEvent>().Subscribe(OnInventoryReserved);
}

private void OnInventoryReserved(InventoryReservedPayload payload)
{
    Log($"Order {payload.OrderId}: Received reservation. Processing payment...", "INFO");

    if (SimulateFailure)
    {
        // FAILURE PATH
        Log($"Order {payload.OrderId}: Payment processing FAILED (Simulated error)", "ERROR");
        
        // Trigger rollback by publishing failure event
        _eventAggregator.GetEvent<PaymentFailedEvent>().Publish(new PaymentFailedPayload
        {
            OrderId = payload.OrderId,
            Reason = "Declined - Insufficient funds / Simulated Error"
        });
    }
    else
    {
        // SUCCESS PATH
        string transactionId = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        Log($"Order {payload.OrderId}: Payment charged successfully. Txn: {transactionId}", "SUCCESS");

        // Publish success event - Saga completes
        _eventAggregator.GetEvent<PaymentProcessedEvent>().Publish(new PaymentProcessedPayload
        {
            OrderId = payload.OrderId,
            TransactionId = transactionId
        });
    }
}
```

**Key Points**:
- Can simulate failure via UI checkbox
- Publishes different events based on success/failure
- Generates unique transaction ID on success

---

## 4. Event Flow & Sequence

### Does It Execute Sequentially?

**YES**, but with important nuances:

#### Sequential Execution Timeline:

```
T0: User clicks "Place Order"
    ↓
T1: OrderViewModel publishes OrderCreatedEvent
    ↓
T2: InventoryService receives OrderCreatedEvent (synchronous)
    ↓
T3: InventoryService reserves stock
    ↓
T4: InventoryService publishes InventoryReservedEvent
    ↓
T5: PaymentService receives InventoryReservedEvent (synchronous)
    ↓
T6: PaymentService processes payment
    ↓
T7a: [SUCCESS] Publishes PaymentProcessedEvent → END
    OR
T7b: [FAILURE] Publishes PaymentFailedEvent
    ↓
T8: InventoryService receives PaymentFailedEvent (synchronous)
    ↓
T9: InventoryService executes compensating transaction
    ↓
T10: InventoryService publishes OrderFailedEvent → END
```

### How Sequence is Managed:

1. **Event Chain Pattern**: Each service only responds to specific events
   - `InventoryService` waits for `OrderCreatedEvent`
   - `PaymentService` waits for `InventoryReservedEvent`
   - This creates an implicit sequence without explicit orchestration

2. **No Parallel Execution**: Services don't act until they receive their trigger event

3. **Prism EventAggregator Behavior**: 
   - By default, events execute **synchronously** on the publisher's thread
   - Unless you specify `ThreadOption`, execution is immediate and sequential
   - In `OrderViewModel`, logs subscribe with `ThreadOption.UIThread` to safely update UI

4. **Subscription Registration**: Services subscribe to events in their constructors, ensuring they're ready before any events are published

---

## 5. Success vs Failure Paths

### Flow A: Success Path (Happy Path) ✅

```
1. OrderViewModel → publishes OrderCreatedEvent
2. InventoryService → receives event → reserves stock → publishes InventoryReservedEvent
3. PaymentService → receives event → charges payment → publishes PaymentProcessedEvent
4. DONE: Order completed successfully
```

**Expected Log Output:**
```
ℹ️ 12:30:45 - [OrderViewModel] Order 1 (Premium Item) placed by user
ℹ️ 12:30:45 - [InventoryService] Order 1: Received. Attempting to reserve stock...
✅ 12:30:45 - [InventoryService] Order 1: Stock successfully RESERVED for items
ℹ️ 12:30:45 - [PaymentService] Order 1: Received reservation. Processing payment...
✅ 12:30:45 - [PaymentService] Order 1: Payment charged successfully. Txn: TXN-ABC12345
```

**State Changes:**
- Inventory: Item reserved ✅
- Payment: Charged ✅
- Order Status: Completed ✅

---

### Flow B: Failure Path (Rollback) 🔄

```
1. OrderViewModel → publishes OrderCreatedEvent
2. InventoryService → receives event → reserves stock → publishes InventoryReservedEvent
3. PaymentService → receives event → payment FAILS → publishes PaymentFailedEvent
4. InventoryService → receives failure event → releases stock → publishes OrderFailedEvent
5. DONE: Order rolled back completely (as if it never happened)
```

**Expected Log Output:**
```
ℹ️ 12:31:10 - [OrderViewModel] Order 2 (Premium Item) placed by user
ℹ️ 12:31:10 - [InventoryService] Order 2: Received. Attempting to reserve stock...
✅ 12:31:10 - [InventoryService] Order 2: Stock successfully RESERVED for items
ℹ️ 12:31:10 - [PaymentService] Order 2: Received reservation. Processing payment...
❌ 12:31:10 - [PaymentService] Order 2: Payment processing FAILED (Simulated error)
⚠️ 12:31:10 - [InventoryService] Order 2: Rollback signal received (Reason: Declined...)
🔄 12:31:10 - [InventoryService] Order 2: [COMPENSATING TRANSACTION] Releasing stock...
🔄 12:31:10 - [InventoryService] Order 2: Stock successfully RELEASED. Rollback complete
```

**State Changes:**
- Inventory: Reserved → Released (back to original state) 🔄
- Payment: Not charged ❌
- Order Status: Failed ❌

---

## 6. Interview Preparation

### Q1: Why use Saga Pattern instead of traditional transactions?

**Answer:**
> "Traditional ACID transactions work within a single database, but in microservices or distributed systems, each service has its own database. The Saga Pattern allows us to maintain data consistency across services without distributed locks, which would hurt performance and scalability. It provides eventual consistency while keeping services loosely coupled."

**Key Points to Mention:**
- Microservices architecture
- Database per service pattern
- Avoids distributed locking
- Better scalability

---

### Q2: How does this implementation ensure data consistency?

**Answer:**
> "It uses **compensating transactions**. If Step 2 (Payment) fails, the InventoryService listens for the `PaymentFailedEvent` and executes a compensating action to release the reserved stock. This ensures we never have 'orphaned' reservations where inventory is held but payment didn't go through."

**Key Points to Mention:**
- Compensating transactions undo previous steps
- Each service is responsible for its own rollback
- No partial commits remain

---

### Q3: What are the trade-offs of this approach?

**Answer:**

**Advantages:**
- ✅ Loose coupling between services
- ✅ Scalable and resilient
- ✅ Each service owns its data
- ✅ Easy to add new steps without modifying existing ones
- ✅ No single point of failure

**Challenges:**
- ⚠️ Eventual consistency (not immediate)
- ⚠️ Complex error handling and rollback logic
- ⚠️ Need to design compensating transactions carefully
- ⚠️ Harder to debug due to asynchronous nature
- ⚠️ No atomic rollback (each step rolls back independently)

---

### Q4: Is this orchestration or choreography?

**Answer:**
> "This is **Choreography-based Saga**. Each service knows what to do when it receives an event, and there's no central coordinator. The alternative is **Orchestration**, where a central saga manager tells each service what to do. Choreography is simpler for small workflows but can become hard to manage with many steps or complex branching logic."

**Comparison:**

| Aspect | Choreography (This Demo) | Orchestration |
|--------|-------------------------|---------------|
| Coordinator | None (decentralized) | Central saga manager |
| Complexity | Simple for few steps | Better for complex workflows |
| Coupling | Very loose | Slightly tighter (to orchestrator) |
| Visibility | Hard to see full flow | Easy to monitor entire saga |
| Maintenance | Can get messy with many steps | Easier to modify workflow |

---

### Q5: How would you add a third step (e.g., Shipping)?

**Answer:**
> "I would:
> 1. Create a `ShippingService` that subscribes to `PaymentProcessedEvent`
> 2. After shipping is arranged, publish `ShippingCompletedEvent`
> 3. Add rollback logic in `ShippingService` to cancel shipping if needed
> 4. Subscribe to relevant failure events to trigger compensation
> 
> The beauty of choreography is that existing services don't need modification - just add the new service and it plugs into the event chain!"

**Code Example:**
```csharp
public class ShippingService
{
    public ShippingService(IEventAggregator eventAggregator)
    {
        // Subscribe to payment success
        eventAggregator.GetEvent<PaymentProcessedEvent>().Subscribe(OnPaymentProcessed);
        
        // Subscribe to failures for rollback
        eventAggregator.GetEvent<OrderFailedEvent>().Subscribe(OnOrderFailed);
    }
    
    private void OnPaymentProcessed(PaymentProcessedPayload payload)
    {
        // Arrange shipping
        // Publish ShippingCompletedEvent
    }
    
    private void OnOrderFailed(OrderFailedPayload payload)
    {
        // Cancel shipping arrangement (compensating transaction)
    }
}
```

---

### Q6: What happens if an event is lost or a service is down?

**Answer:**
> "In this simple demo, there's no retry mechanism or persistent event store. In production, you'd need:
> 1. **Persistent Event Store**: Save events to database before publishing
> 2. **Retry Logic**: Implement exponential backoff for failed deliveries
> 3. **Dead Letter Queue**: Capture failed events for manual inspection
> 4. **Health Checks**: Monitor service availability
> 5. **Idempotency**: Ensure processing same event twice doesn't cause issues
> 
> Prism's EventAggregator is in-memory, so it's not suitable for production distributed systems. You'd use something like RabbitMQ, Kafka, or Azure Service Bus."

---

### Q7: How do you handle long-running sagas?

**Answer:**
> "For long-running sagas (hours or days), you need:
> 1. **Persistence**: Store saga state in database
> 2. **Timeouts**: Define maximum wait times for each step
> 3. **Correlation IDs**: Track which events belong to which saga instance
> 4. **State Machine**: Model saga as state machine with clear transitions
> 
> This demo is synchronous and short-lived, but real-world sagas often involve human approval, external APIs, or batch processing that takes time."

---

## 7. Key Design Patterns

| Pattern | Where Used | Purpose |
|---------|-----------|---------|
| **Saga Pattern** | Overall workflow | Manage distributed transactions |
| **Event-Driven Architecture** | Prism EventAggregator | Decouple services |
| **Publisher-Subscriber** | Services subscribe to events | Enable loose coupling |
| **MVVM** | WPF with ViewModels | Separate UI from business logic |
| **Dependency Injection** | Prism container | Manage service lifecycles |
| **Compensating Transaction** | InventoryService rollback | Undo previous operations |
| **Singleton** | Services registered as singletons | Single instance handles all events |

---

## 8. Code Reference

### Service Initialization

**File**: `App.xaml.cs`

```csharp
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // Register services as singletons
    containerRegistry.RegisterSingleton<AuditService>();
    containerRegistry.RegisterSingleton<InventoryService>();
    containerRegistry.RegisterSingleton<PaymentService>();
    
    // Register ViewModels
    containerRegistry.Register<MainViewModel>();
    containerRegistry.Register<OrderViewModel>();
}

protected override void OnInitialized()
{
    base.OnInitialized();
    
    // Force service creation so they subscribe to events
    Container.Resolve<AuditService>();
    Container.Resolve<InventoryService>();
    Container.Resolve<PaymentService>();
}
```

**Why This Matters:**
- Services must be instantiated to subscribe to events
- Singleton ensures one instance handles all events
- Resolution in `OnInitialized` ensures subscriptions are active before UI interactions

---

### Event Definition Pattern

**Example**: `Events/OrderCreatedEvent.cs`

```csharp
using Prism.Events;

namespace WpfPrismEventAggregatorDemo.Events
{
    // Payload class carries data
    public class OrderCreatedPayload
    {
        public int OrderId { get; set; }
        public string OrderName { get; set; } = string.Empty;
    }

    // Event class inherits from PubSubEvent<T>
    public class OrderCreatedEvent : PubSubEvent<OrderCreatedPayload>
    {
    }
}
```

**Pattern:**
1. Create payload class with properties
2. Create event class inheriting from `PubSubEvent<T>`
3. Use generic type to enforce type safety

---

### Subscription Pattern

```csharp
// In constructor
public InventoryService(IEventAggregator eventAggregator)
{
    _eventAggregator = eventAggregator;
    
    // Subscribe with handler method
    _eventAggregator.GetEvent<OrderCreatedEvent>().Subscribe(OnOrderCreated);
}

// Handler method
private void OnOrderCreated(OrderCreatedPayload payload)
{
    // Process the event
    // Access payload.OrderId, payload.OrderName, etc.
}
```

---

### Publication Pattern

```csharp
// Create payload
var payload = new InventoryReservedPayload
{
    OrderId = orderId,
    ReservedItemsCount = 1
};

// Publish event (synchronous by default)
_eventAggregator.GetEvent<InventoryReservedEvent>().Publish(payload);
```

---

### Thread Option Pattern

```csharp
// Subscribe to receive events on UI thread (for safe UI updates)
_eventAggregator.GetEvent<TransactionLogEvent>().Subscribe(
    OnTransactionLogReceived, 
    ThreadOption.UIThread  // Ensures UI thread execution
);
```

**Thread Options:**
- `PublisherThread` (default): Execute on publisher's thread
- `UIThread`: Execute on UI thread (WPF)
- `BackgroundThread`: Execute on background thread

---

## 🎯 Quick Reference Cheat Sheet

### Saga Flow Summary

```
OrderCreatedEvent 
    → InventoryService.Reserve() 
    → InventoryReservedEvent 
    → PaymentService.Charge() 
    → [SUCCESS] PaymentProcessedEvent
    → [FAILURE] PaymentFailedEvent 
    → InventoryService.Release() 
    → OrderFailedEvent
```

### Key Files

| File | Lines | Purpose |
|------|-------|---------|
| `OrderViewModel.cs` | 109 | Initiates saga, displays logs |
| `InventoryService.cs` | 62 | Step 1 + rollback logic |
| `PaymentService.cs` | 61 | Step 2 + failure simulation |
| `App.xaml.cs` | 38 | Service registration & initialization |

### Critical Methods

| Method | Location | Role |
|--------|----------|------|
| `PlaceOrder()` | OrderViewModel | Starts saga |
| `OnOrderCreated()` | InventoryService | Reserves inventory |
| `OnInventoryReserved()` | PaymentService | Processes payment |
| `OnPaymentFailed()` | InventoryService | Executes rollback |

---

## 💡 Best Practices Learned

1. **Always Design Compensating Transactions**: Every forward action needs a way to undo it
2. **Use Meaningful Event Names**: Events should describe what happened, not what to do
3. **Include Correlation IDs**: Always pass OrderId/TransactionId to track saga instances
4. **Log Everything**: Distributed transactions are hard to debug without proper logging
5. **Keep Services Focused**: Each service should handle one responsibility
6. **Test Both Paths**: Always test success AND failure scenarios
7. **Consider Idempotency**: Design handlers to safely process duplicate events

---

## 🔍 Common Interview Questions Checklist

- [ ] Explain the difference between orchestration and choreography
- [ ] What is eventual consistency?
- [ ] How do compensating transactions work?
- [ ] What are the advantages of event-driven architecture?
- [ ] How would you handle a service that's temporarily unavailable?
- [ ] What is idempotency and why is it important?
- [ ] How do you monitor saga execution in production?
- [ ] When would you NOT use the Saga Pattern?

---

## 📚 Additional Resources

### Recommended Reading:
- Microsoft Docs: "Saga pattern - Cloud Design Patterns"
- Martin Fowler: "Saga Pattern"
- Chris Richardson: "Microservices Patterns" (Chapter on Sagas)

### Related Patterns:
- Outbox Pattern (for reliable event publishing)
- Circuit Breaker (for fault tolerance)
- CQRS (Command Query Responsibility Segregation)
- Event Sourcing (storing state as event sequence)

---

## 📝 Notes for Future Enhancement

Potential improvements for production:

1. **Add Retry Logic**: Implement exponential backoff for transient failures
2. **Persistent Event Store**: Use database or message queue instead of in-memory
3. **Saga State Tracking**: Store current state of each saga instance
4. **Timeout Handling**: Cancel sagas that take too long
5. **Monitoring Dashboard**: Visualize saga execution in real-time
6. **Dead Letter Queue**: Capture and inspect failed events
7. **Correlation Tracking**: Use distributed tracing (e.g., OpenTelemetry)

---

**Document Created**: For learning and interview preparation  
**Pattern Type**: Choreography-based Saga with Compensating Transactions  
**Framework**: WPF with Prism EventAggregator  
**Difficulty Level**: Beginner to Intermediate

---

*Remember: The key to mastering the Saga Pattern is understanding that it trades immediate consistency for availability and partition tolerance (CAP theorem). It's about managing failure gracefully in distributed systems.*
