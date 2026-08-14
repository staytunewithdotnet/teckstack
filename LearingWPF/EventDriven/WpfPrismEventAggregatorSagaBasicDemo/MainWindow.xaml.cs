using System.Windows;
using WpfPrismEventAggregatorDemo.ViewModels;
 
namespace WpfPrismEventAggregatorDemo
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = mainViewModel;
        }
    }
}