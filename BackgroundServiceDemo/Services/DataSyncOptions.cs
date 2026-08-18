using Microsoft.Extensions.Options;

namespace BackgroundServiceDemo.Services;

public class DataSyncOptions
{
    public int IntervalSeconds { get; set; } = 10;
    public string DataSource { get; set; } = "DefaultDB";
}
