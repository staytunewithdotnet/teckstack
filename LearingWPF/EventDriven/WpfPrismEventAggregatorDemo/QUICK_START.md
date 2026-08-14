# ⚡ Quick Start Guide

> **Get started in 5 minutes!** Learn by doing with these hands-on exercises.

---

## 🎯 Exercise 1: See the Saga Pattern in Action (2 minutes)

### Step 1: Run the Application
```bash
# Open the solution in Visual Studio
# Press F5 to run
```

### Step 2: Place an Order (Success Path)
1. Type an order name (e.g., "Premium Widget")
2. Click **"Place Order"** button
3. Watch the logs on the right side

**Expected Logs:**
```
ℹ️ [OrderViewModel] Order 1 placed by user
ℹ️ [InventoryService] Order 1: Stock RESERVED
✅ [PaymentService] Order 1: Payment SUCCESSFUL
```

### Step 3: Test Failure & Rollback
1. Check the box: **"Simulate Payment Failure"**
2. Place another order
3. Watch the rollback in action!

**Expected Logs:**
```
ℹ️ [OrderViewModel] Order 2 placed by user
ℹ️ [InventoryService] Order 2: Stock RESERVED
❌ [PaymentService] Order 2: Payment FAILED
🔄 [ROLLBACK] [InventoryService] Order 2: Releasing stock
```

**🎓 What You Learned:** 
- Forward transaction (reserve → pay)
- Compensating transaction (rollback on failure)
- Event-driven workflow

---

## 🧵 Exercise 2: Explore ThreadOptions (3 minutes)

### Try All 5 Demo Buttons

Look for the **orange section** labeled "🧵 ThreadOption Demonstrations"

#### Button 1: PublisherThread
- Click it
- Notice UI freezes briefly (500ms)
- Log shows: "completed in 500ms (includes handler time)"
- **Lesson**: Publisher waits for handler (synchronous)

#### Button 2: UIThread
- Click it
- UI stays responsive
- Log appears after ~300ms
- **Lesson**: Safe for UI updates, runs async on UI thread

#### Button 3: BackgroundThread
- Click it
- UI fully responsive during 1000ms operation
- **Lesson**: Non-blocking, perfect for long operations

#### Button 4: Async Operation
- Click it
- Shows thread IDs before/after await
- **Lesson**: Async/await pattern with background threads

#### Button 5: Multiple Subscribers
- Click it
- See 3 subscribers execute sequentially
- Total time: ~600ms (3 × 200ms)
- **Lesson**: Multiple handlers run one after another

**🎓 What You Learned:**
- When to use each ThreadOption
- Blocking vs non-blocking behavior
- Thread safety considerations

---

## 🔧 Exercise 3: Enable Retry Logic (Optional)

### Step 1: Modify App.xaml.cs

Find this section and make the changes:

```csharp
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    containerRegistry.RegisterSingleton<InventoryService>();
    
    // COMMENT THIS OUT:
    // containerRegistry.RegisterSingleton<PaymentService>();
    
    // ADD THIS:
    containerRegistry.RegisterSingleton<PaymentServiceWithRetry>();
    
    containerRegistry.RegisterSingleton<ThreadOptionDemoService>();
}

protected override void OnInitialized()
{
    base.OnInitialized();
    
    Container.Resolve<InventoryService>();
    
    // COMMENT THIS OUT:
    // Container.Resolve<PaymentService>();
    
    // ADD THIS:
    Container.Resolve<PaymentServiceWithRetry>();
    
    Container.Resolve<ThreadOptionDemoService>();
}
```

### Step 2: Run and Test

1. Run the application
2. Place an order (normal success path works same as before)
3. Look for logs from `[PaymentServiceWithRetry]` instead of `[PaymentService]`

### Step 3: Test Retry Behavior (Advanced)

Add this temporarily to `PaymentServiceWithRetry.cs` constructor:

