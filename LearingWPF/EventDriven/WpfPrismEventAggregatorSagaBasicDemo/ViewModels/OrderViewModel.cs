using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Events;
using Prism.Mvvm;
using WpfPrismEventAggregatorDemo.Events;
using WpfPrismEventAggregatorDemo.Infrastructure;
using WpfPrismEventAggregatorDemo.Services;

namespace WpfPrismEventAggregatorDemo.ViewModels
{
    public class OrderViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private string _orderName = string.Empty;
        private int _orderCounter = 1;
        private bool _simulatePaymentFailure;

        public OrderViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            PlaceOrderCommand = new RelayCommand(PlaceOrder);
            ClearLogsCommand = new RelayCommand(ClearLogs);

            Logs = new ObservableCollection<string>();

            // Subscribe to live transaction logging from the system
            _eventAggregator.GetEvent<TransactionLogEvent>().Subscribe(OnTransactionLogReceived, ThreadOption.UIThread);
        }

        public string OrderName
        {
            get => _orderName;
            set => SetProperty(ref _orderName, value);
        }

        public bool SimulatePaymentFailure
        {
            get => _simulatePaymentFailure;
            set
            {
                if (SetProperty(ref _simulatePaymentFailure, value))
                {
                    PaymentService.SimulateFailure = value;
                }
            }
        }

        public ObservableCollection<string> Logs { get; }

        public ICommand PlaceOrderCommand { get; }
        public ICommand ClearLogsCommand { get; }

        private void PlaceOrder()
        {
            if (string.IsNullOrWhiteSpace(OrderName))
            {
                OrderName = "Premium Item";
            }

            int id = _orderCounter++;
            
            // Log locally & publish OrderCreatedEvent to kick off the Saga
            _eventAggregator.GetEvent<TransactionLogEvent>().Publish(new TransactionLogPayload
            {
                Message = $"[OrderViewModel] Order {id} ({OrderName}) placed by user.",
                Type = "INFO"
            });

            // Legacy notification support
            var legacyPayload = new OrderPlacedPayload
            {
                OrderId = id,
                OrderName = OrderName,
                CreatedAt = DateTime.Now
            };
            _eventAggregator.GetEvent<OrderPlacedEvent>().Publish(legacyPayload);

            // New Saga Pattern workflow initiator
            _eventAggregator.GetEvent<OrderCreatedEvent>().Publish(new OrderCreatedPayload
            {
                OrderId = id,
                OrderName = OrderName
            });

            OrderName = string.Empty;
        }

        private void OnTransactionLogReceived(TransactionLogPayload payload)
        {
            string prefix = payload.Type switch
            {
                "SUCCESS" => "✅ ",
                "ERROR" => "❌ ",
                "ROLLBACK" => "🔄 [ROLLBACK] ",
                "WARNING" => "⚠️ ",
                _ => "ℹ️ "
            };

            Logs.Insert(0, $"{DateTime.Now:HH:mm:ss} - {prefix}{payload.Message}");
        }

        private void ClearLogs()
        {
            Logs.Clear();
        }
    }
}
