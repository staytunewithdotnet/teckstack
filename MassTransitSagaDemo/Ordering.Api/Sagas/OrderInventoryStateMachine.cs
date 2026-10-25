using Contracts;
using MassTransit;

namespace Ordering.Api.Sagas;

/// <summary>
/// State Machine that orchestrates the Order-Inventory workflow
/// Handles: SubmitOrder -> ReserveInventory -> (InventoryReserved | InventoryRejected)
/// Includes timeout handling and compensation patterns
/// </summary>
public sealed class OrderInventoryStateMachine : MassTransitStateMachine<OrderInventoryState>
{
    // States
    public State AwaitingInventory { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    // Events
    public Event<SubmitOrder> OrderSubmitted { get; private set; } = null!;
    public Event<InventoryReservedEvent> InventoryReserved { get; private set; } = null!;
    public Event<InventoryRejectedEvent> InventoryRejected { get; private set; } = null!;

    // Timeout Event (Triggers if we wait too long for inventory response)
    public Event<OrderTimeout> OrderTimeoutExpired { get; private set; } = null!;
    public Schedule<OrderInventoryState, OrderTimeout> OrderTimeoutSchedule { get; private set; } = null!;

    public OrderInventoryStateMachine()
    {
        InstanceState(x => x.CurrentState);

        // Configure Events with correlation
        Event(() => OrderSubmitted, e =>
        {
            e.CorrelateById(m => m.Message.OrderId);
            e.InsertOnInitial = true;
            e.SetSagaFactory(ctx => new OrderInventoryState
            {
                CorrelationId = ctx.Message.OrderId,
                OrderId = ctx.Message.OrderId,
                ProductId = ctx.Message.ProductId,
                Quantity = ctx.Message.Quantity,
                CreatedAt = DateTime.UtcNow
            });
        });
        
        Event(() => InventoryReserved, e => e.CorrelateById(m => m.Message.OrderId));
        Event(() => InventoryRejected, e => e.CorrelateById(m => m.Message.OrderId));
        
        // Configure Timeout event
        Event(() => OrderTimeoutExpired, e => 
        {
            e.CorrelateBy((saga, context) => saga.OrderId == context.Message.OrderId);
        });

        // INITIAL STATE - When saga is first created
        Initially(
            When(OrderSubmitted)
                .Then(ctx => 
                {
                    Console.WriteLine($"[SAGA] Starting Order {ctx.Saga.OrderId}");
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                // Send command to inventory queue
                .Send(new Uri("queue:inventory-reserve"), ctx => 
                    new ReserveInventoryCommand(
                        NewId.NextGuid(), 
                        ctx.Saga.OrderId, 
                        ctx.Saga.ProductId, 
                        ctx.Saga.Quantity))
                // Start the timeout timer (2 minutes)
                .Schedule(OrderTimeoutSchedule, ctx => new OrderTimeout(ctx.Saga.OrderId), ctx => TimeSpan.FromMinutes(2))
                .TransitionTo(AwaitingInventory)
        );

        // AWAITING INVENTORY STATE
        During(AwaitingInventory,
            // Success Path - Inventory Reserved
            When(InventoryReserved)
                .Then(ctx => 
                {
                    ctx.Saga.ReservationId = ctx.Message.ReservationId;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                    Console.WriteLine($"[SAGA] Inventory Reserved for Order {ctx.Saga.OrderId}, ReservationId: {ctx.Message.ReservationId}");
                })
                .Unschedule(OrderTimeoutSchedule) // Cancel the timeout timer
                // Publish success event to notify upstream services
                .Publish(ctx => new OrderCompletedEvent(ctx.Saga.OrderId, ctx.Message.ReservationId))
                .TransitionTo(Completed),

            // Failure Path - Inventory Rejected (Compensation)
            When(InventoryRejected)
                .Then(ctx => 
                {
                    ctx.Saga.FailureReason = ctx.Message.Reason;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                    Console.WriteLine($"[SAGA] Inventory Rejected for Order {ctx.Saga.OrderId}: {ctx.Message.Reason}");
                })
                .Unschedule(OrderTimeoutSchedule)
                // Publish failure event so other services can compensate (e.g., refund)
                .Publish(ctx => new OrderFailedEvent(ctx.Saga.OrderId, ctx.Message.Reason)) 
                .TransitionTo(Failed),

            // Timeout Path - No response from inventory service
            When(OrderTimeoutExpired)
                .Then(ctx => 
                {
                    ctx.Saga.FailureReason = "Inventory service timeout - no response within 2 minutes";
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                    Console.WriteLine($"[SAGA] TIMEOUT! Order {ctx.Saga.OrderId} took too long.");
                })
                .Publish(ctx => new OrderFailedEvent(ctx.Saga.OrderId, "Inventory service timeout"))
                .TransitionTo(Failed),

            // Idempotency: Handle duplicate SubmitOrder while waiting
            When(OrderSubmitted)
                .Then(ctx => 
                {
                    Console.WriteLine($"[SAGA] Duplicate SubmitOrder ignored for {ctx.Saga.OrderId} (already in AwaitingInventory state)");
                })
        );

        // COMPLETED STATE (Idempotency - prevent duplicate processing)
        During(Completed,
            When(OrderSubmitted)
                .Then(ctx => 
                {
                    Console.WriteLine($"[SAGA] Order {ctx.Saga.OrderId} already completed. Ignoring duplicate message.");
                }),
            When(InventoryReserved)
                .Then(ctx => 
                {
                    Console.WriteLine($"[SAGA] Duplicate InventoryReserved for completed order {ctx.Saga.OrderId}. Ignoring.");
                }),
            When(InventoryRejected)
                .Then(ctx => 
                {
                    Console.WriteLine($"[SAGA] Late InventoryRejected for completed order {ctx.Saga.OrderId}. Ignoring.");
                })
        );

        // FAILED STATE (Idempotency - prevent duplicate processing)
        During(Failed,
            When(OrderSubmitted)
                .Then(ctx => 
                {
                    Console.WriteLine($"[SAGA] Order {ctx.Saga.OrderId} already failed. Ignoring duplicate message.");
                }),
            When(InventoryReserved)
                .Then(ctx => 
                {
                    Console.WriteLine($"[SAGA] Late InventoryReserved for failed order {ctx.Saga.OrderId}. Ignoring.");
                }),
            When(InventoryRejected)
                .Then(ctx => 
                {
                    Console.WriteLine($"[SAGA] Duplicate InventoryRejected for failed order {ctx.Saga.OrderId}. Ignoring.");
                })
        );
    }
}

/// <summary>
/// Timeout message for saga expiration
/// </summary>
public record OrderTimeout(Guid OrderId);
