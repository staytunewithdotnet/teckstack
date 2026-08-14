using Prism.Events;
using WpfPrismEventAggregatorDemo.Events;

namespace WpfPrismEventAggregatorDemo.Services
{
    /// <summary>
    /// InventoryServiceOrchestrated - Orchestrated version of inventory service.
    /// 
    /// DIFFERENCE FROM CHOREOGRAPHY VERSION:
    /// - Does NOT subscribe to OrderCreatedEvent directly
    /// - Only responds to explicit commands from OrderOrchestrator
    /// - Reports results back to orchestrator via response events
    /// - Doesn't know about PaymentService or next steps
    /// - Completely decoupled from workflow logic
    /// 
    /// BENEFITS:
    /// - Service only knows its own responsibility
    /// - Easy to test in isolation
    /// - Can be reused in different workflows
    /// - No coupling to other services' events
    /// </summary>
    public class InventoryServiceOrchestrated
    {
        private readonly IEventAggregator _eventAggregator;

        public InventoryServiceOrchestrated(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;

            Log("InventoryServiceOrchestrated initialized", "INFO");
            
            // Only subscribe to orchestrator commands, not domain events
            _eventAggregator.GetEvent<ReserveInventoryCommand>().Subscribe(OnReserveInventory);
            _eventAggregator.GetEvent<ReleaseInventoryCommand>().Subscribe(OnReleaseInventory);
        }

        /// <summary>
        /// Handles RESERVE command from orchestrator.
        /// 
        /// PROCESS:
        /// 1. Receive explicit command from orchestrator
        /// 2. Perform inventory reservation
        /// 3. Report result back to orchestrator (not publish domain event)
        /// 
        /// KEY DIFFERENCE:
        /// In choreography, this would publish InventoryReservedEvent for anyone to hear.
        /// In orchestration, it sends response ONLY to orchestrator.
        /// </summary>
        private void OnReserveInventory(ReserveInventoryCommandPayload payload)
        {
            Log($"[ORCHESTRATED] Order {payload.OrderId}: Received RESERVE command", "INFO");
            
            try
            {
                // Simulate inventory reservation
                Log($"[ORCHESTRATED] Order {payload.OrderId}: Reserving {payload.Quantity} item(s)...", "INFO");
                
                // In production: Check database, lock inventory, update counts
                // For demo: Always succeeds
                Log($"[ORCHESTRATED] Order {payload.OrderId}: Stock reserved successfully", "SUCCESS");
                
                // Report success back to orchestrator
                _eventAggregator.GetEvent<InventoryOperationCompletedEvent>().Publish(new InventoryOperationCompletedPayload
                {
                    OrderId = payload.OrderId,
                    Success = true,
                    SagaId = payload.SagaId
                });
            }
            catch (Exception ex)
            {
                Log($"[ORCHESTRATED] Order {payload.OrderId}: Reservation failed: {ex.Message}", "ERROR");
                
                // Report failure back to orchestrator
                _eventAggregator.GetEvent<InventoryOperationCompletedEvent>().Publish(new InventoryOperationCompletedPayload
                {
                    OrderId = payload.OrderId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    SagaId = payload.SagaId
                });
            }
        }

        /// <summary>
        /// Handles RELEASE command from orchestrator (compensation).
        /// 
        /// This is triggered when orchestrator decides to rollback due to payment failure.
        /// The service doesn't listen for PaymentFailedEvent - orchestrator tells it what to do.
        /// </summary>
        private void OnReleaseInventory(ReleaseInventoryCommandPayload payload)
        {
            Log($"[ORCHESTRATED] Order {payload.OrderId}: Received RELEASE command (Reason: {payload.Reason})", "ROLLBACK");
            
            try
            {
                // Simulate releasing reserved inventory
                Log($"[ORCHESTRATED] Order {payload.OrderId}: Releasing reserved stock...", "ROLLBACK");
                Log($"[ORCHESTRATED] Order {payload.OrderId}: Stock released successfully", "ROLLBACK");
                
                // Report compensation success to orchestrator
                _eventAggregator.GetEvent<InventoryOperationCompletedEvent>().Publish(new InventoryOperationCompletedPayload
                {
                    OrderId = payload.OrderId,
                    Success = true,
                    ErrorMessage = $"Released due to: {payload.Reason}",
                    SagaId = payload.SagaId
                });
            }
            catch (Exception ex)
            {
                Log($"[ORCHESTRATED] Order {payload.OrderId}: Release failed: {ex.Message}", "ERROR");
                
                _eventAggregator.GetEvent<InventoryOperationCompletedEvent>().Publish(new InventoryOperationCompletedPayload
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
