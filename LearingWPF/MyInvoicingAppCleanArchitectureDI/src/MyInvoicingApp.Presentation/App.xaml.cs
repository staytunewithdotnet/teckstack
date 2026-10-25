using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyInvoicingApp.Application.Interfaces;
using MyInvoicingApp.Application.UseCases;
using MyInvoicingApp.Infrastructure.Persistence;
using MyInvoicingApp.Infrastructure.Services;
using MyInvoicingApp.Presentation.ViewModels;
using MyInvoicingApp.Presentation.Views;

namespace MyInvoicingApp.Presentation;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static IHost Host { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Infrastructure - Singletons (factories, long-lived)
                services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();
                services.AddSingleton<IFileService, FileService>();
                services.AddSingleton<IInvoiceRepository, SqliteInvoiceRepository>();

                // Application - Transient (business logic per operation)
                services.AddTransient<CreateInvoiceUseCase>();
                services.AddTransient<GetAllInvoicesUseCase>();
                services.AddTransient<DeleteInvoiceUseCase>();

                // Presentation - Transient is CRITICAL for WPF to avoid memory leaks
                services.AddTransient<MainViewModel>();
                services.AddTransient<CreateInvoiceViewModel>();
                services.AddTransient<MainWindow>();
                services.AddTransient<CreateInvoiceWindow>();

                services.AddLogging(b => b.AddDebug().AddConsole());
            })
            .Build();

        Host.Start();
        Services = Host.Services;

        // Log DB location for learning
        var factory = Services.GetRequiredService<IDbConnectionFactory>();
        var logger = Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("SQLite DB Path: {Path}", factory.DbPath);

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Host.Dispose();
        base.OnExit(e);
    }
}
