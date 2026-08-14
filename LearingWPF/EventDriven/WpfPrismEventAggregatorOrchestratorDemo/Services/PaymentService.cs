using System;
using Prism.Events;
using WpfPrismEventAggregatorDemo.Events;

namespace WpfPrismEventAggregatorDemo.Services
{
    /// <summary>
    /// PaymentService - Handles payment processing in the Saga workflow.
    /// 
    /// ROLE IN SAGA PATTERN:
    /// - Step 2 (Forward): Processes payment after inventory is reserved
    /// - Can trigger rollback by publishing PaymentFailedEvent if payment is declined
    /// 
    /// THREADING DEMONSTRATION:
    /// This service shows different ThreadOption approaches for learning purposes.
    /// See comments in constructor for detailed examples.
    /// 
    /// PRISM EVENTAGGREGATOR THREAD OPTIONS EXPLAINED:
    /// 
    /// The EventAggregator supports three threading models via ThreadOption enum:
    /// 
    /// 1. ThreadOption.PublisherThread (DEFAULT)
    ///    ----------------------------------------
    ///    Behavior: Subscriber executes on the same thread that published the event
    ///    Use Case: Fast, synchronous operations that don't block
    ///    Example: Simple data transformations, logging
    ///    
    ///    Code:
    ///    _eventAggregator.GetEvent<MyEvent>().Subscribe(Handler);
    ///    // Equivalent to:
    ///    _eventAggregator.GetEvent<MyEvent>().Subscribe(Handler, ThreadOption.PublisherThread);
    ///    
    ///    Pros: Simple, predictable execution order
    ///    Cons: Can block publisher if handler is slow
    /// 
    /// 2. ThreadOption.UIThread
    ///    ----------------------
    ///    Behavior: Subscriber executes on WPF UI thread via Dispatcher
    ///    Use Case: Updating UI elements (ObservableCollection, UI controls)
    ///    Example: Updating log display, showing notifications
    ///    
    ///    Code:
    ///    _eventAggregator.GetEvent<MyEvent>().Subscribe(Handler, ThreadOption.UIThread);
    ///    
    ///    Pros: Safe UI updates without Dispatcher.Invoke
    ///    Cons: Can cause deadlocks if publisher is also on UI thread and waiting
    ///          All UI subscribers execute sequentially (can be slow)
    /// 
    /// 3. ThreadOption.BackgroundThread
    ///    ------------------------------
    ///    Behavior: Subscriber executes on a background thread from ThreadPool
    ///    Use Case: Long-running operations (API calls, file I/O, database queries)
    ///    Example: Calling payment gateway API, processing files
    ///    
    ///    Code:
    ///    _eventAggregator.GetEvent<MyEvent>().Subscribe(Handler, ThreadOption.BackgroundThread);
    ///    
    ///    Pros: Doesn't block publisher or UI
    ///    Cons: Cannot directly update UI (must marshal back)
    ///          Handlers may execute in parallel (thread safety concerns)
    /// 
    /// REAL-WORLD SCENARIO EXAMPLES:
    /// 
    /// Scenario A: Quick In-Memory Operation (PublisherThread - DEFAULT)
    /// ------------------------------------------------------------------
    /// _eventAggregator.GetEvent<OrderCreatedEvent>().Subscribe(OnOrderCreated);
    /// private void OnOrderCreated(OrderCreatedPayload payload)
    /// {
    ///     // Fast operation: update in-memory cache
    ///     _cache.Add(payload.OrderId, payload);
    /// }
    /// 
    /// Scenario B: UI Update (UIThread)
    /// ---------------------------------
    /// _eventAggregator.GetEvent<TransactionLogEvent>().Subscribe(OnLogReceived, ThreadOption.UIThread);
    /// private void OnLogReceived(TransactionLogPayload payload)
    /// {
    ///     // Safe to update ObservableCollection bound to UI
    ///     Logs.Add(payload.Message);
    /// }
    /// 
    /// Scenario C: External API Call (BackgroundThread)
    /// -------------------------------------------------
    /// _eventAggregator.GetEvent<PaymentRequestEvent>().Subscribe(ProcessPayment, ThreadOption.BackgroundThread);
    /// private async void ProcessPayment(PaymentRequestPayload payload)
    /// {
    ///     // Non-blocking HTTP call to payment gateway
    ///     var response = await _httpClient.PostAsync("https://api.stripe.com/charge", content);
    ///     
    ///     // Must marshal back to UI thread if updating UI
    ///     Application.Current.Dispatcher.Invoke(() => 
    ///     {
    ///         StatusMessage = "Payment processed";
    ///     });
    /// }
    /// 
    /// COMMON PITFALLS:
    /// 
    /// ❌ DEADLOCK RISK:
    /// // Publisher on UI thread waits for subscriber
    /// var tcs = new TaskCompletionSource<bool>();
    /// _eventAggregator.GetEvent<MyEvent>().Subscribe(_ => 
    /// {
    ///     Thread.Sleep(5000); // Blocks UI thread!
    ///     tcs.SetResult(true);
    /// }, ThreadOption.UIThread);
    /// 
    /// _eventAggregator.GetEvent<MyEvent>().Publish(new MyPayload());
    /// await tcs.Task; // DEADLOCK! UI thread is blocked by subscriber
    /// 
    /// ✅ CORRECT APPROACH:
    /// _eventAggregator.GetEvent<MyEvent>().Subscribe(async _ => 
    /// {
    ///     await Task.Delay(5000); // Non-blocking
    /// }, ThreadOption.BackgroundThread);
    /// </summary>
    public class PaymentService
    {
        private readonly IEventAggregator _eventAggregator;

