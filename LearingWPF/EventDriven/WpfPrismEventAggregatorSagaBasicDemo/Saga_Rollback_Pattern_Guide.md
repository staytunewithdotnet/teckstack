# Learning Guide: Saga Transactions & Rollback in Event-Driven Architecture

This guide explains how transactions and rollbacks work in an Event-Driven Architecture (EDA), based on the implementation we just built in the WPF project.

---

## 1. Why Traditional Transactions Don't Work in EDA
In a traditional monolith, if you want to place an order and charge a credit card, you open a single database transaction:
```csharp
using (var transaction = db.BeginTransaction())
{
    try {
        db.SaveOrder(order);
        db.ReserveStock(items);
        paymentGateway.ChargeCard(card); // What if this takes 10 seconds?
        transaction.Commit();
    } catch {
        transaction.Rollback(); // Everything reverts automatically
    }
}
```
In modern distributed or event-driven systems:
- **Decoupled Databases**: The Order service and Inventory service have separate databases.
- **Asynchronous Execution**: Services communicate by publishing and subscribing to events over a network. There is no shared transaction context to automatically roll back.

If the payment fails after stock is reserved, how do we revert the stock reservation? We use the **Saga Pattern**.

---

## 2. What is the Saga Pattern?
A **Saga** is a sequence of local transactions. Each transaction updates data within a single service and publishes an event. 
1. **Local Transaction**: The service executes its work and commits locally.
2. **Event Trigger**: An event is published (e.g. `OrderCreatedEvent`).
3. **Next Step**: Another service listens, executes its local transaction, and publishes its own event (e.g. `InventoryReservedEvent`).

### The Rollback Mechanism: Compensating Transactions
If a step fails (e.g., payment is declined), the system must execute **Compensating Transactions** (rollbacks).
- A compensating transaction is an action that explicitly undoes the changes made by a previous step.
- Example: If the step was *Reserve Stock*, the compensating transaction is *Release Stock*.

---

## 3. Step-by-Step Execution of our Saga Implementation

Here is the exact code flow from the project we built:

```
[User clicks Place Order]
       │
       ▼
 1. OrderViewModel
    └─ Publishes OrderCreatedEvent (Saga Initiator)
       │
       ▼
 2. InventoryService
    ├─ Receives OrderCreatedEvent
    ├─ Action: Reserves stock locally
    └─ Publishes InventoryReservedEvent
       │
       ▼
 3. PaymentService
    ├─ Receives InventoryReservedEvent
    ├─ Action: Attempts to charge payment
    │
    ├─► [SUCCESS PATH]
    │   └─ Publishes PaymentProcessedEvent (Order Complete!)
    │
    └─► [FAIL / ROLLBACK PATH]
        ├─ Publishes PaymentFailedEvent
        │  │
        │  ├─► [Compensating Transaction Triggered]
        │  ▼
        │  4. InventoryService
        │     ├─ Receives PaymentFailedEvent
        │     └─ Action: Releases reserved stock (Undo Step 2)
        │
        └─► [UI Update]
           ▼
           5. OrderViewModel
              ├─ Receives OrderFailedEvent
              └─ Action: Displays order failure & rollback status to User
```

---

## 4. Key Files to Reference in the Codebase

### 1. Initiating the Transaction
In [OrderViewModel.cs](file:///c:/AppCodeStore/AI-Model-Code/WPFEventDriven/WpfPrismEventAggregatorDemo/ViewModels/OrderViewModel.cs):
```csharp
// Kick off the transaction saga
_eventAggregator.GetEvent<OrderCreatedEvent>().Publish(new OrderCreatedPayload {
    OrderId = id,
    OrderName = OrderName
});
```

### 2. The Forward & Compensating Steps
In [InventoryService.cs](file:///c:/AppCodeStore/AI-Model-Code/WPFEventDriven/WpfPrismEventAggregatorDemo/Services/InventoryService.cs):
```csharp
// 1. Forward Transaction (Subscribe to OrderCreatedEvent)
_eventAggregator.GetEvent<OrderCreatedEvent>().Subscribe(OnOrderCreated);

// 2. Compensating (Rollback) Transaction (Subscribe to PaymentFailedEvent)
_eventAggregator.GetEvent<PaymentFailedEvent>().Subscribe(OnPaymentFailed);

private void OnPaymentFailed(PaymentFailedPayload payload)
{
    // UNDO the previous reservation:
    ReleaseStock(payload.OrderId);
}
```

### 3. Simulating Failures
In [PaymentService.cs](file:///c:/AppCodeStore/AI-Model-Code/WPFEventDriven/WpfPrismEventAggregatorDemo/Services/PaymentService.cs):
```csharp
if (SimulateFailure)
{
    // Trigger rollback saga
    _eventAggregator.GetEvent<PaymentFailedEvent>().Publish(new PaymentFailedPayload {
        OrderId = payload.OrderId,
        Reason = "Insufficient funds"
    });
}
```

---

## 5. Verification: How to Run and Observe the Rollback
1. Open the WPF app.
2. Enter an item name (e.g. `MacBook Pro`) and click **Place Order**:
   - Check the **Saga Transaction Log** on the right. You will see:
     * `[OrderViewModel] Order 1 placed by user.`
     * `[InventoryService] Stock successfully RESERVED.`
     * `[PaymentService] Payment charged successfully.`
3. Check the **Simulate Payment Failure** checkbox.
4. Click **Place Order** again:
   - Check the log:
     * `[OrderViewModel] Order 2 placed by user.`
     * `[InventoryService] Stock successfully RESERVED.`
     * `[PaymentService] Payment processing FAILED.`
     * `[InventoryService] [COMPENSATING TRANSACTION] Releasing reserved stock...`
     * `[InventoryService] Stock successfully RELEASED. Rollback complete.`
