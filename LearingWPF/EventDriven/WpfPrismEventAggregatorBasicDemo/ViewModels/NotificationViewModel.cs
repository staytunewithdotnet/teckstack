using Prism.Events;
using Prism.Mvvm;
using WpfPrismEventAggregatorDemo.Events;
 
namespace WpfPrismEventAggregatorDemo.ViewModels
{
    public class NotificationViewModel : BindableBase
    {
        private string _message = "No notification yet.";
 
        public NotificationViewModel(IEventAggregator eventAggregator)
        {
            eventAggregator
                .GetEvent<OrderPlacedEvent>()
                .Subscribe(OnOrderPlaced, ThreadOption.UIThread);
        }
 
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }
 
        private void OnOrderPlaced(OrderPlacedPayload payload)
        {
            Message = $"Order placed: {payload.OrderName}, Order Id: {payload.OrderId}";
        }
    }
}
