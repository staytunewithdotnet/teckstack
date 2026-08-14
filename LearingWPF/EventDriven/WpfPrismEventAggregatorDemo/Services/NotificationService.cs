using Prism.Events;
using WpfPrismEventAggregatorDemo.Events;

namespace WpfPrismEventAggregatorDemo.Services
{
    /// <summary>
    /// NotificationService - Demonstrates who subscribes to PaymentProcessedEvent.
    /// 
    /// PURPOSE:
    /// This service shows what happens AFTER a saga completes successfully.
    /// It subscribes to PaymentProcessedEvent to perform post-completion actions.
    /// 
    /// REAL-WORLD EXAMPLES:
    /// - Send order confirmation email
    /// - Trigger shipping workflow
    /// - Update analytics/dashboard
    /// - Send push notification to mobile app
    /// - Generate invoice
    /// 
    /// WHY THIS IS IMPORTANT:
    /// The saga pattern handles the core transaction (reserve → pay).
    /// Once complete, other systems can react to the success without
    /// being part of the critical saga path.
    /// 
    /// This keeps the saga focused and allows optional side effects.
    /// </summary>
    public class NotificationService
    {
        private readonly IEventAggregator _eventAggregator;

        /// <summary>
        /// Initializes NotificationService and subscribes to payment success events.
        /// 
        /// SUBSCRIPTIONS:
        /// - PaymentProcessedEvent: Trigger notifications on successful payment
        /// - OrderFailedEvent: Trigger notifications on failed orders
        /// 
        /// THREADING:
        /// Uses default ThreadOption (PublisherThread) since operations are fast.
        /// If sending actual emails (slow), would use ThreadOption.BackgroundThread.
        /// </summary>
        /// <param name="eventAggregator">Prism's event aggregator</param>
        public NotificationService(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;

            Log("NotificationService initialized", "INFO");
            Log("Subscribing to PaymentProcessedEvent and OrderFailedEvent", "INFO");

            // ========================================================================
            // SUBSCRIBE TO SUCCESSFUL PAYMENTS
            // ========================================================================
            // This is the subscriber you asked about!
            // When OrderOrchestrator publishes PaymentProcessedEvent, this handler runs.
            // ========================================================================
            _eventAggregator.GetEvent<PaymentProcessedEvent>().Subscribe(OnPaymentProcessed);

            // Also subscribe to failures for customer notifications
            _eventAggregator.GetEvent<OrderFailedEvent>().Subscribe(OnOrderFailed);
        }

        /// <summary>
        /// Handles PaymentProcessedEvent - Called when saga completes successfully.
        /// 
        /// THIS ANSWERS YOUR QUESTION:
        /// "Who subscribes to PaymentProcessedEvent?"
        /// Answer: This NotificationService (and potentially many others in production)
        /// 
        /// ACTIONS TAKEN:
        /// 1. Log the successful transaction
        /// 2. Simulate sending confirmation email
        /// 3. Simulate updating customer dashboard
        /// 4. Could trigger shipping, analytics, etc.
        /// 
        /// NOTE: These are SIDE EFFECTS, not part of the core saga.
        /// If email fails, we don't rollback the payment - it's non-critical.
        /// </summary>
        /// <param name="payload">Contains OrderId and TransactionId from successful payment</param>
        private void OnPaymentProcessed(PaymentProcessedPayload payload)
        {
            Log($"📧 NOTIFICATION: Order {payload.OrderId} payment successful!", "SUCCESS");
            Log($"   Transaction ID: {payload.TransactionId}", "INFO");
            
            // ========================================================================
            // SIMULATED POST-SAGA ACTIONS
            // ========================================================================
            // In production, these would be real operations:
            
            // 1. Send confirmation email
            // await _emailService.SendOrderConfirmationAsync(payload.OrderId);
            Log($"   ✉️  Sending order confirmation email to customer...", "INFO");
            
            // 2. Update customer notification center
            // await _notificationHub.SendNotificationAsync(userId, "Order confirmed!");
            Log($"   🔔 Sending push notification to customer's mobile app...", "INFO");
            
            // 3. Trigger shipping workflow
            // _eventAggregator.GetEvent<StartShippingEvent>().Publish(...);
            Log($"   📦 Notifying warehouse to prepare shipment...", "INFO");
            
            // 4. Update analytics
            // _analyticsService.TrackSale(payload.OrderId, payload.Amount);
            Log($"   📊 Recording sale in analytics dashboard...", "INFO");
            
            Log($"   ✅ All notifications sent for Order {payload.OrderId}", "SUCCESS");
        }

        /// <summary>
        /// Handles OrderFailedEvent - Notify customer of failure.
        /// </summary>
        /// <param name="payload">Contains OrderId and failure reason</param>
        private void OnOrderFailed(OrderFailedPayload payload)
        {
            Log($"📧 NOTIFICATION: Order {payload.OrderId} failed", "ERROR");
            Log($"   Reason: {payload.Reason}", "WARNING");
            
            // Simulate sending failure notification
            Log($"   ✉️  Sending apology email to customer...", "INFO");
            Log($"   💳 Informing customer that card was not charged...", "INFO");
            Log($"   ✅ Failure notifications sent for Order {payload.OrderId}", "INFO");
        }

        /// <summary>
        /// Logs messages to transaction log.
        /// </summary>
        private void Log(string message, string type)
        {
            _eventAggregator.GetEvent<TransactionLogEvent>().Publish(new TransactionLogPayload
            {
                Message = $"[NotificationService] {message}",
                Type = type
            });
        }
    }
}
