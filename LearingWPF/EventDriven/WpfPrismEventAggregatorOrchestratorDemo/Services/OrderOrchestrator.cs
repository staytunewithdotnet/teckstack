using System;
using System.Collections.Generic;
using Prism.Events;
using WpfPrismEventAggregatorDemo.Events;

namespace WpfPrismEventAggregatorDemo.Services
{
    /// <summary>
    /// OrderOrchestrator - Implements ORCHESTRATION-BASED SAGA PATTERN.
    /// 
    /// KEY DIFFERENCE FROM CHOREOGRAPHY:
    /// ----------------------------------
    /// CHOREOGRAPHY (Current Implementation):
    /// - Each service knows what to do next
    /// - Services communicate directly via events
    /// - No central coordinator
    /// - Flow: Service A → Event → Service B → Event → Service C
    /// - Harder to see full workflow
    /// - Tight coupling between services (they must know each other's events)
    /// 
    /// ORCHESTRATION (This Implementation):
    /// - Central orchestrator controls the flow
    /// - Services only respond to orchestrator commands
    /// - Orchestrator decides next step based on responses
    /// - Flow: Orchestrator → Command → Service A → Response → Orchestrator → Command → Service B
    /// - Easy to see and modify entire workflow
    /// - Loose coupling (services don't know about each other)
    /// 
    /// ANALOGY:
    /// - Choreography: Dance where each dancer knows their moves independently
    /// - Orchestration: Conductor directing an orchestra
    /// 
    /// WHEN TO USE ORCHESTRATION:
    /// - Complex workflows with many steps
    /// - Need centralized monitoring/logging
    /// - Conditional branching (if payment > $1000, require approval)
    /// - Easier to add/remove steps without changing services
    /// - Better for debugging and testing
    /// 
    /// WHEN TO USE CHOREOGRAPHY:
    /// - Simple workflows (2-3 steps)
    /// - Maximum loose coupling required
    /// - Services are truly independent
    /// - Performance critical (one less hop)
    /// </summary>
    public class OrderOrchestrator
    {
        private readonly IEventAggregator _eventAggregator;
        
        // Track active saga instances
        private readonly Dictionary<int, OrderSagaState> _activeSagas = new Dictionary<int, OrderSagaState>();

        public OrderOrchestrator(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            
            Log("OrderOrchestrator initialized (ORCHESTRATION-BASED SAGA)", "INFO");
            Log("This demonstrates centralized saga coordination vs distributed choreography", "INFO");
            
            // The orchestrator listens to ALL service responses
            // It then decides what to do next based on the response
            
            // Listen for service responses
            _eventAggregator.GetEvent<OrderCreatedEvent>().Subscribe(OnOrderCreated);
            _eventAggregator.GetEvent<InventoryOperationCompletedEvent>().Subscribe(OnInventoryResponse);
            _eventAggregator.GetEvent<PaymentOperationCompletedEvent>().Subscribe(OnPaymentResponse);
        }

        /// <summary>
        /// Handles order creation request - STARTS THE ORCHESTRATED SAGA.
        /// 
        /// ORCHESTRATION FLOW:
        /// 1. Receive OrderCreatedEvent
        /// 2. Create saga state to track progress
        /// 3. Send command to InventoryService to reserve stock
        /// 4. Wait for response
        /// 5. Based on response, either proceed to payment or rollback
        /// 
        /// This is different from choreography where InventoryService would
        /// automatically subscribe to OrderCreatedEvent and act independently.
        /// Here, the orchestrator explicitly tells InventoryService what to do.
        /// </summary>
        /// <param name="payload">Order creation details</param>
        private void OnOrderCreated(OrderCreatedPayload payload)
        {
            Log($"[ORCHESTRATOR] Order {payload.OrderId}: Received. Starting orchestrated saga...", "INFO");
            
            // Create saga state to track this order's progress
            var sagaState = new OrderSagaState
            {
                OrderId = payload.OrderId,
                OrderName = payload.OrderName,
                CurrentStep = SagaStep.Started,
                StartedAt = DateTime.Now
            };
            
            _activeSagas[payload.OrderId] = sagaState;
            
            // STEP 1: Tell InventoryService to reserve stock
            // In orchestration, we send explicit commands, not just events
            Log($"[ORCHESTRATOR] Order {payload.OrderId}: Step 1 - Sending RESERVE command to InventoryService", "INFO");
            
            sagaState.CurrentStep = SagaStep.ReservingInventory;
            
            // Publish command event (different from regular events - these are commands)
            _eventAggregator.GetEvent<ReserveInventoryCommand>().Publish(new ReserveInventoryCommandPayload
            {
                OrderId = payload.OrderId,
                OrderName = payload.OrderName,
                Quantity = 1,
                SagaId = Guid.NewGuid() // Track this specific command
            });
        }

