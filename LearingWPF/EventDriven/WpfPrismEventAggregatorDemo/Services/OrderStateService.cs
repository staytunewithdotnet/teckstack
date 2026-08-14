using System;
using System.Collections.ObjectModel;
using Prism.Events;
using WpfPrismEventAggregatorDemo.Events;

namespace WpfPrismEventAggregatorDemo.Services
{
    /// <summary>
    /// OrderStateService - The "Single Source of Truth" for Order Data.
    /// 
    /// PROBLEM SOLVED:
    /// ViewModels are transient (created/destroyed on navigation). If an event 
    /// fires when no ViewModel is listening, the data is lost.
    /// 
    /// SOLUTION:
    /// This Singleton Service is initialized at App Startup. It listens to ALL 
    /// order events permanently. It maintains the state.
    /// 
    /// ViewModels simply read from this service when they are created.
    /// </summary>
    public class OrderStateService
    {
        private readonly IEventAggregator _eventAggregator;
        
        // State Storage
        public int TotalOrdersPlaced { get; private set; } = 0;
        public int TotalSuccessfulPayments { get; private set; } = 0;
        public int TotalFailedOrders { get; private set; } = 0;
        
        public ObservableCollection<string> OrderHistory { get; } = new ObservableCollection<string>();

        // Notification for ViewModels that are already active
        public event Action OnStateUpdated;

        public OrderStateService(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;

            // These subscriptions happen ONCE at app startup
            _eventAggregator.GetEvent<OrderCreatedEvent>().Subscribe(OnOrderCreated);
            _eventAggregator.GetEvent<PaymentProcessedEvent>().Subscribe(OnPaymentSuccess);
            _eventAggregator.GetEvent<OrderFailedEvent>().Subscribe(OnOrderFailed);
        }

        private void OnOrderCreated(OrderCreatedPayload payload)
        {
            TotalOrdersPlaced++;
            OrderHistory.Insert(0, $"Order #{payload.OrderId} Created: {payload.OrderName}");
            NotifyUi();
        }

        private void OnPaymentSuccess(PaymentProcessedPayload payload)
        {
            TotalSuccessfulPayments++;
            OrderHistory.Insert(0, $"Order #{payload.OrderId} Payment Success: {payload.TransactionId}");
            NotifyUi();
        }

        private void OnOrderFailed(OrderFailedPayload payload)
        {
            TotalFailedOrders++;
            OrderHistory.Insert(0, $"Order #{payload.OrderId} Failed: {payload.Reason}");
            NotifyUi();
        }

        private void NotifyUi()
        {
            // Tell any active ViewModels to refresh their display
            OnStateUpdated?.Invoke();
        }
    }
}
