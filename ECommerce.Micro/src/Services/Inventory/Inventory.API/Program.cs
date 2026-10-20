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
builder.Services.AddSingleton<InventoryStore>();
builder.Services.AddSingleton<InMemoryOutbox>();

// Outbox publisher - Singleton + HostedService (same instance)
builder.Services.AddSingleton<OutboxPublisher>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OutboxPublisher>());

// MassTransit + RabbitMQ (Aspire 13.5.3 fix)
builder.Services.AddMassTransit(massTransitConfig =>
{
    massTransitConfig.AddConsumer<OrderPlacedConsumer>();

    massTransitConfig.UsingRabbitMq((context, rabbitMqConfig) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("rabbitmq");

        // Aspire gives amqp://user:pass@host:port - use Uri overload
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
            // local dev outside Aspire
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

app.MapGet("/inventory/{productId:int}", (int productId, InventoryStore inventoryStore, HttpContext httpContext) =>
{
    var result = new
    {
        productId,
        stock = inventoryStore.GetStock(productId),
        correlationId = httpContext.GetHttpCorrelationId()
    };
    return Results.Ok(result);
});

app.Run();

// --- Domain ---

public class InventoryStore
{
    private readonly ConcurrentDictionary<int, int> _stockByProductId = new(
        new Dictionary<int, int> { [1] = 100, [2] = 200, [3] = 50 });

    private readonly object _lock = new();

    public int GetStock(int productId) =>
        _stockByProductId.TryGetValue(productId, out var quantity) ? quantity : 0;

    public bool TryReserve(int productId, int quantity)
    {
        lock (_lock)
        {
            var currentStock = GetStock(productId);
            if (currentStock < quantity) return false;

            _stockByProductId[productId] = currentStock - quantity;
            return true;
        }
    }
}

public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
{
    private readonly InventoryStore _inventoryStore;
    private readonly InMemoryOutbox _inMemoryOutbox;

    public OrderPlacedConsumer(InventoryStore inventoryStore, InMemoryOutbox inMemoryOutbox)
    {
        _inventoryStore = inventoryStore;
        _inMemoryOutbox = inMemoryOutbox;
    }

    public Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var orderPlaced = context.Message;

        if (_inventoryStore.TryReserve(orderPlaced.ProductId, orderPlaced.Qty))
        {
            var stockReserved = new StockReservedEvent(
                orderPlaced.OrderId,
                orderPlaced.ProductId,
                orderPlaced.Qty,
                orderPlaced.CorrelationId);

            _inMemoryOutbox.Add(nameof(StockReservedEvent), stockReserved);
        }
        else
        {
            var stockFailed = new StockFailedEvent(
                orderPlaced.OrderId,
                $"Out of stock for product {orderPlaced.ProductId}",
                orderPlaced.CorrelationId);

            _inMemoryOutbox.Add(nameof(StockFailedEvent), stockFailed);
        }

        return Task.CompletedTask;
    }
}