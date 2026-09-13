using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MyInvoicingApp.Application.UseCases;
using MyInvoicingApp.Application.Abstractions;

namespace MyInvoicingApp.Presentation.ViewModels;

public sealed class CreateInvoiceViewModel : INotifyPropertyChanged
{
    private readonly CreateInvoiceUseCase _createInvoice;
    private string _customerName = string.Empty;
    private decimal _amount;
    private string _message = string.Empty;
    private bool _isBusy;

    public CreateInvoiceViewModel(CreateInvoiceUseCase createInvoice)
    {
        _createInvoice = createInvoice;
        SaveCommand = new Helpers.AsyncRelayCommand(SaveAsync, () => !IsBusy);
    }

    public string CustomerName
    {
        get => _customerName;
        set { _customerName = value; OnPropertyChanged(); }
    }

    public decimal Amount
    {
        get => _amount;
        set { _amount = value; OnPropertyChanged(); }
    }

    public string Message
    {
        get => _message;
        private set { _message = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set { _isBusy = value; OnPropertyChanged(); }
    }

    public ICommand SaveCommand { get; }

    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _createInvoice.ExecuteAsync(
                new CreateInvoiceRequest(CustomerName, Amount),
                CancellationToken.None);

            Message = $"Saved invoice {result.InvoiceNumber} for {result.DisplayAmount}";
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}