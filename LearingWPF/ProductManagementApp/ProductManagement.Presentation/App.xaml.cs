using System.Windows;

namespace ProductManagement.Presentation
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// In a full Clean Architecture implementation, this would be the Composition Root
    /// where all dependencies are registered in the DI container
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }
    }
}