        /// <summary>
        /// Handles inventory service response.
        /// 
        /// ORCHESTRATOR DECISION LOGIC:
        /// - If reservation succeeded → Proceed to payment
        /// - If reservation failed → Cancel order immediately
        /// 
        /// This is the key difference from choreography:
        /// The orchestrator makes the decision, not the InventoryService.
        /// </summary>
        /// <param name="payload">Inventory operation result payload</param>
        private void OnInventoryResponse(InventoryOperationCompletedPayload payload)
        {
            if (!_activeSagas.TryGetValue(payload.OrderId, out var sagaState))
            {
                Log($"[ORCHESTRATOR] Order {payload.OrderId}: ERROR - No active saga found!", "ERROR");
                return;
            }
            
            Log($"[ORCHESTRATOR] Order {payload.OrderId}: Received inventory response: {(payload.Success ? "SUCCESS" : "FAILED")}", "INFO");
            
            if (payload.Success)
            {
                // Inventory reserved successfully - proceed to payment
                Log($"[ORCHESTRATOR] Order {payload.OrderId}: Step 2 - Sending CHARGE command to PaymentService", "INFO");
                
                sagaState.CurrentStep = SagaStep.ProcessingPayment;
                sagaState.InventoryReserved = true;
                
                _eventAggregator.GetEvent<ChargePaymentCommand>().Publish(new ChargePaymentCommandPayload
                {
                    OrderId = payload.OrderId,
                    Amount = 99.99m, // In production, calculate from order
                    Currency = "USD",
                    SagaId = Guid.NewGuid()
                });
            }
            else
            {
                // Inventory reservation failed - cancel order
                Log($"[ORCHESTRATOR] Order {payload.OrderId}: Inventory reservation failed. Cancelling order.", "ERROR");
                
                CompleteSagaAsFailed(payload.OrderId, $"Inventory reservation failed: {payload.ErrorMessage}");
            }
        }

        /// <summary>
        /// Handles payment service response.
        /// 
        /// ORCHESTRATOR DECISION LOGIC:
        /// - If payment succeeded → Complete order successfully
        /// - If payment failed → Trigger compensation (release inventory)
        /// 
        /// The orchestrator coordinates the rollback, unlike choreography
        /// where InventoryService would listen for PaymentFailedEvent.
        /// </summary>
        /// <param name="payload">Payment operation result payload</param>
        private void OnPaymentResponse(PaymentOperationCompletedPayload payload)
        {
            if (!_activeSagas.TryGetValue(payload.OrderId, out var sagaState))
            {
                Log($"[ORCHESTRATOR] Order {payload.OrderId}: ERROR - No active saga found!", "ERROR");
                return;
            }
            
            Log($"[ORCHESTRATOR] Order {payload.OrderId}: Received payment response: {(payload.Success ? "SUCCESS" : "FAILED")}", "INFO");
            
            if (payload.Success)
            {
                // Payment successful - complete the saga
                Log($"[ORCHESTRATOR] Order {payload.OrderId}: ✅ SUCCESS - All steps completed!", "SUCCESS");
                
                sagaState.CurrentStep = SagaStep.Completed;
                sagaState.CompletedAt = DateTime.Now;
                
                // Publish final success event
                _eventAggregator.GetEvent<PaymentProcessedEvent>().Publish(new PaymentProcessedPayload
                {
                    OrderId = payload.OrderId,
                    TransactionId = payload.TransactionId ?? "UNKNOWN"
                });
                
                // Clean up completed saga
                _activeSagas.Remove(payload.OrderId);
            }
            else
            {
                // Payment failed - trigger compensation
                Log($"[ORCHESTRATOR] Order {payload.OrderId}: Payment failed. Initiating compensation...", "WARNING");
                
                sagaState.CurrentStep = SagaStep.Compensating;
                
                // Tell InventoryService to release the reserved stock
                Log($"[ORCHESTRATOR] Order {payload.OrderId}: Sending RELEASE command to InventoryService (compensation)", "ROLLBACK");
                
                _eventAggregator.GetEvent<ReleaseInventoryCommand>().Publish(new ReleaseInventoryCommandPayload
                {
                    OrderId = payload.OrderId,
                    Reason = $"Payment failed: {payload.ErrorMessage}",
                    SagaId = Guid.NewGuid()
                });
            }
        }