```csharp
public PaymentServiceWithRetry(IEventAggregator eventAggregator)
{
    _eventAggregator = eventAggregator;
    
    // ADD THESE LINES TO TEST RETRY:
    SimulateTransientFailure = true; // Will fail first 2 attempts, succeed on 3rd
    
    _eventAggregator.GetEvent<InventoryReservedEvent>().Subscribe(
        OnInventoryReservedAsync, 
        ThreadOption.BackgroundThread
    );
}
```

Run again and place an order. Watch the retry logic in action:

**Expected Logs:**
```
ℹ️ [PaymentServiceWithRetry] Order 1: Payment attempt 1/4
⚠️ [PaymentServiceWithRetry] Order 1: Transient error (attempt 1): Network timeout
⚠️ [PaymentServiceWithRetry] Order 1: Retrying in 1523ms...
ℹ️ [PaymentServiceWithRetry] Order 1: Payment attempt 2/4
⚠️ [PaymentServiceWithRetry] Order 1: Transient error (attempt 2): Network timeout
⚠️ [PaymentServiceWithRetry] Order 1: Retrying in 3847ms...
ℹ️ [PaymentServiceWithRetry] Order 1: Payment attempt 3/4
✅ [PaymentServiceWithRetry] Order 1: Payment successful
```

**🎓 What You Learned:**
- Exponential backoff (1s → 2s → 4s)
- Automatic retry for transient failures
- Circuit breaker pattern

---

## 🔄 Exercise 4: Try Orchestration (Optional)

### Step 1: Modify App.xaml.cs

Replace the service registrations:

```csharp
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // COMMENT OUT CHOREOGRAPHY SERVICES:
    // containerRegistry.RegisterSingleton<InventoryService>();
    // containerRegistry.RegisterSingleton<PaymentService>();
    
    // ADD ORCHESTRATION SERVICES:
    containerRegistry.RegisterSingleton<OrderOrchestrator>();
    containerRegistry.RegisterSingleton<InventoryServiceOrchestrated>();
    containerRegistry.RegisterSingleton<PaymentServiceOrchestrated>();
    
    containerRegistry.RegisterSingleton<ThreadOptionDemoService>();
}

protected override void OnInitialized()
{
    base.OnInitialized();
    
    // COMMENT OUT CHOREOGRAPHY SERVICES:
    // Container.Resolve<InventoryService>();
    // Container.Resolve<PaymentService>();
    
    // ADD ORCHESTRATION SERVICES:
    Container.Resolve<OrderOrchestrator>();
    Container.Resolve<InventoryServiceOrchestrated>();
    Container.Resolve<PaymentServiceOrchestrated>();
    
    Container.Resolve<ThreadOptionDemoService>();
}
```

### Step 2: Run and Compare

1. Run the application
2. Place an order
3. Observe the different log format!

**Orchestration Logs:**
```
ℹ️ [ORCHESTRATOR] Order 1: Received. Starting orchestrated saga...
ℹ️ [ORCHESTRATOR] Order 1: Step 1 - Sending RESERVE command
ℹ️ [ORCHESTRATED] Order 1: Received RESERVE command
✅ [ORCHESTRATED] Order 1: Stock reserved successfully
ℹ️ [ORCHESTRATOR] Order 1: Received inventory response: SUCCESS
ℹ️ [ORCHESTRATOR] Order 1: Step 2 - Sending CHARGE command
ℹ️ [ORCHESTRATED] Order 1: Received CHARGE command
✅ [ORCHESTRATED] Order 1: Payment SUCCESSFUL
ℹ️ [ORCHESTRATOR] Order 1: ✅ SUCCESS - All steps completed!
```

Notice how the orchestrator controls every step!

**🎓 What You Learned:**
- Centralized vs distributed control
- Command-response pattern
- Explicit workflow management

---

## 📚 Exercise 5: Deep Dive into Documentation (Self-Paced)

### Read in This Order:

