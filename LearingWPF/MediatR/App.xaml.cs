using System.Windows;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WpfMediatRLearningApp.Behaviors;
using WpfMediatRLearningApp.Data;
using WpfMediatRLearningApp.ViewModels;

namespace WpfMediatRLearningApp
{
    /// <summary>
    /// Section 5.11: Configures the Host, registers Services, MediatR, and Behaviors.
    /// </summary>
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            // Configure Dependency Injection Container
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // 1. Register Application Services (Repositories)
                    services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();

                    // 2. Register MediatR
                    // Scans the current assembly for Handlers (IRequestHandler, INotificationHandler)
                    services.AddMediatR(cfg =>
                    {
                        cfg.RegisterServicesFromAssembly(typeof(App).Assembly);
                    });

                    // 3. Register Pipeline Behaviors (Logging)
                    // This ensures LoggingBehavior runs for EVERY request
                    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

                    // 4. Register ViewModels and Views
                    services.AddTransient<MainViewModel>();
                    services.AddTransient<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Start the DI Host
            await _host.StartAsync();

            // Resolve the MainWindow (which injects MainViewModel automatically)
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            // Gracefully shut down the host
            await _host.StopAsync();
            _host.Dispose();
            base.OnExit(e);
        }
    }
}