        /// <summary>
        /// Static flag controlled by UI to simulate success or failure scenarios.
        /// In production, this would be determined by actual payment gateway response.
        /// </summary>
        public static bool SimulateFailure { get; set; } = false;

        /// <summary>
        /// Initializes the PaymentService and subscribes to inventory reservation events.
        /// 
        /// CURRENT IMPLEMENTATION:
        /// Uses default ThreadOption (PublisherThread) for synchronous execution.
        /// This means OnInventoryReserved runs on the same thread that published InventoryReservedEvent.
        /// 
        /// TO MAKE ASYNCHRONOUS (Production Approach):
        /// Change subscription to:
        /// _eventAggregator.GetEvent<InventoryReservedEvent>().Subscribe(
        ///     OnInventoryReserved, 
        ///     ThreadOption.BackgroundThread
        /// );
        /// 
        /// And make the handler async:
        /// private async void OnInventoryReserved(InventoryReservedPayload payload)
        /// {
        ///     // Call external payment API without blocking
        ///     var result = await ProcessPaymentWithGatewayAsync(payload);
        ///     // Publish result event
        /// }
        /// </summary>
        /// <param name="eventAggregator">Prism's event aggregator for pub/sub messaging</param>
        public PaymentService(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;

            // =========================================================================
            // STEP 2 SUBSCRIPTION: Listen for inventory reservation completion
            // =========================================================================
            // Current: Synchronous execution on publisher's thread
            // To make async: Add ThreadOption.BackgroundThread parameter
            // =========================================================================
            _eventAggregator.GetEvent<InventoryReservedEvent>().Subscribe(OnInventoryReserved);
            
            // =========================================================================
            // THREADOPTION EXAMPLES (For Learning):
            // =========================================================================
            
            // Example 1: Background processing for long-running payment API call
            // Uncomment to test:
            /*
            _eventAggregator.GetEvent<InventoryReservedEvent>().Subscribe(
                async payload => 
                {
                    Log($"Order {payload.OrderId}: Processing on background thread...", "INFO");
                    
                    // Simulate async API call to payment gateway
                    await Task.Delay(2000); // Simulates network latency
                    
                    Log($"Order {payload.OrderId}: Payment API call completed", "SUCCESS");
                    
                    // Note: Cannot update UI directly from background thread
                    // Must use Application.Current.Dispatcher.Invoke() if needed
                }, 
                ThreadOption.BackgroundThread
            );
            */
            
            // Example 2: UI thread subscription for direct UI updates
            // Uncomment to test:
            /*
            _eventAggregator.GetEvent<InventoryReservedEvent>().Subscribe(
                payload => 
                {
                    // Safe to update UI-bound properties here
                    Log($"Order {payload.OrderId}: Direct UI update possible here", "INFO");
                }, 
                ThreadOption.UIThread
            );
            */
        }

