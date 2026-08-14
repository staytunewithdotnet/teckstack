using System;
using Prism.Events;
using WpfPrismEventAggregatorDemo.Events;

namespace WpfPrismEventAggregatorDemo.Services
{
    /// <summary>
    /// PaymentServiceOrchestrated - Orchestrated version of payment service.
    /// 
    /// DIFFERENCE FROM CHOREOGRAPHY VERSION:
    /// - Does NOT subscribe to InventoryReservedEvent
    /// - Only responds to explicit CHARGE command from OrderOrchestrator
    /// - Reports results back to orchestrator via response events
    /// - Doesn't decide what happens next (orchestrator decides)
    /// - Completely unaware of inventory or other services
    /// 
    /// BENEFITS:
    /// - Single responsibility: just process payments
    /// - Easy to swap payment providers without affecting workflow
    /// - Can be tested independently
    /// - No knowledge of saga pattern or compensation logic
    /// </summary>
    public class PaymentServiceOrchestrated
    {
        private readonly IEventAggregator _eventAggregator;
        
        public static bool SimulateFailure { get; set; } = false;

        public PaymentServiceOrchestrated(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;

            Log("PaymentServiceOrchestrated initialized", "INFO");
            
            // Only subscribe to orchestrator commands
            _eventAggregator.GetEvent<ChargePaymentCommand>().Subscribe(OnChargePayment);
        }

        /// <summary>
        /// Handles CHARGE command from orchestrator.
        /// 
        /// PROCESS:
        /// 1. Receive explicit charge command with amount
        /// 2. Process payment (simulated)
        /// 3. Report result back to orchestrator
        /// 
        /// KEY DIFFERENCE FROM CHOREOGRAPHY:
        /// In choreography, this would listen for InventoryReservedEvent and decide
        /// whether to publish PaymentProcessedEvent or PaymentFailedEvent.
        /// Here, it just does what it's told and reports back.
        /// </summary>
        private void OnChargePayment(ChargePaymentCommandPayload payload)
        {
            Log($"[ORCHESTRATED] Order {payload.OrderId}: Received CHARGE command for {payload.Amount} {payload.Currency}", "INFO");
            
            try
            {
                if (SimulateFailure)
                {
                    // Simulate payment failure
                    Log($"[ORCHESTRATED] Order {payload.OrderId}: Payment DECLINED (simulated)", "ERROR");
                    
                    // Report failure to orchestrator - IT decides what to do next
                    _eventAggregator.GetEvent<PaymentOperationCompletedEvent>().Publish(new PaymentOperationCompletedPayload
                    {
                        OrderId = payload.OrderId,
                        Success = false,
                        ErrorMessage = "Payment declined - Insufficient funds",
                        SagaId = payload.SagaId
                    });
                }
                else
                {
                    // Simulate successful payment
                    string transactionId = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                    Log($"[ORCHESTRATED] Order {payload.OrderId}: Payment SUCCESSFUL. Txn: {transactionId}", "SUCCESS");
                    
                    // Report success to orchestrator
                    _eventAggregator.GetEvent<PaymentOperationCompletedEvent>().Publish(new PaymentOperationCompletedPayload
                    {
                        OrderId = payload.OrderId,
                        Success = true,
                        TransactionId = transactionId,
                        SagaId = payload.SagaId
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"[ORCHESTRATED] Order {payload.OrderId}: Payment error: {ex.Message}", "ERROR");
                
                _eventAggregator.GetEvent<PaymentOperationCompletedEvent>().Publish(new PaymentOperationCompletedPayload
                {
                    OrderId = payload.OrderId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    SagaId = payload.SagaId
                });
            }
        }

        private void Log(string message, string type)
        {
            _eventAggregator.GetEvent<TransactionLogEvent>().Publish(new TransactionLogPayload
            {
                Message = message,
                Type = type
            });
        }
    }
}
