# Core Concepts - Understanding Distributed System Safety

## 🍕 The Pizza Shop Analogy

Imagine you own a pizza shop with 3 delivery drivers (Pod A, Pod B, Pod C).

### Problem 1: The Claim Pattern (Preventing Double-Delivery)

**Scenario**: An order comes in for "1 Large Pepperoni"

**WITHOUT Claim Pattern** (Chaos):
```
1. Order arrives: "Make pizza for John"
2. Pod A sees it: "I'll deliver this!" → Starts making pizza
3. Pod B sees it: "I'll deliver this!" → ALSO starts making pizza (SAME order!)
4. Result: John gets 2 pizzas (waste), company loses money
```

**WITH Claim Pattern** (Order):
```
1. Order arrives: "Make pizza for John"
2. Pod A: "I'm claiming this order. My name is A. Deadline: 30 seconds."
   → Writes: {LockedBy: "Pod-A", LockedUntil: 12:00:30}
3. Pod B tries to claim: Sees Pod A's name + timer → SKIPS this order
4. Pod C tries to claim: Sees Pod A's name + timer → SKIPS this order
5. Result: Only Pod A delivers. No waste.
```

**The SQL Magic**:
```sql
UPDATE TOP (1) Orders WITH (UPDLOCK, ROWLOCK, READPAST)
SET LockedBy = 'Pod-A', LockedUntil = DATEADD(SECOND, 30, GETUTCDATE())
OUTPUT INSERTED.*
WHERE Status = 'Pending'
```

**Lock Breakdown**:
- `UPDLOCK` = "I'm about to update this. Block others from updating."
- `ROWLOCK` = "Only lock THIS row. Don't lock the whole table."
- `READPAST` = "If someone else locked it, SKIP IT. Don't wait."

---

### Problem 2: The Outbox Pattern (Guaranteed Delivery)

**Scenario**: You save an order to DB, then send it to RabbitMQ.

**WITHOUT Outbox** (Data Loss):
```
1. Pod A saves order to SQL Server ✅
2. Pod A tries to send to RabbitMQ... ❌ NETWORK CRASH!
3. RabbitMQ never gets the message
4. Kitchen never sees the order
5. Customer waits forever
```

**WITH Outbox** (Guaranteed):
```
1. Pod A starts transaction
2. Saves order to Orders table ✅
3. Saves message to OutboxMessage table ✅ (in SAME transaction)
4. Commits transaction ✅
5. Background thread: "I see a new OutboxMessage. I'll send it to RabbitMQ."
6. Even if Pod A crashes, background thread retries delivery
```

**Code Example**:
```csharp
using var tx = await db.Database.BeginTransactionAsync();

// 1. Save business data
db.Orders.Add(new Order { ... });
await db.SaveChangesAsync();

// 2. Save message to outbox (NOT sent to RabbitMQ yet)
await bus.Publish(new OrderCreatedEvent { ... }); // MassTransit intercepts this

// 3. Commit BOTH together
await tx.CommitAsync();

// Background thread handles actual RabbitMQ delivery
```

---

### Problem 3: The Inbox Pattern (Preventing Duplicates)

**Scenario**: RabbitMQ delivers the same message twice (network glitch).

**WITHOUT Inbox** (Double Processing):
```
1. Message "Refund $100" arrives → Pod A processes it ✅
2. Network glitch causes RabbitMQ to redeliver same message
3. Message "Refund $100" arrives AGAIN → Pod A processes it AGAIN ❌
4. Customer gets $200 refund instead of $100
5. Company loses $100
```

**WITH Inbox** (Safe):
```
1. Message "Refund $100" (ID: msg-123) arrives
2. Check InboxState table: "Have I seen msg-123 before?"
3. Answer: NO → Process it, then record msg-123 in InboxState
4. Message "Refund $100" (ID: msg-123) arrives AGAIN
5. Check InboxState: "Have I seen msg-123 before?"
6. Answer: YES → Skip it! Already processed.
```

**Code Example**:
```csharp
public async Task Consume(ConsumeContext<RefundCommand> ctx)
{
    // 1. Check if already processed
    var exists = await db.IdempotencyKeys.AnyAsync(x => x.MessageId == ctx.MessageId);
    if (exists) 
    {
        Console.WriteLine("Duplicate! Skipping.");
        return; // Stop here. Idempotency achieved!
    }

    // 2. Process the message
    await ProcessRefund(ctx.Message);

    // 3. Record that we processed it
    db.IdempotencyKeys.Add(new IdempotencyKey { MessageId = ctx.MessageId });
    await db.SaveChangesAsync();
}
```

