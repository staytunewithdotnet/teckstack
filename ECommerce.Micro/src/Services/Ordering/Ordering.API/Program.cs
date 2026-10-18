using BuildingBlocks.Events;
using BuildingBlocks.Middleware;
using BuildingBlocks.Outbox;
using MassTransit;
using Serilog;
using System.Collections.Concurrent;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Host.UseSerilog();

// Application state
builder.Services.AddSingleton<OrderStore>();
builder.Services.AddSingleton<InMemoryOutbox>();

// Outbox publisher - Singleton + HostedService (same instance)
builder.Services.AddSingleton<OutboxPublisher>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OutboxPublisher>());

// MassTransit + RabbitMQ (Aspire 13.5.3 fix)
builder.Services.AddMassTransit(massTransitConfig =>
{
    massTransitConfig.AddConsumer<StockReservedConsumer>();
    massTransitConfig.AddConsumer<StockFailedConsumer>();

    massTransitConfig.UsingRabbitMq((context, rabbitMqConfig) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("rabbitmq");

        if (!string.IsNullOrWhiteSpace(connectionString) && connectionString.StartsWith("amqp://"))
        {
            rabbitMqConfig.Host(new Uri(connectionString));
        }
        else if (!string.IsNullOrWhiteSpace(connectionString))
        {
            rabbitMqConfig.Host(connectionString);
        }
        else
        {
            rabbitMqConfig.Host("localhost", "/", host =>
            {
                host.Username("guest");
                host.Password("guest");
            });
        }

        rabbitMqConfig.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.MapDefaultEndpoints();

app.MapPost("/orders", (CreateOrderRequest request, OrderStore orderStore, InMemoryOutbox outbox, HttpContext httpContext) =>
{
    var correlationId = httpContext.GetHttpCorrelationId();

    var order = new Order(
        Id: Guid.NewGuid(),
        ProductId: request.ProductId,
        Qty: request.Qty,
        Status: "Created",
        FailureReason: null,
        CreatedAt: DateTime.UtcNow,
        UpdatedAt: null,
        CorrelationId: correlationId);

    orderStore.Add(order);

    var orderPlacedEvent = new OrderPlacedEvent(
        order.Id,
        order.ProductId,
        order.Qty,
        correlationId,
        Guid.NewGuid());

    outbox.Add(nameof(OrderPlacedEvent), orderPlacedEvent);

    return Results.Accepted($"/orders/{order.Id}", new
    {
        orderId = order.Id,
        status = order.Status,
        correlationId
    });
});

app.MapGet("/orders/{orderId:guid}", (Guid orderId, OrderStore orderStore) =>
    orderStore.GetById(orderId) is { } order ? Results.Ok(order) : Results.NotFound());

app.MapGet("/orders", (OrderStore orderStore) =>
    Results.Ok(orderStore.GetAll()));

app.Run();

// --- Contracts ---

public record CreateOrderRequest(int ProductId, int Qty);

public record Order(
    Guid Id,
    int ProductId,
    int Qty,
    string Status,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string CorrelationId);

// --- Stores ---

public class OrderStore
{
    private readonly ConcurrentDictionary<Guid, Order> _ordersById = new();

    public void Add(Order order) => _ordersById[order.Id] = order;

    public Order? GetById(Guid orderId) =>
        _ordersById.TryGetValue(orderId, out var order) ? order : null;

    public IEnumerable<Order> GetAll() => _ordersById.Values;

    public void UpdateStatus(Guid orderId, string status, string? failureReason = null)
    {
        _ordersById.AddOrUpdate(
            orderId,
            _ => throw new KeyNotFoundException($"Order {orderId} not found"),
            (_, existingOrder) => existingOrder with
            {
                Status = status,
                FailureReason = failureReason ?? existingOrder.FailureReason,
                UpdatedAt = DateTime.UtcNow
            });
    }
}

// --- Consumers ---

public class StockReservedConsumer : IConsumer<StockReservedEvent>
{
    private readonly OrderStore _orderStore;

    public StockReservedConsumer(OrderStore orderStore) => _orderStore = orderStore;

    public Task Consume(ConsumeContext<StockReservedEvent> context)
    {
        _orderStore.UpdateStatus(context.Message.OrderId, "Completed");
        return Task.CompletedTask;
    }
}

public class StockFailedConsumer : IConsumer<StockFailedEvent>
{
    private readonly OrderStore _orderStore;

    public StockFailedConsumer(OrderStore orderStore) => _orderStore = orderStore;

    public Task Consume(ConsumeContext<StockFailedEvent> context)
    {
        _orderStore.UpdateStatus(context.Message.OrderId, "Failed", context.Message.Reason);
        return Task.CompletedTask;
    }
}