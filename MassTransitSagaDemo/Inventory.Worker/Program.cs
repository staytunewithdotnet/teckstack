using MassTransit;
using Inventory.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Register the consumer
    x.AddConsumer<ReserveInventoryConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        // Configure RabbitMQ connection
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Configure the inventory reservation endpoint
        cfg.ReceiveEndpoint("inventory-reserve", e =>
        {
            e.ConfigureConsumer<ReserveInventoryConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// Remove the default Worker service - we're using MassTransit consumers instead
// builder.Services.AddHostedService<Worker>();

var host = builder.Build();

Console.WriteLine("Inventory.Worker is starting...");
Console.WriteLine("Listening for ReserveInventoryCommand messages on queue: inventory-reserve");

host.Run();