---

## 🔬 How It Works Internally

### SQL Server Locks (The Secret Sauce)

When you run this query:
```sql
UPDATE TOP (1) Orders WITH (UPDLOCK, ROWLOCK, READPAST)
SET LockedBy = 'Pod-A'
WHERE Status = 'Pending'
```

**What SQL Server Does**:

1. **Find the first row** where `Status = 'Pending'`
2. **Acquire U-lock** (Update lock) on that row's memory page
   - U-lock means: "I intend to update this. Others can read but not update."
3. **Check for conflicts**:
   - Does another transaction hold an X-lock (exclusive lock)? 
   - If YES → `READPAST` says "Skip this row, find another one"
   - If NO → Proceed
4. **Convert U-lock to X-lock** (exclusive lock)
   - X-lock means: "I'm updating NOW. No one else can touch this."
5. **Update the row**: Set `LockedBy`, `LockedUntil`, increment `ROWVERSION`
6. **Return the row** via `OUTPUT INSERTED.*`
7. **Release lock** (after transaction commits)

**Timeline**:
```
Time 0ms: Pod A starts UPDATE query
Time 1ms: Pod A acquires U-lock on Order #1
Time 2ms: Pod B starts UPDATE query
Time 3ms: Pod B sees Pod A's U-lock → READPAST skips Order #1 → Finds Order #2
Time 4ms: Pod A converts to X-lock, updates row, releases lock
Time 5ms: Pod B already claimed Order #2 (different order, no conflict!)
```

---

### MassTransit Outbox (The Background Worker)

**Architecture**:
```
┌──────────────────────────────────────────────────┐
│                  Your Application                 │
│                                                   │
│  1. Save Order ──┐                               │
│                  ├──→ Single Transaction          │
│  2. Publish      │   (Both succeed or both fail) │
│     Message      │                               │
│     (Outbox)     │                               │
│                  │                               │
│  3. Background Service (HostedService)           │
│     ┌──────────────────────────────────────┐    │
│     │ Loop every 1 second:                 │    │
│     │   SELECT * FROM OutboxMessage        │    │
│     │   WHERE SentAt IS NULL               │    │
│     │   → Send to RabbitMQ                 │    │
│     │   → Mark as SentAt = NOW             │    │
│     └──────────────────────────────────────┘    │
└──────────────────────────────────────────────────┘
```

**What Happens on Crash**:
```
Time 0: App saves Order + publishes message (saved to Outbox) ✅
Time 1: App crashes before sending to RabbitMQ ❌
Time 2: App restarts
Time 3: Background service wakes up
Time 4: Sees unsent OutboxMessage
Time 5: Sends to RabbitMQ ✅
Time 6: Marks as sent
```

**Result**: Message delivered even after crash!

---

## 🎯 When to Use Each Pattern

### Use Claim Pattern When:
✅ Multiple workers (pods) process tasks from a queue  
✅ You can't afford to process the same task twice  
✅ You're using SQL Server (not Redis/Kafka)  
✅ You need self-healing (lock expiration = automatic retry)

### Don't Use Claim Pattern When:
❌ You need microsecond latency (use Kafka/RabbitMQ direct push)  
❌ You forget to add indexes (will cause deadlocks!)  
❌ You're building a simple single-user app (overkill)

---

### Use Outbox Pattern When:
✅ You save data to DB AND send messages  
✅ You can't afford to lose messages (financial data, orders)  
✅ You want guaranteed delivery even if app crashes

### Don't Use Outbox Pattern When:
❌ You're doing read-only operations  
❌ You're okay with eventual consistency (use direct publish)  
❌ You don't have a database (use RabbitMQ transactions instead)

---

### Use Inbox Pattern When:
✅ You consume messages from RabbitMQ/Kafka  
✅ You can't afford to process the same message twice  
✅ You want idempotency (safe to retry)

### Don't Use Inbox Pattern When:
❌ Your consumer is naturally idempotent (e.g., "Set status to X" is safe to repeat)  
❌ You're okay with duplicate processing (rare, but sometimes acceptable)

---

## 💰 Real-World Production Issue

