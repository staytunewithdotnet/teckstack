using Prism.Events;
using WpfPrismEventAggregatorDemo.Events;

namespace WpfPrismEventAggregatorDemo.Services
{
    /// <summary>
    /// InventoryService - Handles inventory reservation and release in the Saga workflow.
    /// 
    /// ROLE IN SAGA PATTERN:
    /// - Step 1 (Forward): Reserves inventory when an order is created
    /// - Compensation (Rollback): Releases reserved inventory when payment fails
    /// 
    /// THREADING MODEL:
    /// - Subscribes with default ThreadOption (PublisherThread) - executes synchronously
    /// - This means OnOrderCreated runs on the same thread that published OrderCreatedEvent
    /// - For UI updates or long-running operations, use ThreadOption.UIThread or ThreadOption.BackgroundThread
    /// 
    /// EXAMPLES OF THREAD OPTIONS:
    /// 
    /// 1. PublisherThread (DEFAULT - Current Implementation):
    ///    _eventAggregator.GetEvent<OrderCreatedEvent>().Subscribe(OnOrderCreated);
    ///    → Executes on the publisher's thread (synchronous, blocking)
    ///    → Use when: Handler is fast and doesn't update UI
    /// 
    /// 2. UIThread:
    ///    _eventAggregator.GetEvent<OrderCreatedEvent>().Subscribe(OnOrderCreated, ThreadOption.UIThread);
    ///    → Executes on WPF UI thread
    ///    → Use when: Handler needs to update UI elements
    ///    → Warning: Can cause deadlocks if publisher is also on UI thread
    /// 
    /// 3. BackgroundThread:
    ///    _eventAggregator.GetEvent<OrderCreatedEvent>().Subscribe(OnOrderCreated, ThreadOption.BackgroundThread);
    ///    → Executes on a background thread from thread pool
    ///    → Use when: Handler performs long-running operations (API calls, file I/O)
    ///    → Warning: Cannot directly update UI; must marshal back to UI thread
    /// 
    /// REAL-WORLD EXAMPLE:
    /// If OnOrderCreated made an HTTP call to reserve inventory:
    /// 
    /// WRONG (blocks UI):
    /// _eventAggregator.GetEvent<OrderCreatedEvent>().Subscribe(OnOrderCreated); // Default = PublisherThread
    /// private void OnOrderCreated(OrderCreatedPayload payload)
    /// {
    ///     var result = httpClient.PostAsync(...).Result; // BLOCKS! Bad!
    /// }
    /// 
    /// RIGHT (async on background):
    /// _eventAggregator.GetEvent<OrderCreatedEvent>().Subscribe(OnOrderCreated, ThreadOption.BackgroundThread);
    /// private async void OnOrderCreated(OrderCreatedPayload payload)
    /// {
    ///     var result = await httpClient.PostAsync(...); // Non-blocking!
    /// }
    /// </summary>
    public class InventoryService
    {
        private readonly IEventAggregator _eventAggregator;

        /// <summary>
        /// Initializes the InventoryService and subscribes to saga events.
        /// 
        /// SUBSCRIPTION STRATEGY:
        /// - OrderCreatedEvent: Triggers forward transaction (reserve stock)
        /// - PaymentFailedEvent: Triggers compensating transaction (release stock)
        /// 
        /// Both subscriptions use default ThreadOption (PublisherThread), meaning they execute
        /// synchronously on whatever thread published the event.
        /// </summary>
        /// <param name="eventAggregator">Prism's event aggregator for pub/sub messaging</param>
        public InventoryService(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;

            // =============================================================================
            // FORWARD TRANSACTION SUBSCRIPTION
            // =============================================================================
            // When OrderCreatedEvent is published, this service will immediately handle it
            // on the publisher's thread (synchronous execution).
            // 
            // To make this asynchronous, we could use:
            // .Subscribe(OnOrderCreated, ThreadOption.BackgroundThread)
            // =============================================================================
            _eventAggregator.GetEvent<OrderCreatedEvent>().Subscribe(OnOrderCreated);

            // =============================================================================
            // COMPENSATING TRANSACTION SUBSCRIPTION (ROLLBACK)
            // =============================================================================
            // When PaymentFailedEvent is published, this service executes rollback logic
            // to release previously reserved inventory.
            // 
            // This ensures eventual consistency: if payment fails, inventory is restored.
            // =============================================================================
            _eventAggregator.GetEvent<PaymentFailedEvent>().Subscribe(OnPaymentFailed);
        }

