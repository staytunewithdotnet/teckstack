# ⚡ Parallel Execution in Prism EventAggregator

> **Your Question**: "When does parallel execution happen? Can you show me an example?"

---

## 🎯 **Understanding Parallel vs Sequential**

### The Confusion Clarified:

1. **Saga Workflow = Sequential** (Step 1 → Step 2 → Step 3)
   - Services wait for their trigger event
   - This is the saga pattern flow

2. **Event Subscribers = Can Be Parallel** (when using BackgroundThread)
   - Multiple subscribers to the SAME event
   - Can execute simultaneously on different threads

---

## 📊 **Visual Comparison**

### Sequential Execution (PublisherThread)

```
Event Published
      ↓
┌──────────┐
│ Task A   │ ← 2 seconds
└────┬─────┘
     ↓
┌──────────┐
│ Task B   │ ← 2 seconds
└────┬─────┘
     ↓
┌──────────┐
│ Task C   │ ← 2 seconds
└────┬─────┘
     ↓
  Complete

Total Time: 6 seconds (2s + 2s + 2s)
Thread: Same thread for all tasks
```

---

### Parallel Execution (BackgroundThread)

```
Event Published
      ↓
┌──────────┐  ┌──────────┐  ┌──────────┐
│ Task A   │  │ Task B   │  │ Task C   │
│ Thread 5 │  │ Thread 8 │  │ Thread 12│
└────┬─────┘  └────┬─────┘  └────┬─────┘
     │             │             │
     └─────────────┼─────────────┘
                   ↓
            All run simultaneously
            
Total Time: ~2 seconds (not 6!)
Threads: Different thread pool threads
```

---

## 🧪 **Try It Yourself!**

I've added **two new demo buttons** to the UI:

### Button 1: "⚡ Test Parallel (2s)"

**What Happens:**
1. Publishes `ParallelExecutionEvent`
2. Three subscribers receive it
3. All three run on **different background threads**
4. Each takes 2 seconds
5. **Total time: ~2 seconds** (parallel!)

**Expected Logs:**
```
[TEST] ===== PARALLEL EXECUTION DEMO =====
[TEST] Publishing event with 3 BackgroundThread subscribers...
[Parallel Task A] Started on Thread #5
[Parallel Task B] Started on Thread #8    ← Different thread!
[Parallel Task C] Started on Thread #12   ← Different thread!
[TEST] Publisher returned in 15ms          ← Returned immediately!
[TEST] Tasks are now running in background...
(wait ~2 seconds...)
[Parallel Task A] Completed on Thread #5
[Parallel Task B] Completed on Thread #8
[Parallel Task C] Completed on Thread #12
[TEST] Expected completion: ~2 seconds (parallel, not sequential!)
```

