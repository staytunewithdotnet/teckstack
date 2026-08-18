using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // For VisualTreeHelper

namespace RoutedEventSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// This window demonstrates how parent containers can listen to routed events
    /// from child controls without direct subscriptions.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Register the event handler for the routed event from QuantityBox using the helper
            QuantityChangedEventHelper.AddValueChangedHandler(MainPanel, AnyQuantityChanged);
        }

        // Handler for the ValueChanged routed event from any child QuantityBox
        private void AnyQuantityChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
        {
            // Demonstrates the difference between sender and source
            AppendToEventLog($"Routed Event Received!\n" +
                            $"  Sender (handler target): {sender.GetType().Name}\n" +
                            $"  Source (original raiser): {(e.OriginalSource as FrameworkElement)?.Name ?? "Unknown"}\n" +
                            $"  Old Value: {e.OldValue}, New Value: {e.NewValue}\n" +
                            $"  Timestamp: {DateTime.Now:HH:mm:ss.fff}\n");
        }

        // Helper method to append messages to the event log
        private void AppendToEventLog(string message)
        {
            eventLog.Text += message + new string('-', 50) + "\n";
            
            // Auto-scroll to the bottom
            var scrollViewer = FindVisualChild<ScrollViewer>(eventLog);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToBottom();
            }
        }

        // Helper method to find a visual child of a given type in the visual tree
        private T? FindVisualChild<T>(DependencyObject? parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T childOfT)
                    return childOfT;

                var childOfTFromSubtree = FindVisualChild<T>(child);
                if (childOfTFromSubtree != null)
                    return childOfTFromSubtree;
            }
            return null;
        }
    }
}