using Prism.Events;
using Prism.Mvvm;
using System;
using System.Windows.Input;
using WpfPrismEventAggregatorDemo.Events;
using WpfPrismEventAggregatorDemo.Infrastructure;
 
namespace WpfPrismEventAggregatorDemo.ViewModels
{
    public class OrderViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private string _orderName = string.Empty;
        private int _orderCounter = 1;
 
        public OrderViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            PlaceOrderCommand = new RelayCommand(PlaceOrder);
        }
 
        public string OrderName
        {
            get => _orderName;
            set => SetProperty(ref _orderName, value);
        }
 
        public ICommand PlaceOrderCommand { get; }
 
        private void PlaceOrder()
        {
            if (string.IsNullOrWhiteSpace(OrderName))
            {
                OrderName = "Sample Order";
            }
 
            var payload = new OrderPlacedPayload
            {
                OrderId = _orderCounter++,
                OrderName = OrderName,
                CreatedAt = DateTime.Now
            };
 
            _eventAggregator
                .GetEvent<OrderPlacedEvent>()
                .Publish(payload);
 
            OrderName = string.Empty;
        }
    }
}
