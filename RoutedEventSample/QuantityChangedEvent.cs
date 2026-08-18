using System.Windows;

namespace RoutedEventSample
{
    public static class QuantityChangedEventHelper
    {
        // Helper methods to add/remove handlers for the ValueChanged event from QuantityBox
        // This allows any UIElement to listen to the event
        
        public static void AddValueChangedHandler(DependencyObject element, RoutedPropertyChangedEventHandler<int> handler)
        {
            if (element is UIElement uiElement)
            {
                uiElement.AddHandler(QuantityBox.ValueChangedEvent, handler);
            }
        }

        public static void RemoveValueChangedHandler(DependencyObject element, RoutedPropertyChangedEventHandler<int> handler)
        {
            if (element is UIElement uiElement)
            {
                uiElement.RemoveHandler(QuantityBox.ValueChangedEvent, handler);
            }
        }
    }
}