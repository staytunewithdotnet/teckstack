using MassTransit;
using Microsoft.EntityFrameworkCore;
using Ordering.Api.Data;
using Ordering.Api.Sagas;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 1. Configure SQL Server for Saga state persistence
builder.Services.AddDbContext<SagaDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=localhost;Database=OrderingSagaDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;",
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null); // Resilience for transient failures
        }));

// 2. Configure MassTransit with RabbitMQ and EF Core Outbox/Inbox
builder.Services.AddMassTransit(x =>
{
    // Register the State Machine saga
    x.AddSagaStateMachine<OrderInventoryStateMachine, OrderInventoryState>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<SagaDbContext>();
            r.UseSqlServer();
            
            // Note: SqlServerLockStatementProvider is built into MassTransit.EntityFrameworkCore
            // when using UseSqlServer(), so no explicit configuration needed
        });

    x.UsingRabbitMq((context, cfg) =>
    {
        // Configure RabbitMQ connection
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Configure the Saga endpoint with Outbox/Inbox pattern
        cfg.ReceiveEndpoint("order-saga", e =>
        {
            e.ConfigureSaga<OrderInventoryState>(context);
            
            // 🌟 THE INBOX/OUTBOX PATTERN 🌟
            // This ensures that:
            // 1. Consuming the message AND saving the Saga state happen in the SAME database transaction
            // 2. Outgoing messages (like Send to inventory) are saved to OutboxMessage table
            // 3. Guaranteed delivery even if the app crashes right after processing
            e.UseEntityFrameworkOutbox<SagaDbContext>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

// Run migrations on startup (for demo purposes - in production, use proper migration strategy)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SagaDbContext>();
    
    Console.WriteLine("Applying database migrations...");
    db.Database.Migrate();
    Console.WriteLine("Database migrations applied successfully.");
}

Console.WriteLine("Ordering.Api is starting...");
Console.WriteLine("Swagger UI available at: https://localhost:5001/swagger");
Console.WriteLine("Health check available at: https://localhost:5001/api/orders/health");

app.Run();
