using System.Text.Json;
using Azure.Messaging.ServiceBus;
using SmartTicket.Contracts.Events;
using SmartTicket.Contracts.Models;
using SmartTicket.Infrastructure.Repositories;
using SmartTicket.Worker.Services;

namespace SmartTicket.Worker;

public sealed class TicketProcessingWorker : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly ITicketAnalyzer _analyzer;
    private readonly ITicketRepository _repository;
    private readonly ILogger<TicketProcessingWorker> _logger;

    public TicketProcessingWorker(
        string connectionString, 
        string queueName, 
        ITicketAnalyzer analyzer,
        ITicketRepository repository,
        ILogger<TicketProcessingWorker> logger)
    {
        var options = new ServiceBusClientOptions
        {
            TransportType = ServiceBusTransportType.AmqpWebSockets
        };
        
        var client = new ServiceBusClient(connectionString, options);
        
        var processorOptions = new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 3,
            AutoCompleteMessages = false,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)
        };

        _processor = client.CreateProcessor(queueName, processorOptions);
        _analyzer = analyzer;
        _repository = repository;
        _logger = logger;

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Service Bus Processor...");
        await _processor.StartProcessingAsync(stoppingToken);
        
        // Keep the worker running until the host is stopped
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();
        var ticketId = args.Message.CorrelationId ?? "Unknown";

        try
        {
            _logger.LogInformation("Processing message for TicketId: {TicketId}", ticketId);

            // 1. Deserialize
            var @event = JsonSerializer.Deserialize<TicketSubmittedEvent>(body);
            if (@event is null)
            {
                _logger.LogWarning("Invalid message format. Dead-lettering.");
                await args.DeadLetterMessageAsync(args.Message, "InvalidFormat", "Message could not be deserialized", cancellationToken: args.CancellationToken);
                return;
            }

            // 2. Get current ticket state (to get the ETag)
            var ticket = await _repository.GetByIdAsync(ticketId, @event.CustomerId, args.CancellationToken);
            if (ticket is null)
            {
                _logger.LogWarning("Ticket {Id} not found. Creating new ticket.", ticketId);
                
                // Create a new ticket if it doesn't exist
                var newTicket = new Ticket(
                    Id: ticketId,
                    CustomerId: @event.CustomerId,
                    Title: @event.Title,
                    Description: @event.Description,
                    Status: TicketStatus.Submitted,
                    CreatedAt: DateTimeOffset.UtcNow
                );
                
                await _repository.CreateAsync(newTicket, args.CancellationToken);
                ticket = newTicket;
            }
            
            // Check if already completed (idempotency check)
            if (ticket.Status == TicketStatus.Completed)
            {
                _logger.LogInformation("Ticket {Id} already completed. Completing message.", ticketId);
                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                return;
            }

            // 3. Call Azure AI
            var analysis = await _analyzer.AnalyzeAsync(@event.Title, @event.Description, args.CancellationToken);

            _logger.LogInformation("AI Analysis Complete for {TicketId}: {Category} / {Priority}", 
                ticketId, analysis.Category, analysis.Priority);
            
            // 4. Update Cosmos DB with Optimistic Concurrency
            var updated = await _repository.TryUpdateAnalysisAsync(
                ticketId, 
                @event.CustomerId, 
                ticket.ETag, 
                analysis, 
                args.CancellationToken);

            if (updated)
            {
                _logger.LogInformation("Successfully updated TicketId: {TicketId} with AI analysis", ticketId);
                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            }
            else
            {
                // If false, it was a concurrency conflict. The message was already processed.
                // We safely complete it to remove it from the queue.
                _logger.LogWarning("Concurrency conflict for {TicketId}. Message was already processed. Safely completing.", ticketId);
                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Processing cancelled for TicketId: {TicketId}", ticketId);
            throw; // Allow graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message for TicketId: {TicketId}", ticketId);
            
            // Abandoning releases the lock, allowing the message to be retried
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus error occurred. Source: {ErrorSource}, Entity: {Entity}", 
            args.ErrorSource, args.EntityPath);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Service Bus Processor...");
        await _processor.StopProcessingAsync(cancellationToken);
        await _processor.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
