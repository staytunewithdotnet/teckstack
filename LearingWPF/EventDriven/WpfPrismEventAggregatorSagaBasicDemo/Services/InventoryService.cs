using Prism.Events;
using WpfPrismEventAggregatorDemo.Events;

namespace WpfPrismEventAggregatorDemo.Services
{
    public class InventoryService
    {
        private readonly IEventAggregator _eventAggregator;

        public InventoryService(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;

            // Step 1 forward transaction: Listen to new orders
            _eventAggregator.GetEvent<OrderCreatedEvent>().Subscribe(OnOrderCreated);

            // Step 2 rollback transaction: Listen to payment failures to execute compensating logic
            _eventAggregator.GetEvent<PaymentFailedEvent>().Subscribe(OnPaymentFailed);
        }

        private void OnOrderCreated(OrderCreatedPayload payload)
        {
            Log($"Order {payload.OrderId}: Received. Attempting to reserve stock...", "INFO");

            // Simulate stock reservation logic (always succeeds in this demo)
            Log($"Order {payload.OrderId}: Stock successfully RESERVED for items.", "SUCCESS");

            // Publish success event for next step
            _eventAggregator.GetEvent<InventoryReservedEvent>().Publish(new InventoryReservedPayload
            {
                OrderId = payload.OrderId,
                ReservedItemsCount = 1
            });
        }

        private void OnPaymentFailed(PaymentFailedPayload payload)
        {
            Log($"Order {payload.OrderId}: Rollback signal received (Reason: {payload.Reason}).", "WARNING");
            
            // Execute Compensating Transaction: Release reserved inventory
            Log($"Order {payload.OrderId}: [COMPENSATING TRANSACTION] Releasing reserved stock back to inventory...", "ROLLBACK");
            Log($"Order {payload.OrderId}: Stock successfully RELEASED. Rollback complete.", "ROLLBACK");

            // Notify coordinator/UI that order is failed & rolled back
            _eventAggregator.GetEvent<OrderFailedEvent>().Publish(new OrderFailedPayload
            {
                OrderId = payload.OrderId,
                Reason = payload.Reason
            });
        }

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