1. **[SAGA_PATTERN_DOCUMENTATION.md](file://c:\AppCodeStore\AI-Model-Code\WPFEventDriven\WpfPrismEventAggregatorDemo\SAGA_PATTERN_DOCUMENTATION.md)**
   - Focus on Sections 1-4 first
   - Review interview questions before your interview

2. **[THREADOPTION_GUIDE.md](file://c:\AppCodeStore\AI-Model-Code\WPFEventDriven\WpfPrismEventAggregatorDemo\THREADOPTION_GUIDE.md)**
   - Read while clicking demo buttons
   - Study the "Common Pitfalls" section

3. **[CHOREOGRAPHY_VS_ORCHESTRATION.md](file://c:\AppCodeStore\AI-Model-Code\WPFEventDriven\WpfPrismEventAggregatorDemo\CHOREOGRAPHY_VS_ORCHESTRATION.md)**
   - After trying both implementations
   - Review the comparison tables

---

## ✅ Checklist: Did You Complete These?

- [ ] Ran the application and placed an order
- [ ] Tested payment failure and observed rollback
- [ ] Clicked all 5 ThreadOption demo buttons
- [ ] Read at least one documentation file
- [ ] Understood the difference between choreography and orchestration
- [ ] Can explain when to use each ThreadOption
- [ ] Reviewed interview questions

---

## 🎯 What to Focus On Based on Your Goal

### For Interview Preparation:
1. Read [SAGA_PATTERN_DOCUMENTATION.md](file://c:\AppCodeStore\AI-Model-Code\WPFEventDriven\WpfPrismEventAggregatorDemo\SAGA_PATTERN_DOCUMENTATION.md) - Section 6 (Interview Questions)
2. Understand the saga flow (forward + rollback)
3. Know the difference between choreography and orchestration
4. Be able to explain ThreadOptions

### For Learning WPF Threading:
1. Read [THREADOPTION_GUIDE.md](file://c:\AppCodeStore\AI-Model-Code\WPFEventDriven\WpfPrismEventAggregatorDemo\THREADOPTION_GUIDE.md)
2. Try all demo buttons multiple times
3. Study the code in `ThreadOptionDemoService.cs`
4. Practice in your own projects

### For Production Implementation:
1. Study `PaymentServiceWithRetry.cs` for retry patterns
2. Read about circuit breaker implementation
3. Understand async/await with EventAggregator
4. Review error handling strategies

### For Architecture Understanding:
1. Read [CHOREOGRAPHY_VS_ORCHESTRATION.md](file://c:\AppCodeStore\AI-Model-Code\WPFEventDriven\WpfPrismEventAggregatorDemo\CHOREOGRAPHY_VS_ORCHESTRATION.md)
2. Implement both patterns yourself
3. Compare pros and cons for your use case
4. Consider hybrid approaches

---

## 💡 Pro Tips

### Tip 1: Use Breakpoints
Set breakpoints in:
- `InventoryService.OnOrderCreated()` - See saga step 1
- `PaymentService.OnInventoryReserved()` - See saga step 2
- `InventoryService.OnPaymentFailed()` - See compensation

### Tip 2: Watch Thread IDs
In the debug window, watch `Thread.CurrentThread.ManagedThreadId` change based on ThreadOption.

### Tip 3: Clear Logs Frequently
Click "Clear Log" button between tests to keep output clean.

### Tip 4: Read Code Comments
Every service has detailed comments explaining WHY, not just WHAT.

### Tip 5: Experiment!
- Change ThreadOptions and see what breaks
- Add new services
- Modify the workflow
- Break things and learn from errors

---

## 🚀 Ready for More?

After completing these exercises:

1. **Add a Shipping Service**: Create a third step in the saga
2. **Implement Timeouts**: Cancel sagas that take too long
3. **Add Persistence**: Store saga state in a database
4. **Create Unit Tests**: Test all scenarios automatically
5. **Build a Dashboard**: Visualize saga progress in real-time

---

**You're now ready to master event-driven architecture! Happy coding!** 🎉
