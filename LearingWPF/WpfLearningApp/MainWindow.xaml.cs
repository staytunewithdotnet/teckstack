using System.Text;
using System.Windows;
using System.Windows.Controls; // Required for Button, Page, etc.
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfLearningApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InitializeNavigationMenu();
    }

    private void InitializeNavigationMenu()
    {
        // Clear existing menu items
        MenuPanel.Children.Clear();

        // Add menu items - initially empty since no pages exist yet
        AddMenuItem("Home", typeof(HomePage));

        // You can add more menu items as you create new pages:
        // AddMenuItem("Button Examples", typeof(ButtonPage));
        // AddMenuItem("Data Binding", typeof(DataBindingPage));
        // AddMenuItem("Styles & Templates", typeof(StylesPage));
        // AddMenuItem("Animations", typeof(AnimationPage));
        // AddMenuItem("Custom Controls", typeof(CustomControlPage));
    }

    private void AddMenuItem(string displayName, Type pageType)
    {
        var button = new Button
        {
            Content = displayName,
            Height = 35,
            Margin = new Thickness(0, 2, 0, 2),
            Background = new SolidColorBrush(Color.FromRgb(52, 73, 94)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };

        // Store the page type as Tag for later use
        button.Tag = pageType;

        button.Click += MenuItem_Click;

        MenuPanel.Children.Add(button);
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var pageType = button?.Tag as Type;

        if (pageType != null)
        {
            try
            {
                // Create instance of the page and navigate to it
                var pageInstance = Activator.CreateInstance(pageType) as Page;
                if (pageInstance != null)
                {
                    MainFrame.Navigate(pageInstance);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading page: {ex.Message}");
            }
        }
    }
}