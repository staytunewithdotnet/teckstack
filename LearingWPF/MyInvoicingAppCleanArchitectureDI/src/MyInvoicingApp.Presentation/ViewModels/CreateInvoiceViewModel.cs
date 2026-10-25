using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MyInvoicingApp.Application.UseCases;
using MyInvoicingApp.Presentation.Common;

namespace MyInvoicingApp.Presentation.ViewModels;

public class CreateInvoiceViewModel : INotifyPropertyChanged
{
    private readonly CreateInvoiceUseCase _createUseCase;

    private string _customerName = "";
    public string CustomerName { get => _customerName; set { _customerName = value; OnPropertyChanged(); } }

    private string _description = "";
    public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }

    private string _amountText = "";
    public string AmountText { get => _amountText; set { _amountText = value; OnPropertyChanged(); } }

    private string _status = "";
    public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }

    public ICommand SaveCommand { get; }
    public event EventHandler? InvoiceCreated;
    public event EventHandler? CloseRequested;

    public CreateInvoiceViewModel(CreateInvoiceUseCase createUseCase)
    {
        _createUseCase = createUseCase;
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync(), _ => !string.IsNullOrWhiteSpace(CustomerName) && decimal.TryParse(AmountText, out var a) && a > 0);
    }

    private async Task SaveAsync()
    {
        try
        {
            if (!decimal.TryParse(AmountText, out var amount)) { Status = "Invalid amount"; return; }
            var invoice = await _createUseCase.ExecuteAsync(CustomerName, amount, Description);
            Status = $"Saved: {invoice.Id}";
            InvoiceCreated?.Invoke(this, EventArgs.Empty);
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
