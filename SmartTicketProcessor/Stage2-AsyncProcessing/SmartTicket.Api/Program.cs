using SmartTicket.Api.Exceptions;
using SmartTicket.Api.Models;
using SmartTicket.Api.Services;
using Microsoft.AspNetCore.RateLimiting;
using SmartTicket.Contracts.Events;

var builder = WebApplication.CreateBuilder(args);

// 1. Core Services
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

// 2. Application Services (In-memory for Stage 1, swap to Cosmos DB later)
builder.Services.AddSingleton<ITicketRepository, InMemoryTicketRepository>();

// Add Service Bus Publisher (Reads from config, defaults to local emulator)
var serviceBusConnectionString = builder.Configuration.GetValue<string>("ServiceBus:ConnectionString") 
    ?? "Endpoint=sb://127.0.0.1;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
var queueName = builder.Configuration.GetValue<string>("ServiceBus:QueueName") ?? "ticket-processing";

builder.Services.AddSingleton<IMessagePublisher>(sp => 
    new ServiceBusMessagePublisher(serviceBusConnectionString, queueName, sp.GetRequiredService<ILogger<ServiceBusMessagePublisher>>()));

// 3. API Features
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Optional: Add basic rate limiting for production readiness
builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("api", opt => 
{
    opt.PermitLimit = 100;
    opt.Window = TimeSpan.FromMinutes(1);
}));

var app = builder.Build();

// 4. Middleware Pipeline
app.UseExceptionHandler(); // Must be before other middleware
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 5. Endpoints
app.MapPost("/api/tickets", async (
    SubmitTicketRequest request,
    ITicketRepository repository,
    IMessagePublisher publisher, // <-- Injected
    TimeProvider timeProvider,
    CancellationToken ct) =>
{
    var ticketId = $"TKT-{Guid.NewGuid():N}".ToUpper()[..8]; // e.g., TKT-A1B2C3D4
    
    var ticket = new Ticket(
        Id: ticketId,
        CustomerId: request.CustomerId,
        Title: request.Title,
        Description: request.Description,
        Status: TicketStatus.Submitted,
        CreatedAt: timeProvider.GetUtcNow()
    );

    // 1. Save to database (In-memory for now)
    await repository.CreateAsync(ticket, ct);

    // 2. Publish to Service Bus
    var @event = new TicketSubmittedEvent(
        ticket.Id, ticket.CustomerId, ticket.Title, ticket.Description, ticket.CreatedAt);
    
    await publisher.PublishAsync(@event, ct);

    // 3. Return 202 Accepted immediately
    return Results.Accepted($"/api/tickets/{ticketId}", new 
    { 
        ticketId, 
        status = ticket.Status.ToString(), 
        statusUrl = $"/api/tickets/{ticketId}" 
    });
})
.WithName("SubmitTicket")
.RequireRateLimiting("api");

app.MapGet("/api/tickets/{id}", async (
    string id, 
    ITicketRepository repository, 
    CancellationToken ct) =>
{
    var ticket = await repository.GetByIdAsync(id, ct);
    return ticket is not null ? Results.Ok(ticket) : Results.NotFound();
})
.WithName("GetTicket");

// Separate endpoint to test error handling
app.MapGet("/api/tickets/test-error", () =>
{
    throw new InvalidOperationException("This is a test exception to verify global error handling.");
})
.WithName("TestError");

// 6. Health Checks (Crucial for Kubernetes liveness/readiness probes later)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck");

app.MapGet("/ready", () => Results.Ok(new { status = "ready" }))
   .WithName("ReadinessCheck");

app.Run();
