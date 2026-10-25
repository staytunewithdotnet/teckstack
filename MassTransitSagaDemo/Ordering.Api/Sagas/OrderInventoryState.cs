using MassTransit;

namespace Ordering.Api.Sagas;

/// <summary>
/// Saga state for Order Inventory workflow
/// Implements ISaga and SagaStateMachineInstance for MassTransit persistence
/// </summary>
public class OrderInventoryState : SagaStateMachineInstance
{
    // Required by ISaga interface
    public Guid CorrelationId { get; set; }
    
    // Required for state persistence - tracks current state name
    public string CurrentState { get; set; } = null!;

    // Business Data
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    
    // State-specific data
    public Guid? ReservationId { get; set; }
    public string? FailureReason { get; set; }
    
    // Timeout tracking (required for Schedule)
    public Guid? OrderTimeoutTokenId { get; set; }
    
    // Timestamps for auditing
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
