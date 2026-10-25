using System.Text.Json;
using Azure.Messaging.ServiceBus;
using SmartTicket.Contracts.Events;
using SmartTicket.Worker.Models;
using SmartTicket.Worker.Repositories;
using SmartTicket.Worker.Services;

namespace SmartTicket.Worker;

public sealed class TicketProcessingWorker : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly ITicketAnalyzer _analyzer;
    private readonly ITicketAnalysisRepository _repository;
    private readonly ILogger<TicketProcessingWorker> _logger;

    public TicketProcessingWorker(
        string connectionString, 
        string queueName, 
        ITicketAnalyzer analyzer,
        ITicketAnalysisRepository repository,
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
        
        // Initialize Cosmos DB (create database and container if needed)
        await _repository.InitializeAsync(stoppingToken);
        
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

            // 1. Call Azure AI
            var analysis = await _analyzer.AnalyzeAsync(@event.Title, @event.Description, args.CancellationToken);

            _logger.LogInformation("AI Analysis Complete for {TicketId}: {Category} / {Priority}", 
                ticketId, analysis.Category, analysis.Priority);
            
            // 3. Create document for Cosmos DB
            var document = TicketAnalysisDocument.FromEventAndAnalysis(
                eventId: args.Message.MessageId ?? Guid.NewGuid().ToString(),
                ticketId: ticketId,
                customerId: @event.CustomerId,
                title: @event.Title,
                description: @event.Description,
                analysis: analysis
            );

            // 4. Save to Cosmos DB
            await _repository.SaveAsync(document, args.CancellationToken);
            
            _logger.LogInformation("Saved analysis to Cosmos DB for TicketId: {TicketId}, CustomerId: {CustomerId}", 
                ticketId, @event.CustomerId);

            // 5. COMPLETE THE MESSAGE (Removes it from the queue)
            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            _logger.LogInformation("Successfully processed and completed message for TicketId: {TicketId}", ticketId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Processing cancelled for TicketId: {TicketId}", ticketId);
            throw; // Allow graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message for TicketId: {TicketId}", ticketId);
            
            // Abandoning releases the lock, allowing the message to be retried immediately
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
        
        // Dispose Cosmos DB client
        if (_repository is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        
        await base.StopAsync(cancellationToken);
    }
}
