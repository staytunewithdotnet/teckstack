# 🧵 Prism EventAggregator ThreadOption - Complete Guide

> **Purpose**: Understand and master threading options in Prism EventAggregator  
> **Project**: WPF Prism EventAggregator Demo with interactive examples  
> **Difficulty**: Beginner to Intermediate

---

## 📋 Table of Contents

1. [What are ThreadOptions?](#what-are-threadoptions)
2. [Three Thread Options Explained](#three-thread-options-explained)
3. [Interactive Demos](#interactive-demos)
4. [Real-World Examples](#real-world-examples)
5. [Common Pitfalls](#common-pitfalls)
6. [Best Practices](#best-practices)
7. [Performance Comparison](#performance-comparison)

---

## What are ThreadOptions?

**ThreadOptions** control which thread executes an event handler when an event is published in Prism EventAggregator.

### Why Does This Matter?

In WPF applications:
- **UI updates must happen on UI thread** (or you get cross-thread exceptions)
- **Long-running operations block the UI** if run on UI thread
- **Background operations can't update UI directly**

ThreadOptions help you manage these scenarios correctly.

---

## Three Thread Options Explained

### 1. ThreadOption.PublisherThread (DEFAULT)

#### Definition
Event handler executes on the **same thread** that published the event.

#### Code
```csharp
// These are equivalent - PublisherThread is default
_eventAggregator.GetEvent<MyEvent>().Subscribe(Handler);
_eventAggregator.GetEvent<MyEvent>().Subscribe(Handler, ThreadOption.PublisherThread);
```

#### Behavior
- ✅ **Synchronous** - publisher waits for handler to complete
- ✅ **Predictable** - execution order is clear
- ❌ **Blocking** - slow handlers block the publisher
- ❌ **UI Risk** - if publisher is on UI thread, UI freezes

#### When to Use
- Fast operations (< 10ms)
- In-memory data transformations
- Logging
- Cache updates

#### Example
```csharp
// GOOD: Fast operation
_eventAggregator.GetEvent<OrderCreatedEvent>().Subscribe(payload =>
{
    // Quick in-memory cache update
    _orderCache[payload.OrderId] = payload;
});

// BAD: Slow operation blocks publisher
_eventAggregator.GetEvent<OrderCreatedEvent>().Subscribe(payload =>
{
    Thread.Sleep(5000); // BLOCKS for 5 seconds! 😱
});
```

---

### 2. ThreadOption.UIThread

#### Definition
Event handler executes on the **WPF UI thread** via Dispatcher.

#### Code
```csharp
_eventAggregator.GetEvent<MyEvent>().Subscribe(Handler, ThreadOption.UIThread);
```

#### Behavior
- ✅ **Safe UI updates** - can modify ObservableCollection, controls
- ✅ **No Dispatcher.Invoke needed** - already on UI thread
- ❌ **Can deadlock** - if publisher is on UI thread and waits for result
- ❌ **Sequential** - all UIThread handlers execute one at a time

#### When to Use
- Updating UI-bound collections (ObservableCollection)
- Modifying UI controls
- Showing notifications/dialogs
- Updating progress bars

#### Example
```csharp
// GOOD: Safe UI update
_eventAggregator.GetEvent<TransactionLogEvent>().Subscribe(
    payload =>
    {
        // Safe to update ObservableCollection bound to ListBox
        Logs.Add($"{payload.Message}");
    },
    ThreadOption.UIThread
);

// BAD: Deadlock risk
var tcs = new TaskCompletionSource<bool>();
_eventAggregator.GetEvent<MyEvent>().Subscribe(_ =>
{
    Thread.Sleep(5000); // Blocks UI thread!
    tcs.SetResult(true);
}, ThreadOption.UIThread);

_eventAggregator.GetEvent<MyEvent>().Publish(new MyPayload());
await tcs.Task; // DEADLOCK! UI thread blocked by subscriber 😱
```

---

### 3. ThreadOption.BackgroundThread

#### Definition
Event handler executes on a **background thread** from the thread pool.

#### Code
```csharp
_eventAggregator.GetEvent<MyEvent>().Subscribe(Handler, ThreadOption.BackgroundThread);
```

#### Behavior
- ✅ **Non-blocking** - doesn't block publisher or UI
- ✅ **Parallel** - multiple handlers can run simultaneously
- ✅ **Good for async** - perfect for API calls, file I/O
- ❌ **Cannot update UI directly** - must marshal back via Dispatcher
- ❌ **Thread safety** - handlers may run in parallel (use locks if needed)

#### When to Use
- HTTP API calls
- Database queries
- File I/O operations
- Heavy computations
- Image processing

#### Example
```csharp
// GOOD: Async API call
_eventAggregator.GetEvent<PaymentRequestEvent>().Subscribe(
    async payload =>
    {
        // Non-blocking HTTP call
        var response = await _httpClient.PostAsync("https://api.stripe.com/charge", content);
        
        // Must marshal back to UI thread to update UI
        Application.Current.Dispatcher.Invoke(() =>
        {
            StatusMessage = "Payment processed";
        });
    },
    ThreadOption.BackgroundThread
);

// BAD: Trying to update UI directly from background thread
_eventAggregator.GetEvent<MyEvent>().Subscribe(payload =>
{
    Logs.Add("This will throw InvalidOperationException!"); // ❌ Cross-thread exception
}, ThreadOption.BackgroundThread);
```

---

## Interactive Demos

The application includes a **Thread Option Demo** section in the UI. Click the buttons to see each option in action!

### Demo 1: PublisherThread

**Button**: "PublisherThread"

**What Happens**:
1. Publishes event from UI thread
2. Handler executes immediately on UI thread
3. Handler sleeps for 500ms (simulates work)
4. UI is **blocked** during this time
5. Log shows: "Publisher thread test completed in 500ms (includes handler time)"

**Observe**:
- UI freezes for 500ms
- Button stays pressed
- Total time includes handler execution

**Code**:
```csharp
public void TestPublisherThread()
{
    var stopwatch = Stopwatch.StartNew();
    
    _eventAggregator.GetEvent<PublisherThreadDemoEvent>().Publish(...);
    // Handler executes HERE (blocks this thread)
    
    stopwatch.Stop();
    Log($"Completed in {stopwatch.ElapsedMilliseconds}ms"); // ~500ms
}
```

---

### Demo 2: UIThread

**Button**: "UIThread"

**What Happens**:
1. Publishes event from UI thread
2. Handler is queued on UI thread Dispatcher
3. Handler executes asynchronously on UI thread
4. Handler sleeps for 300ms
5. Publisher returns immediately

**Observe**:
- UI doesn't freeze (publisher returns immediately)
- Log message appears after ~300ms
- Safe to update UI elements

**Code**:
```csharp
public void TestUIThread()
{
    _eventAggregator.GetEvent<UIThreadDemoEvent>().Publish(...);
    // Returns immediately - handler executes asynchronously
    
    Log("Published (handler runs async)"); // Appears immediately
}
```

---

### Demo 3: BackgroundThread

**Button**: "BackgroundThread"

**What Happens**:
1. Publishes event from UI thread
2. Handler executes on background thread from thread pool
3. Handler sleeps for 1000ms (simulates long operation)
4. UI remains responsive
5. Publisher returns immediately

**Observe**:
- UI fully responsive during 1000ms operation
- Log shows different thread ID than publisher
- Can't update UI directly (would need Dispatcher)

**Code**:
```csharp
public void TestBackgroundThread()
{
    var stopwatch = Stopwatch.StartNew();
    
    _eventAggregator.GetEvent<BackgroundThreadDemoEvent>().Publish(...);
    // Handler runs on background thread
    
    stopwatch.Stop();
    Log($"Published in {stopwatch.ElapsedMilliseconds}ms"); // ~0ms (doesn't wait)
}
```

---

### Demo 4: Async Operation

**Button**: "Async Operation"

**What Happens**:
1. Publishes event on background thread
2. Handler uses async/await pattern
3. Simulates async API call with Task.Delay(800ms)
4. Thread ID may change after await
5. Demonstrates proper async pattern

**Observe**:
- Thread ID before and after await may differ
- Non-blocking operation
- Best practice for real-world async work

**Code**:
```csharp
_eventAggregator.GetEvent<AsyncDemoEvent>().Subscribe(
    async payload =>
    {
        var threadIdBefore = Thread.CurrentThread.ManagedThreadId;
        Log($"Starting on Thread #{threadIdBefore}");
        
        await Task.Delay(800); // Simulates async API call
        
        var threadIdAfter = Thread.CurrentThread.ManagedThreadId;
        Log($"Completed on Thread #{threadIdAfter} (may be different!)");
    },
    ThreadOption.BackgroundThread
);
```

---

### Demo 5: Multiple Subscribers

**Button**: "Multiple Subscribers"

**What Happens**:
1. Publishes single event
2. Three subscribers all use PublisherThread
3. All three execute sequentially on same thread
4. Each subscriber sleeps for 200ms
5. Total time: ~600ms (all three run one after another)

**Observe**:
- All subscribers run on same thread
- Execution is sequential, not parallel
- Total time = sum of all handler times

**Code**:
```csharp
// Three subscribers to same event
_eventAggregator.GetEvent<MultipleSubscribersEvent>().Subscribe(
    payload => { Thread.Sleep(200); }, // Subscriber 1
    ThreadOption.PublisherThread
);

_eventAggregator.GetEvent<MultipleSubscribersEvent>().Subscribe(
    payload => { Thread.Sleep(200); }, // Subscriber 2
    ThreadOption.PublisherThread
);

_eventAggregator.GetEvent<MultipleSubscribersEvent>().Subscribe(
    payload => { Thread.Sleep(200); }, // Subscriber 3
    ThreadOption.PublisherThread
);

// Publishing triggers all three sequentially
_eventAggregator.GetEvent<MultipleSubscribersEvent>().Publish(...);
// Total time: ~600ms
```

---

## Real-World Examples

### Example 1: Order Processing (Current Project)

#### Scenario
User places order → Reserve inventory → Process payment → Update UI

#### Implementation
```csharp
// OrderViewModel subscribes to logs with UIThread for safe UI updates
public OrderViewModel(IEventAggregator eventAggregator)
{
    _eventAggregator.GetEvent<TransactionLogEvent>().Subscribe(
        OnLogReceived,
        ThreadOption.UIThread  // ✅ Safe to update ObservableCollection
    );
}

private void OnLogReceived(TransactionLogPayload payload)
{
    // No Dispatcher.Invoke needed - already on UI thread
    Logs.Add($"{payload.Message}");
}
```

---

### Example 2: Payment Gateway Integration

#### Scenario
Process payment via external API (Stripe/PayPal) without blocking UI

#### Implementation
```csharp
public class PaymentServiceWithRetry
{
    public PaymentServiceWithRetry(IEventAggregator eventAggregator)
    {
        // Use BackgroundThread for async API calls
        _eventAggregator.GetEvent<InventoryReservedEvent>().Subscribe(
            OnInventoryReservedAsync,
            ThreadOption.BackgroundThread  // ✅ Non-blocking
        );
    }

    private async void OnInventoryReservedAsync(InventoryReservedPayload payload)
    {
        try
        {
            // Async HTTP call to payment gateway (non-blocking)
            var response = await _httpClient.PostAsync(
                "https://api.stripe.com/v1/charges",
                paymentData
            );
            
            var result = await response.Content.ReadAsAsync<PaymentResult>();
            
            if (result.Success)
            {
                // Marshal back to UI thread if needed
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _eventAggregator.GetEvent<PaymentProcessedEvent>().Publish(...);
                });
            }
        }
        catch (Exception ex)
        {
            // Handle error
        }
    }
}
```

---

### Example 3: File Upload with Progress

#### Scenario
Upload file to server and show progress bar

#### Implementation
```csharp
_eventAggregator.GetEvent<FileUploadEvent>().Subscribe(
    async payload =>
    {
        var fileSize = new FileInfo(payload.FilePath).Length;
        var uploaded = 0L;
        
        using (var stream = File.OpenRead(payload.FilePath))
        using (var content = new StreamContent(stream))
        {
            var response = await _httpClient.PostAsync(
                "https://api.example.com/upload",
                content,
                new ProgressMessageHandler((sent, total) =>
                {
                    // Report progress on background thread
                    _eventAggregator.GetEvent<UploadProgressEvent>().Publish(
                        new UploadProgressPayload
                        {
                            PercentComplete = (int)(sent * 100 / total)
                        }
                    );
                })
            );
        }
    },
    ThreadOption.BackgroundThread  // ✅ Doesn't block UI
);

// UI subscribes to progress updates
_eventAggregator.GetEvent<UploadProgressEvent>().Subscribe(
    payload =>
    {
        // Update progress bar (safe on UI thread)
        ProgressBar.Value = payload.PercentComplete;
    },
    ThreadOption.UIThread  // ✅ Safe UI update
);
```

---

### Example 4: Database Query with Caching

#### Scenario
Query database and cache results without blocking UI

#### Implementation
```csharp
_eventAggregator.GetEvent<LoadProductsEvent>().Subscribe(
    async payload =>
    {
        // Check cache first (fast, can be on any thread)
        if (_cache.TryGetValue("products", out var cached))
        {
            _eventAggregator.GetEvent<ProductsLoadedEvent>().Publish(cached);
            return;
        }
        
        // Query database (slow, use background thread)
        var products = await _dbContext.Products.ToListAsync();
        
        // Cache results
        _cache["products"] = products;
        
        // Publish results
        _eventAggregator.GetEvent<ProductsLoadedEvent>().Publish(products);
    },
    ThreadOption.BackgroundThread  // ✅ Non-blocking DB query
);

// UI subscribes to results
_eventAggregator.GetEvent<ProductsLoadedEvent>().Subscribe(
    products =>
    {
        // Update DataGrid (safe on UI thread)
        ProductsGrid.ItemsSource = products;
    },
    ThreadOption.UIThread  // ✅ Safe UI update
);
```

---

## Common Pitfalls

### ❌ Pitfall 1: Blocking UI Thread

**Problem**: Long-running handler on UI thread freezes application

```csharp
// BAD: Blocks UI for 5 seconds!
_eventAggregator.GetEvent<MyEvent>().Subscribe(payload =>
{
    Thread.Sleep(5000); // 😱 UI FREEZE
}, ThreadOption.UIThread);
```

**Solution**: Use BackgroundThread for long operations

```csharp
// GOOD: Non-blocking
_eventAggregator.GetEvent<MyEvent>().Subscribe(async payload =>
{
    await Task.Delay(5000); // ✅ UI stays responsive
}, ThreadOption.BackgroundThread);
```

---

### ❌ Pitfall 2: Cross-Thread UI Access

**Problem**: Updating UI from background thread throws exception

```csharp
// BAD: Throws InvalidOperationException
_eventAggregator.GetEvent<MyEvent>().Subscribe(payload =>
{
    Logs.Add("New log"); // ❌ Cross-thread exception!
}, ThreadOption.BackgroundThread);
```

**Solution**: Marshal to UI thread with Dispatcher

```csharp
// GOOD: Marshals to UI thread
_eventAggregator.GetEvent<MyEvent>().Subscribe(payload =>
{
    Application.Current.Dispatcher.Invoke(() =>
    {
        Logs.Add("New log"); // ✅ Safe
    });
}, ThreadOption.BackgroundThread);

// BETTER: Just use UIThread subscription
_eventAggregator.GetEvent<MyEvent>().Subscribe(payload =>
{
    Logs.Add("New log"); // ✅ Already on UI thread
}, ThreadOption.UIThread);
```

---

### ❌ Pitfall 3: Deadlock with UIThread

**Problem**: Publisher on UI thread waits for UIThread subscriber

```csharp
// BAD: Deadlock!
var tcs = new TaskCompletionSource<bool>();

_eventAggregator.GetEvent<MyEvent>().Subscribe(_ =>
{
    Thread.Sleep(5000); // Blocks UI thread
    tcs.SetResult(true);
}, ThreadOption.UIThread);

_eventAggregator.GetEvent<MyEvent>().Publish(new MyPayload());
await tcs.Task; // 😱 DEADLOCK - UI thread blocked by subscriber!
```

**Solution**: Don't wait for UIThread subscribers, or use BackgroundThread

```csharp
// GOOD: Don't wait
_eventAggregator.GetEvent<MyEvent>().Subscribe(_ =>
{
    // Do work
}, ThreadOption.UIThread);

_eventAggregator.GetEvent<MyEvent>().Publish(new MyPayload());
// Continue without waiting

// OR: Use BackgroundThread if you need to wait
var tcs = new TaskCompletionSource<bool>();
_eventAggregator.GetEvent<MyEvent>().Subscribe(_ =>
{
    Task.Delay(5000).ContinueWith(_ => tcs.SetResult(true));
}, ThreadOption.BackgroundThread);

_eventAggregator.GetEvent<MyEvent>().Publish(new MyPayload());
await tcs.Task; // ✅ No deadlock - subscriber on background thread
```

---

### ❌ Pitfall 4: Race Conditions with BackgroundThread

**Problem**: Multiple background handlers access shared state without synchronization

```csharp
private int _counter = 0;

// BAD: Race condition!
_eventAggregator.GetEvent<MyEvent>().Subscribe(_ =>
{
    _counter++; // ❌ Not thread-safe
}, ThreadOption.BackgroundThread);
```

**Solution**: Use thread-safe operations or locks

```csharp
private int _counter = 0;
private readonly object _lock = new object();

// GOOD: Thread-safe
_eventAggregator.GetEvent<MyEvent>().Subscribe(_ =>
{
    lock (_lock)
    {
        _counter++; // ✅ Thread-safe
    }
}, ThreadOption.BackgroundThread);

// OR: Use Interlocked for simple operations
_eventAggregator.GetEvent<MyEvent>().Subscribe(_ =>
{
    Interlocked.Increment(ref _counter); // ✅ Thread-safe
}, ThreadOption.BackgroundThread);
```

---

## Best Practices

### ✅ Practice 1: Match ThreadOption to Operation Type

| Operation Type | ThreadOption | Reason |
|---------------|--------------|--------|
| UI updates | `UIThread` | Safe, no Dispatcher needed |
| Fast operations (< 10ms) | `PublisherThread` | Simple, predictable |
| API calls | `BackgroundThread` | Non-blocking |
| Database queries | `BackgroundThread` | Non-blocking |
| File I/O | `BackgroundThread` | Non-blocking |
| Heavy computation | `BackgroundThread` | Doesn't block UI |

---

### ✅ Practice 2: Use CancellationToken for Long Operations

```csharp
private CancellationTokenSource _cts = new CancellationTokenSource();

_eventAggregator.GetEvent<LongOperationEvent>().Subscribe(async payload =>
{
    try
    {
        // Support cancellation
        await Task.Delay(10000, _cts.Token);
    }
    catch (OperationCanceledException)
    {
        Log("Operation cancelled", "WARNING");
    }
}, ThreadOption.BackgroundThread);

// Cancel if needed
_cts.Cancel();
```

---

### ✅ Practice 3: Handle Exceptions in Async Handlers

```csharp
_eventAggregator.GetEvent<MyEvent>().Subscribe(async payload =>
{
    try
    {
        await DoWorkAsync();
    }
    catch (Exception ex)
    {
        // Log error, publish failure event
        Log($"Error: {ex.Message}", "ERROR");
        _eventAggregator.GetEvent<OperationFailedEvent>().Publish(...);
    }
}, ThreadOption.BackgroundThread);
```

---

### ✅ Practice 4: Unsubscribe When Done

```csharp
private SubscriptionToken _subscription;

public void Subscribe()
{
    _subscription = _eventAggregator.GetEvent<MyEvent>().Subscribe(Handler);
}

public void Unsubscribe()
{
    _subscription?.Dispose(); // Prevents memory leaks
}
```

---

### ✅ Practice 5: Document Thread Requirements

```csharp
/// <summary>
/// Handles payment processing.
/// 
/// THREADING:
/// - Subscribes with ThreadOption.BackgroundThread
/// - Executes on background thread (non-blocking)
/// - Cannot update UI directly (must use Dispatcher)
/// - Safe for HTTP calls and database operations
/// </summary>
private async void OnProcessPayment(PaymentPayload payload)
{
    // Implementation
}
```

---

## Performance Comparison

### Benchmark Results

| ThreadOption | Handler Time | Publisher Wait | UI Responsive | Use Case |
|-------------|-------------|----------------|---------------|----------|
| PublisherThread | 10ms | ✅ Yes (10ms) | ❌ If on UI | Fast ops |
| PublisherThread | 1000ms | ✅ Yes (1000ms) | ❌ Frozen | Avoid! |
| UIThread | 10ms | ❌ No | ✅ Yes | UI updates |
| UIThread | 1000ms | ❌ No | ✅ Yes* | UI updates |
| BackgroundThread | 10ms | ❌ No | ✅ Yes | Async ops |
| BackgroundThread | 1000ms | ❌ No | ✅ Yes | Long ops |

*\*UI responsive but handler executes later via Dispatcher queue*

### Memory Considerations

- **PublisherThread**: No extra threads created
- **UIThread**: Uses WPF Dispatcher (already exists)
- **BackgroundThread**: Uses ThreadPool threads (limited resource)

**Recommendation**: Don't create too many concurrent BackgroundThread operations. Use semaphore if needed:

```csharp
private static SemaphoreSlim _semaphore = new SemaphoreSlim(5, 5); // Max 5 concurrent

_eventAggregator.GetEvent<MyEvent>().Subscribe(async payload =>
{
    await _semaphore.WaitAsync();
    try
    {
        await DoWorkAsync();
    }
    finally
    {
        _semaphore.Release();
    }
}, ThreadOption.BackgroundThread);
```

---

## Summary Cheat Sheet

### Quick Decision Tree

```
Need to update UI?
├─ YES → Use ThreadOption.UIThread
└─ NO
   ├─ Fast operation (< 10ms)?
   │  ├─ YES → Use ThreadOption.PublisherThread (default)
   │  └─ NO → Use ThreadOption.BackgroundThread
   └─ Long operation (API, DB, file)?
      └─ YES → Use ThreadOption.BackgroundThread + async/await
```

### Code Templates

#### UI Update Pattern
```csharp
_eventAggregator.GetEvent<MyEvent>().Subscribe(payload =>
{
    // Safe to update UI
    MyObservableCollection.Add(payload.Item);
}, ThreadOption.UIThread);
```

#### Async Operation Pattern
```csharp
_eventAggregator.GetEvent<MyEvent>().Subscribe(async payload =>
{
    try
    {
        var result = await HttpClient.GetAsync(url);
        // Process result
    }
    catch (Exception ex)
    {
        // Handle error
    }
}, ThreadOption.BackgroundThread);
```

#### Fast Operation Pattern
```csharp
_eventAggregator.GetEvent<MyEvent>().Subscribe(payload =>
{
    // Quick operation
    _cache[payload.Key] = payload.Value;
});
// No ThreadOption = PublisherThread (default)
```

---

## Testing Your Understanding

### Quiz

1. **Which ThreadOption should you use to update an ObservableCollection?**
   - Answer: `ThreadOption.UIThread`

2. **What happens if you update UI from BackgroundThread without Dispatcher?**
   - Answer: `InvalidOperationException` (cross-thread access)

3. **Does PublisherThread block the publisher?**
   - Answer: Yes, publisher waits for handler to complete

4. **Can multiple BackgroundThread handlers run in parallel?**
   - Answer: Yes, they use ThreadPool threads

5. **What's the default ThreadOption?**
   - Answer: `ThreadOption.PublisherThread`

---

**Next Steps:**
1. Click each demo button in the application
2. Observe the log output and timing
3. Try changing ThreadOptions in the code
4. Experiment with combining different options
5. Apply these patterns to your own projects!

---

*Master ThreadOptions and you'll write responsive, thread-safe WPF applications!* 🚀
