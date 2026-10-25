using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MyInvoicingApp.Application.DTOs;
using MyInvoicingApp.Application.Interfaces;
using MyInvoicingApp.Application.UseCases;
using MyInvoicingApp.Presentation.Common;
using MyInvoicingApp.Presentation.Views;

namespace MyInvoicingApp.Presentation.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly GetAllInvoicesUseCase _getAllUseCase;
    private readonly DeleteInvoiceUseCase _deleteUseCase;
    private readonly IFileService _fileService;
    private readonly IDbConnectionFactory _dbFactory;

    private ObservableCollection<InvoiceDto> _invoices = new();
    public ObservableCollection<InvoiceDto> Invoices { get => _invoices; set { _invoices = value; OnPropertyChanged(); } }

    private string _searchText = "";
    public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); } }

    private string _status = "Ready";
    public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }

    private InvoiceDto? _selectedInvoice;
    public InvoiceDto? SelectedInvoice { get => _selectedInvoice; set { _selectedInvoice = value; OnPropertyChanged(); } }

    public string DbPath => _dbFactory.DbPath;

    public ICommand LoadCommand { get; }
    public ICommand OpenCreateWindowCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand SearchCommand { get; }

    public MainViewModel(GetAllInvoicesUseCase getAllUseCase, DeleteInvoiceUseCase deleteUseCase, IFileService fileService, IDbConnectionFactory dbFactory)
    {
        _getAllUseCase = getAllUseCase;
        _deleteUseCase = deleteUseCase;
        _fileService = fileService;
        _dbFactory = dbFactory;

        LoadCommand = new AsyncRelayCommand(async _ => await LoadAsync());
        SearchCommand = new AsyncRelayCommand(async _ => await LoadAsync(SearchText));
        OpenCreateWindowCommand = new RelayCommand(_ => OpenCreateWindow());
        DeleteCommand = new AsyncRelayCommand(async _ => await DeleteAsync(), _ => SelectedInvoice != null);
        ExportCommand = new AsyncRelayCommand(async _ => await ExportAsync());

        // Initial load - do not block UI thread
        Task.Run(async () => await LoadAsync());
    }

    public async Task LoadAsync(string? search = null)
    {
        try
        {
            Status = "Loading...";
            var data = await _getAllUseCase.ExecuteAsync(search);
            // Must update ObservableCollection on UI thread
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Invoices = new ObservableCollection<InvoiceDto>(data);
            });
            Status = $"Loaded {data.Count} invoices | DB: {DbPath}";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }

    private void OpenCreateWindow()
    {
        // CRITICAL PATTERN: Create scope per child window to prevent memory leaks
        var scope = App.Services.CreateScope();
        var vm = scope.ServiceProvider.GetRequiredService<CreateInvoiceViewModel>();
        var window = scope.ServiceProvider.GetRequiredService<CreateInvoiceWindow>();
        
        // When window closes, dispose scope -> all Transient inside dispose
        window.Closed += async (s, e) =>
        {
            scope.Dispose();
            await LoadAsync();
        };
        
        vm.InvoiceCreated += async (s, e) => await LoadAsync();
        window.Show();
    }

    private async Task DeleteAsync()
    {
        if (SelectedInvoice == null) return;
        var result = MessageBox.Show($"Delete invoice for {SelectedInvoice.CustomerName}?", "Confirm", MessageBoxButton.YesNo);
        if (result != MessageBoxResult.Yes) return;
        
        await _deleteUseCase.ExecuteAsync(SelectedInvoice.Id);
        await LoadAsync();
    }

    private async Task ExportAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = $"invoices_{DateTime.Now:yyyyMMdd}.csv" };
        if (dialog.ShowDialog() == true)
        {
            // Get domain entities for export - need repo directly? Use GetAll then map back via repo search
            // For simplicity, re-fetch domain via factory pattern in VM is anti-pattern, so we export DTOs via FileService extension
            // Here we demonstrate abstraction: we need domain list, so we use repo via use case that returns DTO and export DTOs manually
            var lines = new List<string> { "Id,Customer,Amount,Tax,Total,CreatedAt" };
            lines.AddRange(Invoices.Select(i => $"{i.Id},{i.CustomerName},{i.Amount},{i.Tax},{i.Total},{i.CreatedAt}"));
            await File.WriteAllLinesAsync(dialog.FileName, lines);
            Status = $"Exported to {dialog.FileName}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
