using System.Windows;
using MyInvoicingApp.Presentation.ViewModels;

namespace MyInvoicingApp.Presentation.Views;

public partial class CreateInvoiceWindow : Window
{
    public CreateInvoiceWindow(CreateInvoiceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (s, e) => Close();
        Owner = Application.Current.MainWindow;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