        /// <summary>
        /// Handles OrderCreatedEvent - Step 1 of the Saga (Forward Transaction).
        /// 
        /// PROCESS:
        /// 1. Log receipt of order
        /// 2. Reserve inventory (simulated - always succeeds in demo)
        /// 3. Publish InventoryReservedEvent to trigger next saga step
        /// 
        /// THREADING:
        /// - Runs on publisher's thread (default ThreadOption.PublisherThread)
        /// - In production, this might call external inventory API
        /// - If async/await is needed, use ThreadOption.BackgroundThread
        /// 
        /// ERROR HANDLING:
        /// - Currently assumes success (demo simplification)
        /// - In production, should catch exceptions and publish failure event
        /// - Could implement retry logic for transient failures
        /// </summary>
        /// <param name="payload">Contains OrderId and OrderName from the order creation request</param>
        private void OnOrderCreated(OrderCreatedPayload payload)
        {
            Log($"Order {payload.OrderId}: Received. Attempting to reserve stock...", "INFO");

            // =========================================================================
            // SIMULATED INVENTORY RESERVATION
            // =========================================================================
            // In production, this would:
            // 1. Check database for available stock
            // 2. Lock inventory items to prevent double-selling
            // 3. Update inventory count
            // 4. Handle concurrency conflicts (optimistic/pessimistic locking)
            // 
            // Example with error handling:
            // try
            // {
            //     await _inventoryRepository.ReserveStockAsync(payload.OrderId);
            //     Log("Stock reserved successfully", "SUCCESS");
            //     _eventAggregator.GetEvent<InventoryReservedEvent>().Publish(...);
            // }
            // catch (InsufficientStockException ex)
            // {
            //     Log($"Insufficient stock: {ex.Message}", "ERROR");
            //     _eventAggregator.GetEvent<InventoryReservationFailedEvent>().Publish(...);
            // }
            // =========================================================================
            
            // Simulate stock reservation logic (always succeeds in this demo)
            Log($"Order {payload.OrderId}: Stock successfully RESERVED for items.", "SUCCESS");

            // Publish success event to trigger next step in saga (PaymentService)
            // This creates the sequential flow: OrderCreated → InventoryReserved → PaymentProcessed
            _eventAggregator.GetEvent<InventoryReservedEvent>().Publish(new InventoryReservedPayload
            {
                OrderId = payload.OrderId,
                ReservedItemsCount = 1
            });
        }

        /// <summary>
        /// Handles PaymentFailedEvent - Compensating Transaction (Rollback).
        /// 
        /// PURPOSE:
        /// Undo the inventory reservation made in OnOrderCreated to maintain consistency.
        /// This is the core of the Saga Pattern - every forward action needs a compensation.
        /// 
        /// PROCESS:
        /// 1. Log rollback initiation with failure reason
        /// 2. Release reserved inventory back to available stock
        /// 3. Publish OrderFailedEvent to notify system of complete failure
        /// 
        /// WHY THIS IS IMPORTANT:
        /// Without this compensation, we'd have "orphaned" reservations where inventory
        /// is held but payment never completed, preventing other customers from purchasing.
        /// 
        /// THREADING:
        /// - Runs on publisher's thread (whoever published PaymentFailedEvent)
        /// - Should be fast to avoid blocking the payment failure handling
        /// </summary>
        /// <param name="payload">Contains OrderId and Reason for payment failure</param>
        private void OnPaymentFailed(PaymentFailedPayload payload)
        {
            Log($"Order {payload.OrderId}: Rollback signal received (Reason: {payload.Reason}).", "WARNING");
            
            // =========================================================================
            // COMPENSATING TRANSACTION: Release Reserved Inventory
            // =========================================================================
            // This undoes the work done in OnOrderCreated.
            // 
            // In production, this would:
            // 1. Find the reservation record for this OrderId
            // 2. Release the locked inventory items
            // 3. Update inventory count back to original
            // 4. Log the compensation for audit trail
            // 
            // IMPORTANT: Compensation should be IDEMPOTENT
            // - Safe to call multiple times without side effects
            // - If already released, should not fail or double-release
            // =========================================================================
            
            Log($"Order {payload.OrderId}: [COMPENSATING TRANSACTION] Releasing reserved stock back to inventory...", "ROLLBACK");
            Log($"Order {payload.OrderId}: Stock successfully RELEASED. Rollback complete.", "ROLLBACK");

            // Notify coordinator/UI that order has completely failed and been rolled back
            // This is the terminal state for this saga instance
            _eventAggregator.GetEvent<OrderFailedEvent>().Publish(new OrderFailedPayload
            {
                OrderId = payload.OrderId,
                Reason = payload.Reason
            });
        }

        /// <summary>
        /// Publishes transaction log events for monitoring and debugging.
        /// 
        /// LOG LEVELS:
        /// - INFO: Normal operational messages
        /// - SUCCESS: Successful completion of a step
        /// - ERROR: Unrecoverable errors
        /// - WARNING: Potential issues or rollback initiation
        /// - ROLLBACK: Compensating transaction execution
        /// 
        /// These logs are consumed by OrderViewModel for display in UI.
        /// Subscription uses ThreadOption.UIThread to safely update ObservableCollection.
        /// </summary>
        /// <param name="message">Descriptive log message</param>
        /// <param name="type">Log level/category (INFO, SUCCESS, ERROR, WARNING, ROLLBACK)</param>
        private void Log(string message, string type)
        {
            _eventAggregator.GetEvent<TransactionLogEvent>().Publish(new TransactionLogPayload
            {
                Message = $"[InventoryService] {message}",
                Type = type
            });
        }
    }
}
