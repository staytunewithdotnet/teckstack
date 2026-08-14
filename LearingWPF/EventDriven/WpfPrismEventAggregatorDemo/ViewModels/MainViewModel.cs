using Prism.Mvvm;
 
namespace WpfPrismEventAggregatorDemo.ViewModels
{
    public class MainViewModel : BindableBase
    {
        public OrderViewModel Order { get; }
        public NotificationViewModel Notification { get; }
        public DashboardViewModel Dashboard { get; }
 
        public MainViewModel(
            OrderViewModel orderViewModel,
            NotificationViewModel notificationViewModel,
            DashboardViewModel dashboardViewModel)
        {
            Order = orderViewModel;
            Notification = notificationViewModel;
            Dashboard = dashboardViewModel;
        }
    }
}
