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
    /// <summary>
    /// OrderViewModel - Manages order placement and displays transaction logs.
    /// 
    /// RESPONSIBILITIES:
    /// 1. Initiate saga workflow by publishing OrderCreatedEvent
    /// 2. Display real-time transaction logs from all services
    /// 3. Allow user to simulate payment failures for testing rollback
    /// 4. Provide commands for thread option demonstrations
    /// 
    /// THREADING:
    /// - Subscribes to TransactionLogEvent with ThreadOption.UIThread
    /// - This ensures safe updates to Logs ObservableCollection bound to UI
    /// - Without UIThread option, would need Dispatcher.Invoke for UI updates
    /// </summary>
    public class OrderViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ThreadOptionDemoService _threadDemoService;
        private string _orderName = string.Empty;
        private int _orderCounter = 1;
        private bool _simulatePaymentFailure;

        public OrderViewModel(IEventAggregator eventAggregator, ThreadOptionDemoService threadDemoService)
        {
            _eventAggregator = eventAggregator;
            _threadDemoService = threadDemoService;
            
            PlaceOrderCommand = new RelayCommand(PlaceOrder);
            ClearLogsCommand = new RelayCommand(ClearLogs);
            
            // Thread Option Demo Commands
            TestPublisherThreadCommand = new RelayCommand(() => _threadDemoService.TestPublisherThread());
            TestUIThreadCommand = new RelayCommand(() => _threadDemoService.TestUIThread());
            TestBackgroundThreadCommand = new RelayCommand(() => _threadDemoService.TestBackgroundThread());
            TestAsyncOperationCommand = new RelayCommand(async () => await _threadDemoService.TestAsyncOperation());
            TestMultipleSubscribersCommand = new RelayCommand(() => _threadDemoService.TestMultipleSubscribers());
            TestParallelExecutionCommand = new RelayCommand(() => _threadDemoService.TestParallelExecution());
            TestSequentialExecutionCommand = new RelayCommand(() => _threadDemoService.TestSequentialExecution());
            Logs = new ObservableCollection<string>();

            // ========================================================================
            // SUBSCRIBE TO TRANSACTION LOGS WITH UI THREAD OPTION
            // ========================================================================
            // ThreadOption.UIThread ensures this handler executes on WPF UI thread
            // This allows safe modification of Logs ObservableCollection without
            // needing Dispatcher.Invoke or worrying about cross-thread exceptions
            // 
            // Alternative approaches:
            // 1. Default (PublisherThread): Would need Application.Current.Dispatcher.Invoke()
            // 2. BackgroundThread: Cannot update UI at all without marshaling
            // 3. UIThread: Perfect for UI updates (current choice)
            // ========================================================================
            _eventAggregator.GetEvent<TransactionLogEvent>().Subscribe(
                OnTransactionLogReceived, 
                ThreadOption.UIThread
            );
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
                    // Update both basic and orchestrated payment services
                    PaymentService.SimulateFailure = value;
                    PaymentServiceOrchestrated.SimulateFailure = value;
                }
            }
        }

        public ObservableCollection<string> Logs { get; }

        // Original Commands
        public ICommand PlaceOrderCommand { get; }
        public ICommand ClearLogsCommand { get; }
        
        // Thread Option Demo Commands
        public ICommand TestPublisherThreadCommand { get; }
        public ICommand TestUIThreadCommand { get; }
        public ICommand TestBackgroundThreadCommand { get; }
        public ICommand TestAsyncOperationCommand { get; }
        public ICommand TestMultipleSubscribersCommand { get; }
        public ICommand TestParallelExecutionCommand { get; }
        public ICommand TestSequentialExecutionCommand { get; }

        /// <summary>
        /// Places an order and initiates the saga workflow.
        /// 
        /// SAGA INITIATION:
        /// This method publishes OrderCreatedEvent which starts the entire saga.
        /// In choreography-based pattern, this triggers a chain reaction:
        /// OrderCreated → InventoryReserved → PaymentProcessed (or PaymentFailed → Rollback)
        /// 
        /// THREADING:
        /// Executes on UI thread (button click handler).
        /// Event publishing is synchronous by default, so all subscribers execute
        /// before this method returns (unless they use BackgroundThread).
        /// </summary>
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

            // Legacy notification support (AuditService still uses this)
            var legacyPayload = new OrderPlacedPayload
            {
                OrderId = id,
                OrderName = OrderName,
                CreatedAt = DateTime.Now
            };
            _eventAggregator.GetEvent<OrderPlacedEvent>().Publish(legacyPayload);

            // New Saga Pattern workflow initiator
            // This starts either choreography OR orchestration depending on which services are registered
            _eventAggregator.GetEvent<OrderCreatedEvent>().Publish(new OrderCreatedPayload
            {
                OrderId = id,
                OrderName = OrderName
            });

            OrderName = string.Empty;
        }

        /// <summary>
        /// Handles transaction log events and displays them in the UI.
        /// 
        /// THREADING:
        /// This method runs on UI thread because we subscribed with ThreadOption.UIThread.
        /// We can safely modify the Logs ObservableCollection here.
        /// 
        /// If we used PublisherThread or BackgroundThread, we'd need:
        /// Application.Current.Dispatcher.Invoke(() => Logs.Insert(0, formattedLog));
        /// </summary>
        /// <param name="payload">Log message payload</param>
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