        /// <summary>
        /// Handles inventory release confirmation (compensation completion).
        /// </summary>
        /// <param name="payload">Inventory release result payload</param>
        private void OnInventoryReleaseResponse(InventoryOperationCompletedPayload payload)
        {
            if (!_activeSagas.TryGetValue(payload.OrderId, out var sagaState))
            {
                return;
            }
            
            Log($"[ORCHESTRATOR] Order {payload.OrderId}: Compensation complete. Order fully rolled back.", "ROLLBACK");
            
            CompleteSagaAsFailed(payload.OrderId, payload.ErrorMessage ?? "Payment failed and inventory released");
        }

        /// <summary>
        /// Marks a saga as failed and cleans up state.
        /// </summary>
        /// <param name="orderId">Order ID</param>
        /// <param name="reason">Failure reason</param>
        private void CompleteSagaAsFailed(int orderId, string reason)
        {
            if (_activeSagas.TryGetValue(orderId, out var sagaState))
            {
                sagaState.CurrentStep = SagaStep.Failed;
                sagaState.CompletedAt = DateTime.Now;
                sagaState.FailureReason = reason;
                
                // Publish failure event
                _eventAggregator.GetEvent<OrderFailedEvent>().Publish(new OrderFailedPayload
                {
                    OrderId = orderId,
                    Reason = reason
                });
                
                // Clean up
                _activeSagas.Remove(orderId);
            }
        }

        /// <summary>
        /// Logs messages with orchestrator prefix for easy identification.
        /// </summary>
        private void Log(string message, string type)
        {
            _eventAggregator.GetEvent<TransactionLogEvent>().Publish(new TransactionLogPayload
            {
                Message = message,
                Type = type
            });
        }

        // =============================================================================
        // SAGA STATE TRACKING
        // =============================================================================
        
        /// <summary>
        /// Tracks the state of an individual saga instance.
        /// In production, this would be persisted to database for durability.
        /// </summary>
        private class OrderSagaState
        {
            public int OrderId { get; set; }
            public string OrderName { get; set; }
            public SagaStep CurrentStep { get; set; }
            public bool InventoryReserved { get; set; }
            public DateTime StartedAt { get; set; }
            public DateTime? CompletedAt { get; set; }
            public string FailureReason { get; set; }
        }

        /// <summary>
        /// Defines the possible states of a saga.
        /// </summary>
        private enum SagaStep
        {
            Started,
            ReservingInventory,
            ProcessingPayment,
            Compensating,
            Completed,
            Failed
        }
    }

    // =============================================================================
    // COMMAND EVENTS (Orchestrator → Services)
    // =============================================================================
    // These are COMMANDS from orchestrator to services, different from domain events
    
    public class ReserveInventoryCommandPayload
    {
        public int OrderId { get; set; }
        public string OrderName { get; set; }
        public int Quantity { get; set; }
        public Guid SagaId { get; set; }
    }
    
    public class ReserveInventoryCommand : PubSubEvent<ReserveInventoryCommandPayload> { }
    
    public class ReleaseInventoryCommandPayload
    {
        public int OrderId { get; set; }
        public string Reason { get; set; }
        public Guid SagaId { get; set; }
    }
    
    public class ReleaseInventoryCommand : PubSubEvent<ReleaseInventoryCommandPayload> { }
    
    public class ChargePaymentCommandPayload
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public Guid SagaId { get; set; }
    }
    
    public class ChargePaymentCommand : PubSubEvent<ChargePaymentCommandPayload> { }

    // =============================================================================
    // RESPONSE EVENTS (Services → Orchestrator)
    // =============================================================================
    // Services publish these to report back to orchestrator
    
    public class InventoryOperationCompletedPayload
    {
        public int OrderId { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public Guid SagaId { get; set; }
    }
    
    public class InventoryOperationCompletedEvent : PubSubEvent<InventoryOperationCompletedPayload> { }
    
    public class PaymentOperationCompletedPayload
    {
        public int OrderId { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string TransactionId { get; set; }
        public Guid SagaId { get; set; }
    }
    
    public class PaymentOperationCompletedEvent : PubSubEvent<PaymentOperationCompletedPayload> { }
}
