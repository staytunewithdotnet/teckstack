using BuildingBlocks.Events;
using MassTransit;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Outbox;

public record OutboxItem(string MessageType, object Payload, Guid Id, DateTime CreatedAt);

public class InMemoryOutbox
{
    private readonly ConcurrentQueue<OutboxItem> _queue = new();

    public void Add(string messageType, object payload)
    {
        var item = new OutboxItem(messageType, payload, Guid.NewGuid(), DateTime.UtcNow);
        _queue.Enqueue(item);
    }

    public bool TryTake(out OutboxItem outboxItem) => _queue.TryDequeue(out outboxItem!);
}

public class OutboxPublisher : BackgroundService
{
    private readonly InMemoryOutbox _inMemoryOutbox;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public OutboxPublisher(InMemoryOutbox inMemoryOutbox, IServiceScopeFactory serviceScopeFactory)
    {
        _inMemoryOutbox = inMemoryOutbox;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_inMemoryOutbox.TryTake(out var outboxItem))
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                    if (outboxItem.Payload is OrderPlacedEvent orderPlaced)
                        await publishEndpoint.Publish(orderPlaced, cancellationToken);
                    else if (outboxItem.Payload is StockReservedEvent stockReserved)
                        await publishEndpoint.Publish(stockReserved, cancellationToken);
                    else if (outboxItem.Payload is StockFailedEvent stockFailed)
                        await publishEndpoint.Publish(stockFailed, cancellationToken);
                }
                catch
                {
                    // re-queue on failure
                    _inMemoryOutbox.Add(outboxItem.MessageType, outboxItem.Payload);
                    await Task.Delay(1000, cancellationToken);
                }
            }
            else
            {
                await Task.Delay(200, cancellationToken);
            }
        }
    }
}