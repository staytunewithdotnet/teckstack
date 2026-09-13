using System.Windows;
using MyInvoicingApp.Presentation.ViewModels;

namespace MyInvoicingApp.Presentation.Views;

public partial class MainWindow : Window
{
    public MainWindow(CreateInvoiceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}