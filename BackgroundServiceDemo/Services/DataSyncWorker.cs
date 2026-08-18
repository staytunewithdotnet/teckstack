using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BackgroundServiceDemo.Services;

public class DataSyncWorker : BackgroundService
{
    private readonly ILogger<DataSyncWorker> _logger;
    private readonly DataSyncOptions _options;

    public DataSyncWorker(
        ILogger<DataSyncWorker> logger,
        IOptions<DataSyncOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DataSyncWorker started at {Time}", DateTimeOffset.Now);
        _logger.LogInformation("Configuration: DataSource={DataSource}, Interval={Interval}s", 
            _options.DataSource, _options.IntervalSeconds);

        // Using PeriodicTimer as recommended in Section 3.4 of the guide
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Starting data synchronization cycle...");
                
                // Simulate work
                await PerformSyncAsync(stoppingToken);
                
                _logger.LogInformation("Data synchronization cycle completed successfully.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Cancellation requested. Stopping DataSyncWorker gracefully.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during data synchronization cycle.");
                // In a production app, you might want to implement a retry policy here
                // or move to a dead-letter state as per Section 8.2
            }
        }

        _logger.LogInformation("DataSyncWorker stopped at {Time}", DateTimeOffset.Now);
    }

    private async Task PerformSyncAsync(CancellationToken cancellationToken)
    {
        // Simulate database or API call
        await Task.Delay(2000, cancellationToken);
        
        _logger.LogDebug("Processed 150 records from {DataSource}", _options.DataSource);
    }
}
