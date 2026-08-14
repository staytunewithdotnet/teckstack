using Prism.Mvvm;
using WpfPrismEventAggregatorDemo.Services;

namespace WpfPrismEventAggregatorDemo.ViewModels
{
    /// <summary>
    /// DashboardViewModel - Displays order statistics.
    /// 
    /// IMPROVEMENT:
    /// Instead of subscribing to events directly (which risks missing events 
    /// if this ViewModel isn't loaded), we inject the OrderStateService.
    /// 
    /// The Service is a Singleton that captures ALL events at startup.
    /// This ViewModel just reads the current state from the Service.
    /// </summary>
    public class DashboardViewModel : BindableBase
    {
        private readonly OrderStateService _stateService;

        public DashboardViewModel(OrderStateService stateService)
        {
            _stateService = stateService;
            
            // Subscribe to state changes so UI updates in real-time
            _stateService.OnStateUpdated += OnStateUpdated;
        }

        public string TotalOrdersText => $"Total Orders Placed: {_stateService.TotalOrdersPlaced}";
        
        public string SuccessRateText 
        {
            get 
            {
                int total = _stateService.TotalSuccessfulPayments + _stateService.TotalFailedOrders;
                if (total == 0) return "Success Rate: N/A";
                
                double rate = ((double)_stateService.TotalSuccessfulPayments / total) * 100;
                return $"Success Rate: {rate:F1}% ({_stateService.TotalSuccessfulPayments}/{total})";
            }
        }

        private void OnStateUpdated()
        {
            // Refresh UI properties
            RaisePropertyChanged(nameof(TotalOrdersText));
            RaisePropertyChanged(nameof(SuccessRateText));
        }
    }
}
