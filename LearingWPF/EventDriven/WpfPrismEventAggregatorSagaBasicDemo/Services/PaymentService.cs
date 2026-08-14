using System;
using Prism.Events;
using WpfPrismEventAggregatorDemo.Events;

namespace WpfPrismEventAggregatorDemo.Services
{
    public class PaymentService
    {
        private readonly IEventAggregator _eventAggregator;

        // Static flag controlled by the UI to simulate success or failure rollback scenario
        public static bool SimulateFailure { get; set; } = false;

        public PaymentService(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;

            // Listen to inventory reserved step
            _eventAggregator.GetEvent<InventoryReservedEvent>().Subscribe(OnInventoryReserved);
        }

        private void OnInventoryReserved(InventoryReservedPayload payload)
        {
            Log($"Order {payload.OrderId}: Received reservation. Processing payment...", "INFO");

            if (SimulateFailure)
            {
                Log($"Order {payload.OrderId}: Payment processing FAILED (Simulated error).", "ERROR");
                
                // Publish failure event to trigger compensating transactions (rollback)
                _eventAggregator.GetEvent<PaymentFailedEvent>().Publish(new PaymentFailedPayload
                {
                    OrderId = payload.OrderId,
                    Reason = "Declined - Insufficient funds / Simulated Error"
                });
            }
            else
            {
                string transactionId = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                Log($"Order {payload.OrderId}: Payment charged successfully. Txn: {transactionId}", "SUCCESS");

                // Publish success event
                _eventAggregator.GetEvent<PaymentProcessedEvent>().Publish(new PaymentProcessedPayload
                {
                    OrderId = payload.OrderId,
                    TransactionId = transactionId
                });
            }
        }

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
