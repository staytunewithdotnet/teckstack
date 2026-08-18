using BackgroundServiceDemo.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<DataSyncWorker>();

// Add configuration for the worker
builder.Services.Configure<DataSyncOptions>(builder.Configuration.GetSection("DataSync"));

var host = builder.Build();
host.Run();
