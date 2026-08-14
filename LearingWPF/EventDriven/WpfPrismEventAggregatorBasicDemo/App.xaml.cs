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
            containerRegistry.RegisterSingleton<AuditService>();
            containerRegistry.Register<MainViewModel>();
            containerRegistry.Register<OrderViewModel>();
            containerRegistry.Register<NotificationViewModel>();
            containerRegistry.Register<DashboardViewModel>();
        }
 
        protected override void OnInitialized()
        {
            base.OnInitialized();
 
            // Force AuditService creation so it can subscribe to events.
            Container.Resolve<AuditService>();
        }
    }
}
