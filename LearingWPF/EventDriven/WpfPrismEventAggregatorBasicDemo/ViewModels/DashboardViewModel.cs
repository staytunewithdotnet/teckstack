using Prism.Events;
using Prism.Mvvm;
using WpfPrismEventAggregatorDemo.Events;
 
namespace WpfPrismEventAggregatorDemo.ViewModels
{
    public class DashboardViewModel : BindableBase
    {
        private int _totalOrders;
 
        public DashboardViewModel(IEventAggregator eventAggregator)
        {
            eventAggregator
                .GetEvent<OrderPlacedEvent>()
                .Subscribe(OnOrderPlaced, ThreadOption.UIThread);
        }
 
        public string TotalOrdersText => $"Total Orders Placed: {_totalOrders}";
 
        private void OnOrderPlaced(OrderPlacedPayload payload)
        {
            _totalOrders++;
            RaisePropertyChanged(nameof(TotalOrdersText));
        }
    }
}