### The $50,000 Refund Bug

**Company**: Fintech app processing refund requests  
**Setup**: 5 pods consuming from RabbitMQ queue

**The Bug**:
```
1. Customer requests refund for Order #123
2. RabbitMQ delivers "ProcessRefund" message to Pod A
3. Network glitch: RabbitMQ thinks delivery failed → redelivers to Pod B
4. Pod A processes refund: $100 sent to customer ✅
5. Pod B processes SAME refund: $100 sent to customer AGAIN ❌
6. Customer gets $200 instead of $100
7. Company loses $100 per duplicate
```

**Impact**: $50,000 lost in 1 hour before bug was found

**The Fix**:
```csharp
// 1. Claim Pattern: Only ONE pod can claim the refund
var refund = await db.Refunds.FromSqlRaw(@"
    UPDATE TOP(1) Refunds WITH (UPDLOCK, ROWLOCK, READPAST)
    SET Status = 'Claimed', LockedBy = {0}
    OUTPUT INSERTED.*
    WHERE OrderId = {1} AND Status = 'Pending'",
    podName, orderId).FirstOrDefaultAsync();

if (refund == null) return; // Another pod already claimed it

// 2. Inbox Pattern: Check if already processed
var alreadyProcessed = await db.InboxState.AnyAsync(x => 
    x.MessageId == ctx.MessageId);
if (alreadyProcessed) return; // Already did this refund

// 3. UNIQUE Constraint: Database-level safety net
try 
{
    db.RefundTransactions.Add(new RefundTransaction { OrderId = orderId });
    await db.SaveChangesAsync();
}
catch (DbUpdateException ex) when (ex.Message.Contains("UNIQUE"))
{
    return; // Another pod already inserted
}

// 4. Record in Inbox
db.InboxState.Add(new InboxState { MessageId = ctx.MessageId });
await db.SaveChangesAsync();
```

**Result**: Zero duplicates after fix. $50,000 saved.

---

## ⚖️ Trade-offs

### Claim Pattern Trade-offs

**Pros** ✅:
- 100% prevents race conditions
- Self-healing (lock expiration = automatic retry)
- No extra infrastructure (works in SQL Server)
- Debuggable (you can see who locked what)

**Cons** ❌:
- Database load (constant polling)
- Requires indexes (or will deadlock)
- Wasted work if pod crashes mid-processing

**Mitigation**:
- Add `DELAY` between polls (e.g., `Thread.Sleep(1000)`)
- Monitor lock expiration frequency
- Use longer lock timeouts (e.g., 5 minutes instead of 30 seconds)

---

### Outbox Pattern Trade-offs

**Pros** ✅:
- Guaranteed delivery (even after crash)
- Atomic (data + message saved together)
- Reliable (background thread retries)

**Cons** ❌:
- Slight delay (message not sent instantly)
- Extra DB load (background polling)
- Complexity (need OutboxMessage table)

**Mitigation**:
- Run background thread every 100ms (not 1 second)
- Clean up old OutboxMessages (archive after 7 days)
- Monitor outbox queue size

---

### Inbox Pattern Trade-offs

**Pros** ✅:
- Prevents duplicate processing
- Idempotent (safe to retry)
- Simple (just check MessageId)

**Cons** ❌:
- Extra DB write (record MessageId)
- InboxState table grows forever
- Need to handle out-of-order messages

**Mitigation**:
- Partition InboxState by date (e.g., `InboxState_2024_01`)
- Clean up old records (delete after 30 days)
- Use UNIQUE constraint instead of separate table (if possible)

---

## 🎓 Summary Table

| Pattern | Problem It Solves | Performance Impact | Complexity |
|---------|-------------------|-------------------|------------|
| **Claim** | Double-processing | Medium (polling) | Medium (lock hints) |
| **Outbox** | Lost messages | Low (background) | Medium (extra table) |
| **Inbox** | Duplicate messages | Low (1 extra write) | Low (simple check) |

**Use all three together** for maximum safety in financial/order-processing systems.

---

## 🚀 Next Steps

1. **Run the demo**: Follow `TESTING_GUIDE.md`
2. **See it in action**: Check `sql/queries.sql`
3. **Read the code**: Start with `src/SagaApp/Consumers/OrderConsumer.cs`
4. **Experiment**: Modify `PrefetchCount` and see what happens
