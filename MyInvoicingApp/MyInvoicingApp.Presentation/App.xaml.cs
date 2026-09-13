using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyInvoicingApp.Application.Abstractions;
using MyInvoicingApp.Application.UseCases;
using MyInvoicingApp.Infrastructure.Persistence;
using MyInvoicingApp.Infrastructure.System;
using MyInvoicingApp.Presentation.ViewModels;
using MyInvoicingApp.Presentation.Views;
using System.Windows;

namespace MyInvoicingApp.Presentation;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // Infrastructure implementations
                services.AddSingleton<IClock, SystemClock>();
                services.AddSingleton<IInvoiceRepository, InMemoryInvoiceRepository>();

                // Application layer
                services.AddTransient<CreateInvoiceUseCase>();

                // Presentation layer
                services.AddTransient<CreateInvoiceViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        _host.Start();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
            await _host.StopAsync();

        _host?.Dispose();
        base.OnExit(e);
    }
}