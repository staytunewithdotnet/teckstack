// ============================================================================
// PROGRAM.CS: The Entry Point - Wires Everything Together
// ============================================================================
// This file configures:
// 1. Database connection (SQL Server)
// 2. MassTransit (RabbitMQ + Outbox + Inbox)
// 3. Health checks (for Kubernetes liveness/readiness probes)
// 4. API endpoints (create orders, check status)
// ============================================================================

using MassTransit;
using Microsoft.EntityFrameworkCore;
using SagaApp.Consumers;
using SagaApp.Data;
using SagaApp.Entities;
using SagaApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// 1. DATABASE SETUP
// ============================================================================
// Connect to SQL Server using connection string from environment variable
builder.Services.AddDbContext<SagaDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("sagadb");
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Enable retry logic (transient faults are common in distributed systems)
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    });
});

// ============================================================================
// 2. HEALTH CHECKS (Kubernetes Liveness/Readiness Probes)
// ============================================================================
// Kubernetes periodically checks these endpoints:
// - /health: Is the app alive? (Liveness)
// - /ready: Is the app ready to receive traffic? (Readiness)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SagaDbContext>(); // Checks if DB connection is healthy

// ============================================================================
// 3. MASSTRANSIT SETUP (RabbitMQ + Outbox + Inbox)
// ============================================================================
builder.Services.AddMassTransit(x =>
{
    // A. Register the OrderConsumer
    x.AddConsumer<OrderConsumer>();
    
    // B. Configure EntityFramework Outbox (Guaranteed Message Delivery)
    x.AddEntityFrameworkOutbox<SagaDbContext>(o =>
    {
        o.UseSqlServer(); // Store outbox messages in SQL Server
        o.UseBusOutbox(); // Background thread delivers to RabbitMQ
        
        // Polling interval: How often to check for unsent messages
        o.DuplicateDetectionWindow = TimeSpan.FromMinutes(10);
    });
    
    // C. Configure RabbitMQ Connection
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqHost = builder.Configuration.GetConnectionString("rabbitmq");
        cfg.Host(new Uri(rabbitMqHost!));
        
        // Configure the queue endpoint
        cfg.ReceiveEndpoint("order-queue", e =>
        {
            // PrefetchCount: How many messages to pull from RabbitMQ at once
            // Keep it low for claim pattern (we want pods to compete for each order)
            e.PrefetchCount = 10;
            
            // ConcurrentMessageLimit: How many messages this pod processes simultaneously
            // This controls how many parallel workers the consumer has
            e.ConcurrentMessageLimit = 5;
            
            // Apply Outbox to this endpoint (ensures idempotency)
            e.UseEntityFrameworkOutbox<SagaDbContext>(context);
            
            // Wire up the consumer
            e.ConfigureConsumer<OrderConsumer>(context);
        });
    });
});

// ============================================================================
// 4. REGISTER SERVICES
// ============================================================================
builder.Services.AddScoped<ClaimService>();

// ============================================================================
// 5. BUILD THE APP
// ============================================================================
var app = builder.Build();

// ============================================================================
// 6. HEALTH CHECK ENDPOINTS
// ============================================================================
app.MapHealthChecks("/health"); // Kubernetes liveness probe
app.MapHealthChecks("/ready");  // Kubernetes readiness probe

// ============================================================================
// 7. API ENDPOINTS
// ============================================================================

// POST /orders: Create a new order (triggers the workflow)
app.MapPost("/orders", async (SagaDbContext db, IBus bus) =>
{
    var orderId = Guid.NewGuid();
    var customerName = $"Customer-{Guid.NewGuid().ToString().Substring(0, 8)}";
    var totalAmount = new Random().Next(10, 100);
    
    // =====================================================================
    // OUTBOX PATTERN: Save data + publish message in ONE transaction
    // =====================================================================
    using var tx = await db.Database.BeginTransactionAsync();
    
    // 1. Save order to database
    var order = new Order
    {
        OrderId = orderId,
        Status = "Pending",
        CustomerName = customerName,
        TotalAmount = totalAmount,
        CreatedAt = DateTime.UtcNow
    };
    db.Orders.Add(order);
    await db.SaveChangesAsync();
    
    // 2. Publish message to RabbitMQ
    // MassTransit Outbox intercepts this and saves it to OutboxMessage table
    // (NOT sent to RabbitMQ yet - that happens in the background thread)
    await bus.Publish(new ProcessOrderCommand(orderId, ProductId: 1, Quantity: 1));
    
    // 3. Commit transaction
    // If this succeeds, the background Outbox service will guarantee delivery
    await tx.CommitAsync();
    
    return Results.Ok(new
    {
        orderId,
        status = "Pending",
        message = "Order created. Message sent to RabbitMQ via Outbox."
    });
})
.WithName("CreateOrder")
.WithOpenApi();

// GET /orders: See all orders (for debugging)
app.MapGet("/orders", async (SagaDbContext db) =>
{
    return await db.Orders
        .OrderByDescending(o => o.CreatedAt)
        .Take(50)
        .Select(o => new
        {
            o.OrderId,
            o.Status,
            o.LockedBy,
            o.LockedUntil,
            o.CustomerName,
            o.TotalAmount,
            o.CreatedAt
        })
        .ToListAsync();
});

// GET /claims: See which orders are currently claimed (for debugging)
app.MapGet("/claims", async (ClaimService claimService) =>
{
    return await claimService.GetClaimedOrdersAsync();
});

// GET /pods: See which pod is handling the request
app.MapGet("/pods", () =>
{
    var podName = Environment.GetEnvironmentVariable("POD_NAME") ?? "local-pod";
    return Results.Ok(new { podName, machineName = Environment.MachineName });
});

// ============================================================================
// 8. RUN THE APP
// ============================================================================
Console.WriteLine("Saga App starting...");
Console.WriteLine($"Pod Name: {Environment.GetEnvironmentVariable("POD_NAME") ?? "local-pod"}");
Console.WriteLine($"Listening on port 8080");

app.Run();
