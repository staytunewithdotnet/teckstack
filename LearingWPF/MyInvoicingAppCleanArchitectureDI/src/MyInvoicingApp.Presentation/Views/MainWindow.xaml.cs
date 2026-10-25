using System.Windows;
using MyInvoicingApp.Presentation.ViewModels;

namespace MyInvoicingApp.Presentation.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
