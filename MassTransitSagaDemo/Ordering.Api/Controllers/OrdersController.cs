using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Ordering.Api.Controllers;

/// <summary>
/// API controller to submit orders and trigger the saga workflow
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IPublishEndpoint publishEndpoint, ILogger<OrdersController> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <summary>
    /// Submit a new order - this will start the saga workflow
    /// POST: api/orders
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SubmitOrder([FromBody] SubmitOrderRequest request)
    {
        var orderId = Guid.NewGuid();
        
        _logger.LogInformation("Submitting order {OrderId} for product {ProductId}, quantity {Quantity}", 
            orderId, request.ProductId, request.Quantity);

        // Publish the SubmitOrder event to start the saga
        await _publishEndpoint.Publish(new SubmitOrder(
            orderId, 
            request.ProductId, 
            request.Quantity));

        _logger.LogInformation("Order {OrderId} submitted successfully. Saga workflow initiated.", orderId);

        return Accepted(new 
        { 
            OrderId = orderId,
            Status = "Processing",
            Message = "Order submitted. Tracking via saga workflow."
        });
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }
}

/// <summary>
/// Request model for submitting orders
/// </summary>
public record SubmitOrderRequest(Guid ProductId, int Quantity);
