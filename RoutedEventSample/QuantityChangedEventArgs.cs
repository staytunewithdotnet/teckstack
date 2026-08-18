using System.Windows;

namespace RoutedEventSample
{
    public static class QuantityChangedEvent
    {
        // Register an attached event that any UI element can handle
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
            "ValueChanged",
            RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<int>),
            typeof(UIElement) // Added ownerType parameter
        );

        // Add the event to an element
        public static void AddValueChangedHandler(DependencyObject element, RoutedPropertyChangedEventHandler<int> handler)
        {
            if (element is UIElement uiElement)
            {
                uiElement.AddHandler(ValueChangedEvent, handler);
            }
        }

        // Remove the event from an element
        public static void RemoveValueChangedHandler(DependencyObject element, RoutedPropertyChangedEventHandler<int> handler)
        {
            if (element is UIElement uiElement)
            {
                uiElement.RemoveHandler(ValueChangedEvent, handler);
            }
        }
    }
}