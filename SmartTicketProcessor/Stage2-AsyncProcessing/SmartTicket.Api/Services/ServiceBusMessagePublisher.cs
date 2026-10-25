using System.Text.Json;
using Azure.Messaging.ServiceBus;
using SmartTicket.Contracts.Events;

namespace SmartTicket.Api.Services;

public interface IMessagePublisher
{
    Task PublishAsync(TicketSubmittedEvent @event, CancellationToken cancellationToken);
}

public sealed class ServiceBusMessagePublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<ServiceBusMessagePublisher> _logger;

    public ServiceBusMessagePublisher(string connectionString, string queueName, ILogger<ServiceBusMessagePublisher> logger)
    {
        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(queueName);
        _logger = logger;
    }

    public async Task PublishAsync(TicketSubmittedEvent @event, CancellationToken cancellationToken)
    {
        var message = new ServiceBusMessage(JsonSerializer.Serialize(@event))
        {
            // Crucial for distributed tracing and idempotency
            CorrelationId = @event.TicketId,
            MessageId = Guid.NewGuid().ToString(),
            Subject = "TicketSubmitted"
        };

        // Add custom application properties for routing/filtering later
        message.ApplicationProperties.Add("MessageType", "TicketSubmitted");
        message.ApplicationProperties.Add("SchemaVersion", "1.0");

        await _sender.SendMessageAsync(message, cancellationToken);
        _logger.LogInformation("Published message for TicketId: {TicketId}", @event.TicketId);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
