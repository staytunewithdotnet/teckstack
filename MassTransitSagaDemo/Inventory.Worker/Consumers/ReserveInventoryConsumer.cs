using Contracts;
using MassTransit;

namespace Inventory.Worker.Consumers;

/// <summary>
/// Consumer that processes inventory reservation commands
/// This is a participant in the saga workflow
/// </summary>
public class ReserveInventoryConsumer : IConsumer<ReserveInventoryCommand>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ReserveInventoryConsumer> _logger;

    public ReserveInventoryConsumer(IPublishEndpoint publishEndpoint, ILogger<ReserveInventoryConsumer> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReserveInventoryCommand> context)
    {
        var command = context.Message;
        
        _logger.LogInformation("[INVENTORY] Processing reservation for Order: {OrderId}, Product: {ProductId}, Quantity: {Quantity}", 
            command.OrderId, command.ProductId, command.Quantity);

        try
        {
            // Simulate business logic / database check
            // In production, this would query your inventory database
            bool isAvailable = await CheckInventoryAvailability(command.ProductId, command.Quantity);

            if (isAvailable)
            {
                var reservationId = Guid.NewGuid();
                
                _logger.LogInformation("[INVENTORY] ✅ Success! Reserving inventory for Order {OrderId}. ReservationId: {ReservationId}", 
                    command.OrderId, reservationId);
                
                // Simulate some processing time
                await Task.Delay(Random.Shared.Next(500, 2000));
                
                // Publish success event back to saga
                await _publishEndpoint.Publish(new InventoryReservedEvent(command.OrderId, reservationId));
                
                _logger.LogInformation("[INVENTORY] Published InventoryReservedEvent for Order {OrderId}", command.OrderId);
            }
            else
            {
                _logger.LogWarning("[INVENTORY] ❌ Failed! Insufficient stock for Order {OrderId}. Product {ProductId} out of stock.", 
                    command.OrderId, command.ProductId);
                
                // Publish failure event back to saga
                await _publishEndpoint.Publish(new InventoryRejectedEvent(
                    command.OrderId, 
                    $"Insufficient stock for product {command.ProductId}. Requested: {command.Quantity}"));
                
                _logger.LogInformation("[INVENTORY] Published InventoryRejectedEvent for Order {OrderId}", command.OrderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[INVENTORY] Error processing reservation for Order {OrderId}", command.OrderId);
            
            // Publish failure event on exception
            await _publishEndpoint.Publish(new InventoryRejectedEvent(
                command.OrderId, 
                $"Internal error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Simulates checking inventory availability
    /// In production, replace with actual database query
    /// </summary>
    private async Task<bool> CheckInventoryAvailability(Guid productId, int requestedQuantity)
    {
        // Simulate database call
        await Task.Delay(100);
        
        // For demo purposes: 
        // - Products ending in odd numbers are available
        // - Products ending in even numbers are out of stock
        // This allows testing both success and failure paths
        return productId.GetHashCode() % 2 != 0;
    }
}
