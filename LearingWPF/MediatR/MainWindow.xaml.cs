using System.Windows;
using WpfMediatRLearningApp.ViewModels;

namespace WpfMediatRLearningApp
{
    /// <summary>
    /// Code-behind simply accepts the ViewModel via Dependency Injection.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            // Set DataContext to the injected ViewModel
            DataContext = viewModel;
        }
    }
}