        /// <summary>
        /// Handles InventoryReservedEvent - Step 2 of the Saga (Payment Processing).
        /// 
        /// PROCESS:
        /// 1. Receive inventory reservation confirmation
        /// 2. Attempt to process payment (simulated)
        /// 3a. On success: Publish PaymentProcessedEvent (saga completes successfully)
        /// 3b. On failure: Publish PaymentFailedEvent (triggers compensating transactions)
        /// 
        /// THREADING:
        /// - Currently runs on publisher's thread (default ThreadOption.PublisherThread)
        /// - For production with real payment gateway, use ThreadOption.BackgroundThread
        /// - Example async version shown in commented code below
        /// 
        /// ERROR HANDLING:
        /// - Simulates failure via SimulateFailure flag
        /// - In production, would catch exceptions from payment gateway
        /// - Should implement retry logic for transient failures (network timeouts, etc.)
        /// - Permanent failures (insufficient funds) should immediately trigger rollback
        /// 
        /// IDEMPOTENCY CONSIDERATION:
        /// Payment processing should be idempotent - charging same order twice is bad!
        /// In production, use idempotency keys with payment gateway.
        /// </summary>
        /// <param name="payload">Contains OrderId and ReservedItemsCount from inventory reservation</param>
        private void OnInventoryReserved(InventoryReservedPayload payload)
        {
            Log($"Order {payload.OrderId}: Received reservation. Processing payment...", "INFO");

            // =========================================================================
            // PAYMENT PROCESSING LOGIC
            // =========================================================================
            // This simulates calling a payment gateway (Stripe, PayPal, etc.)
            // 
            // PRODUCTION ASYNC VERSION:
            // ---------------------------------------------------------------------
            // private async void OnInventoryReserved(InventoryReservedPayload payload)
            // {
            //     try
            //     {
            //         Log($"Order {payload.OrderId}: Calling payment gateway...", "INFO");
            //         
            //         // Async HTTP call to payment gateway (non-blocking)
            //         var paymentResult = await _paymentGateway.ChargeAsync(
            //             amount: 99.99m,
            //             currency: "USD",
            //             orderId: payload.OrderId,
            //             idempotencyKey: $"order_{payload.OrderId}"
            //         );
            //         
            //         if (paymentResult.Success)
            //         {
            //             Log($"Order {payload.OrderId}: Payment successful", "SUCCESS");
            //             _eventAggregator.GetEvent<PaymentProcessedEvent>().Publish(...);
            //         }
            //         else
            //         {
            //             Log($"Order {payload.OrderId}: Payment declined", "ERROR");
            //             _eventAggregator.GetEvent<PaymentFailedEvent>().Publish(...);
            //         }
            //     }
            //     catch (HttpRequestException ex)
            //     {
            //         // Transient error - could retry
            //         Log($"Order {payload.OrderId}: Network error, will retry...", "WARNING");
            //         await RetryPaymentAsync(payload, maxRetries: 3);
            //     }
            //     catch (PaymentException ex)
            //     {
            //         // Permanent error - trigger rollback immediately
            //         Log($"Order {payload.OrderId}: Payment failed: {ex.Message}", "ERROR");
            //         _eventAggregator.GetEvent<PaymentFailedEvent>().Publish(...);
            //     }
            // }
            // =========================================================================

            if (SimulateFailure)
            {
                // =====================================================================
                // FAILURE PATH: Simulate payment decline
                // =====================================================================
                Log($"Order {payload.OrderId}: Payment processing FAILED (Simulated error).", "ERROR");
                
                // Publish failure event to trigger compensating transactions (rollback)
                // InventoryService will receive this and release the reserved stock
                _eventAggregator.GetEvent<PaymentFailedEvent>().Publish(new PaymentFailedPayload
                {
                    OrderId = payload.OrderId,
                    Reason = "Declined - Insufficient funds / Simulated Error"
                });
            }
            else
            {
                // =====================================================================
                // SUCCESS PATH: Simulate successful payment
                // =====================================================================
                string transactionId = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                Log($"Order {payload.OrderId}: Payment charged successfully. Txn: {transactionId}", "SUCCESS");

                // Publish success event - Saga completes successfully
                // No further steps in this simple demo (could add Shipping, Notification, etc.)
                _eventAggregator.GetEvent<PaymentProcessedEvent>().Publish(new PaymentProcessedPayload
                {
                    OrderId = payload.OrderId,
                    TransactionId = transactionId
                });
            }
        }

        /// <summary>
        /// Publishes transaction log events for monitoring and debugging.
        /// 
        /// These logs are consumed by OrderViewModel for display in UI.
        /// Subscription in OrderViewModel uses ThreadOption.UIThread to safely update UI.
        /// </summary>
        /// <param name="message">Descriptive log message</param>
        /// <param name="type">Log level/category (INFO, SUCCESS, ERROR, WARNING, ROLLBACK)</param>
        private void Log(string message, string type)
        {
            _eventAggregator.GetEvent<TransactionLogEvent>().Publish(new TransactionLogPayload
            {
                Message = $"[PaymentService] {message}",
                Type = type
            });
        }
    }
}
