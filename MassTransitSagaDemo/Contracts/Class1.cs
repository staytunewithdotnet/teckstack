namespace Contracts;

/// <summary>
/// Initial command/event that starts the Saga
/// </summary>
public record SubmitOrder(Guid OrderId, Guid ProductId, int Quantity);

/// <summary>
/// Command sent FROM Saga TO Inventory Service
/// </summary>
public record ReserveInventoryCommand(Guid CommandId, Guid OrderId, Guid ProductId, int Quantity);

/// <summary>
/// Event published BY Inventory Service when reservation succeeds
/// </summary>
public record InventoryReservedEvent(Guid OrderId, Guid ReservationId);

/// <summary>
/// Event published BY Inventory Service when reservation fails
/// </summary>
public record InventoryRejectedEvent(Guid OrderId, string Reason);

/// <summary>
/// Event published by Saga to notify upstream services of order failure
/// </summary>
public record OrderFailedEvent(Guid OrderId, string Reason);

/// <summary>
/// Event published by Saga to notify upstream services of order success
/// </summary>
public record OrderCompletedEvent(Guid OrderId, Guid ReservationId);
