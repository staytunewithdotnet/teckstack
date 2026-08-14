using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using MediatR;
using WpfMediatRLearningApp.Core;
using WpfMediatRLearningApp.Features.Customers.CreateCustomer;
using WpfMediatRLearningApp.Features.Customers.GetCustomers;
using WpfMediatRLearningApp.Models;

namespace WpfMediatRLearningApp.ViewModels
{
    /// <summary>
    /// Section 5.8: The ViewModel only knows about IMediator. 
    /// It does not know about Repositories or specific services.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IMediator _mediator;
        private string _customerName = string.Empty;
        private bool _isBusy;

        public ObservableCollection<Customer> Customers { get; } = new();

        public string CustomerName
        {
            get => _customerName;
            set { _customerName = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        // Commands delegate directly to MediatR
        public ICommand LoadCustomersCommand { get; }
        public ICommand AddCustomerCommand { get; }

        public MainViewModel(IMediator mediator)
        {
            _mediator = mediator;
            
            // Initialize commands
            LoadCustomersCommand = new RelayCommand(LoadCustomersAsync);
            AddCustomerCommand = new RelayCommand(AddCustomerAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(CustomerName));
        }

        private async Task LoadCustomersAsync()
        {
            SetBusy(true);
            try
            {
                // Section 6.1: Send a query to MediatR
                var customers = await _mediator.Send(new GetCustomersQuery());
                
                // Update UI-bound collection
                Customers.Clear();
                foreach (var customer in customers)
                {
                    Customers.Add(customer);
                }
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task AddCustomerAsync()
        {
            SetBusy(true);
            try
            {
                // Section 6.1: Send a command to MediatR
                var newCustomer = await _mediator.Send(new CreateCustomerCommand(CustomerName));
                
                // Optimistically add to UI or reload
                Customers.Add(newCustomer);
                CustomerName = string.Empty;
                
                // Raise CanExecuteChanged to update button state
                ((RelayCommand)AddCustomerCommand).RaiseCanExecuteChanged();
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool isBusy)
        {
            IsBusy = isBusy;
            // Update command state based on busy status
            ((RelayCommand)LoadCustomersCommand).RaiseCanExecuteChanged();
            ((RelayCommand)AddCustomerCommand).RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
