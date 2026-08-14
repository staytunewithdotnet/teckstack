using Prism.Ioc;
using Prism.Unity;
using System.Windows;
using WpfPrismEventAggregatorDemo.Services;
using WpfPrismEventAggregatorDemo.ViewModels;
 
namespace WpfPrismEventAggregatorDemo
{
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }
 
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // ========================================================================
            // STATE MANAGEMENT SERVICES (Singletons)
            // ========================================================================
            // These services hold the "Truth" of the application.
            // They are initialized once and live forever, ensuring no events are missed.
            containerRegistry.RegisterSingleton<OrderStateService>();
            
            // ========================================================================
            // ORIGINAL CHOREOGRAPHY-BASED SERVICES
            // ========================================================================
            // These implement choreography-based saga pattern
            containerRegistry.RegisterSingleton<AuditService>();
            containerRegistry.RegisterSingleton<InventoryService>();
            containerRegistry.RegisterSingleton<PaymentService>();
            
            // ========================================================================
            // NOTIFICATION SERVICE - Subscribes to PaymentProcessedEvent!
            // ========================================================================
            // This service demonstrates WHO receives the PaymentProcessedEvent
            // It performs post-saga actions like sending emails, notifications, etc.
            containerRegistry.RegisterSingleton<NotificationService>();
            
            // ========================================================================
            // ENHANCED SERVICES WITH RETRY LOGIC
            // ========================================================================
            // Alternative payment service with retry, circuit breaker, and async support
            // Comment out PaymentService above and uncomment this to test:
            // containerRegistry.RegisterSingleton<PaymentServiceWithRetry>();
            
            // ========================================================================
            // ORCHESTRATION-BASED SAGA SERVICES
            // ========================================================================
            // Uncomment these to use orchestration instead of choreography
            // Note: You should use EITHER choreography OR orchestration, not both
            /*
            containerRegistry.RegisterSingleton<OrderOrchestrator>();
            containerRegistry.RegisterSingleton<InventoryServiceOrchestrated>();
            containerRegistry.RegisterSingleton<PaymentServiceOrchestrated>();
            */
            
            // ========================================================================
            // THREAD OPTION DEMONSTRATION SERVICE
            // ========================================================================
            // Demonstrates different threading models in Prism EventAggregator
            containerRegistry.RegisterSingleton<ThreadOptionDemoService>();
            
            // ========================================================================
            // VIEWMODELS
            // ========================================================================
            containerRegistry.Register<MainViewModel>();
            containerRegistry.Register<OrderViewModel>();
            containerRegistry.Register<NotificationViewModel>();
            containerRegistry.Register<DashboardViewModel>();
        }
 
        protected override void OnInitialized()
        {
            base.OnInitialized();
 
            // Force service creation so they can subscribe to events.
            
            // Original choreography services
            Container.Resolve<AuditService>();
            Container.Resolve<InventoryService>();
            Container.Resolve<PaymentService>();
            
            // Notification service - subscribes to PaymentProcessedEvent!
            Container.Resolve<NotificationService>();
            
            // Enhanced service with retry (uncomment to test)
            // Container.Resolve<PaymentServiceWithRetry>();
            
            // Orchestration services (uncomment to test orchestration pattern)
            // Container.Resolve<OrderOrchestrator>();
            // Container.Resolve<InventoryServiceOrchestrated>();
            // Container.Resolve<PaymentServiceOrchestrated>();
            
            // Thread option demo service
            Container.Resolve<ThreadOptionDemoService>();
        }
    }
}