**Notice:**
- ✅ All three tasks start almost simultaneously
- ✅ Different thread IDs (#5, #8, #12)
- ✅ Publisher returns immediately (15ms)
- ✅ Total time is ~2 seconds, not 6!

---

### Button 2: "🐌 Test Sequential (6s)"

**What Happens:**
1. Publishes `SequentialExecutionEvent`
2. Three subscribers receive it
3. All three run on **same publisher thread**
4. Each takes 2 seconds
5. **Total time: ~6 seconds** (sequential!)

**Expected Logs:**
```
[TEST] ===== SEQUENTIAL EXECUTION DEMO =====
[TEST] Publishing event with 3 PublisherThread subscribers...
[Sequential Task 1] Started on Thread #1
(wait 2 seconds...)
[Sequential Task 1] Completed
[Sequential Task 2] Started on Thread #1   ← Same thread!
(wait 2 seconds...)
[Sequential Task 2] Completed
[Sequential Task 3] Started on Thread #1   ← Same thread!
(wait 2 seconds...)
[Sequential Task 3] Completed
[TEST] Sequential execution completed in 6015ms
[TEST] Note: Publisher was blocked for entire duration (~6 seconds)
```

**Notice:**
- ❌ Tasks run one after another
- ✅ Same thread ID (#1) for all
- ❌ Publisher blocked for 6 seconds
- ❌ Total time is 6 seconds (2+2+2)

---

## 💻 **Code Implementation**

### Parallel Execution Setup

**File**: `Services/ThreadOptionDemoService.cs`

```csharp
// Subscriber A - runs on background thread
_eventAggregator.GetEvent<ParallelExecutionEvent>().Subscribe(
    payload =>
    {
        var threadId = Thread.CurrentThread.ManagedThreadId;
        Log($"[Parallel Task A] Started on Thread #{threadId}", "INFO");
        
        Thread.Sleep(2000); // Simulate 2 seconds of work
        
        Log($"[Parallel Task A] Completed on Thread #{threadId}", "SUCCESS");
    },
    ThreadOption.BackgroundThread // 👈 KEY: Allows parallel execution
);

// Subscriber B - also runs on background thread
_eventAggregator.GetEvent<ParallelExecutionEvent>().Subscribe(
    payload =>
    {
        var threadId = Thread.CurrentThread.ManagedThreadId;
        Log($"[Parallel Task B] Started on Thread #{threadId}", "INFO");
        
        Thread.Sleep(2000);
        
        Log($"[Parallel Task B] Completed on Thread #{threadId}", "SUCCESS");
    },
    ThreadOption.BackgroundThread // 👈 KEY: Allows parallel execution
);

// Subscriber C - also runs on background thread
_eventAggregator.GetEvent<ParallelExecutionEvent>().Subscribe(
    payload =>
    {
        var threadId = Thread.CurrentThread.ManagedThreadId;
        Log($"[Parallel Task C] Started on Thread #{threadId}", "INFO");
        
        Thread.Sleep(2000);
        
        Log($"[Parallel Task C] Completed on Thread #{threadId}", "SUCCESS");
    },
    ThreadOption.BackgroundThread // 👈 KEY: Allows parallel execution
);
```

---

### Sequential Execution Setup (For Comparison)

```csharp
// All three subscribers use PublisherThread
_eventAggregator.GetEvent<SequentialExecutionEvent>().Subscribe(
    payload =>
    {
        Log($"[Sequential Task 1] Started", "INFO");
        Thread.Sleep(2000);
        Log($"[Sequential Task 1] Completed", "SUCCESS");
    },
    ThreadOption.PublisherThread // 👈 Forces sequential execution
);

_eventAggregator.GetEvent<SequentialExecutionEvent>().Subscribe(
    payload =>
    {
        Log($"[Sequential Task 2] Started", "INFO");
        Thread.Sleep(2000);
        Log($"[Sequential Task 2] Completed", "SUCCESS");
    },
    ThreadOption.PublisherThread // 👈 Forces sequential execution
);

_eventAggregator.GetEvent<SequentialExecutionEvent>().Subscribe(
    payload =>
    {
        Log($"[Sequential Task 3] Started", "INFO");
        Thread.Sleep(2000);
        Log($"[Sequential Task 3] Completed", "SUCCESS");
    },
    ThreadOption.PublisherThread // 👈 Forces sequential execution
);
```

---

## 🏭 **Real-World Example: Order Processing**

### Scenario: Send Notifications After Payment

When payment succeeds, you need to:
1. Send confirmation email
2. Send SMS notification
3. Update analytics dashboard

### Sequential Approach (SLOW):

```csharp
// All use PublisherThread - runs sequentially
_eventAggregator.GetEvent<PaymentProcessedEvent>().Subscribe(
    payload => {
        SendEmail(payload.OrderId);    // 2 seconds
    },
    ThreadOption.PublisherThread
);

_eventAggregator.GetEvent<PaymentProcessedEvent>().Subscribe(
    payload => {
        SendSMS(payload.OrderId);      // 2 seconds
    },
    ThreadOption.PublisherThread
);

_eventAggregator.GetEvent<PaymentProcessedEvent>().Subscribe(
    payload => {
        UpdateAnalytics(payload.OrderId); // 2 seconds
    },
    ThreadOption.PublisherThread
);

// Total time: 6 seconds 😱
```

---

### Parallel Approach (FAST):

```csharp
// All use BackgroundThread - runs in parallel!
_eventAggregator.GetEvent<PaymentProcessedEvent>().Subscribe(
    async payload => {
        await SendEmailAsync(payload.OrderId);    // 2 seconds
    },
    ThreadOption.BackgroundThread
);

_eventAggregator.GetEvent<PaymentProcessedEvent>().Subscribe(
    async payload => {
        await SendSMSAsync(payload.OrderId);      // 2 seconds
    },
    ThreadOption.BackgroundThread
);

_eventAggregator.GetEvent<PaymentProcessedEvent>().Subscribe(
    async payload => {
        await UpdateAnalyticsAsync(payload.OrderId); // 2 seconds
    },
    ThreadOption.BackgroundThread
);

// Total time: ~2 seconds 🚀
```

---

## ⚠️ **Important Considerations**

### When to Use Parallel Execution:

✅ **Independent Operations**: Tasks don't depend on each other  
✅ **Performance Critical**: Need to minimize total execution time  
✅ **No Shared State**: Tasks don't modify same data  
✅ **External APIs**: Calling different services  

### When NOT to Use Parallel Execution:

❌ **Dependent Operations**: Task B needs Task A's result  
❌ **Shared Resources**: Tasks write to same database record  
❌ **Order Matters**: Must execute in specific sequence  
❌ **Transaction Required**: All must succeed or all fail together  

---

## 🔒 **Thread Safety Warning**

When running in parallel, be careful with shared state:

### ❌ BAD: Race Condition

```csharp
private int _counter = 0;

_eventAggregator.GetEvent<MyEvent>().Subscribe(_ =>
{
    _counter++; // ❌ Not thread-safe! Race condition!
}, ThreadOption.BackgroundThread);
```

### ✅ GOOD: Thread-Safe

```csharp
private int _counter = 0;
private readonly object _lock = new object();

_eventAggregator.GetEvent<MyEvent>().Subscribe(_ =>
{
    lock (_lock)
    {
        _counter++; // ✅ Thread-safe with lock
    }
}, ThreadOption.BackgroundThread);

// OR use Interlocked for simple operations
_eventAggregator.GetEvent<MyEvent>().Subscribe(_ =>
{
    Interlocked.Increment(ref _counter); // ✅ Thread-safe
}, ThreadOption.BackgroundThread);
```

---

## 📊 **Performance Comparison Table**

| Aspect | Sequential (PublisherThread) | Parallel (BackgroundThread) |
|--------|-----------------------------|----------------------------|
| **Total Time** | 6 seconds (2+2+2) | ~2 seconds |
| **Threads Used** | 1 (publisher's thread) | 3 (from thread pool) |
| **Publisher Blocked** | Yes (6 seconds) | No (returns immediately) |
| **UI Responsive** | No (if on UI thread) | Yes |
| **Complexity** | Simple | Need thread safety |
| **Use Case** | Dependent tasks | Independent tasks |

---

## 🎬 **How to Test**

### Step 1: Run the Application

```bash
# Open solution in Visual Studio
# Press F5
```

### Step 2: Try Sequential Demo

1. Click **"🐌 Test Sequential (6s)"** button
2. Watch logs appear one by one
3. Notice UI freezes for ~6 seconds
4. Observe same thread ID for all tasks
5. See total time: ~6000ms

### Step 3: Try Parallel Demo

1. Click **"⚡ Test Parallel (2s)"** button
2. Watch all three tasks start almost simultaneously
3. Notice UI stays responsive
4. Observe different thread IDs
5. See total time: ~2000ms

### Step 4: Compare Results

**Sequential:**
- Total time: 6 seconds
- UI frozen
- Same thread

**Parallel:**
- Total time: 2 seconds
- UI responsive
- Different threads

**Result: 3x faster with parallel execution!** 🚀

---

## 💡 **Key Takeaways**

1. **ThreadOption controls execution model**:
   - `PublisherThread` = Sequential
   - `BackgroundThread` = Parallel

2. **Parallel is faster for independent tasks**:
   - 3 tasks × 2 seconds = 2 seconds (parallel)
   - Not 6 seconds (sequential)

3. **Use parallel when**:
   - Tasks are independent
   - Performance matters
   - No shared state

4. **Be careful with**:
   - Thread safety
   - Race conditions
   - Resource contention

---

## 🎯 **Answer to Your Original Question**

> "On which case it is parallel execution?"

**Answer**: Parallel execution happens when:

1. **Multiple subscribers** listen to the **same event**
2. They use **`ThreadOption.BackgroundThread`**
3. The tasks are **independent** (don't depend on each other)

**Example from this project**:
```csharp
// Three subscribers to ParallelExecutionEvent
// All use ThreadOption.BackgroundThread
// They run in parallel on different threads!
```

**In Saga Pattern context**:
- Saga steps themselves are sequential (Step 1 → Step 2 → Step 3)
- BUT post-saga notifications can be parallel (email + SMS + analytics)

---

**Try the demos now and see the difference yourself!** ⚡
