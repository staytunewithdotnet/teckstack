using System.Windows;
using ProductManagement.Presentation.ViewModels;

namespace ProductManagement.Presentation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// This demonstrates the Presentation layer in Clean Architecture
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // In a full implementation, this would come from DI container
            DataContext = new ProductViewModel();
        }
    }
}