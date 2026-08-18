using System.Windows;
using System.Windows.Controls;

namespace RoutedEventSample
{
    /// <summary>
    /// Interaction logic for QuantityBox.xaml
    /// This control demonstrates a custom routed event in WPF.
    /// </summary>
    public partial class QuantityBox : UserControl
    {
        // 1. REGISTERING THE ROUTED EVENT
        public static readonly RoutedEvent ValueChangedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(ValueChanged),
                RoutingStrategy.Bubble,  // Fixed: Using Bubble instead of Bubbling
                typeof(RoutedPropertyChangedEventHandler<int>),  // Fixed: Properly typed
                typeof(QuantityBox));

        private int _quantity = 1;

        public QuantityBox()
        {
            InitializeComponent();
            txtQuantity.Text = _quantity.ToString();
        }

        // 2. CLR EVENT WRAPPER
        public event RoutedPropertyChangedEventHandler<int> ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        // 3. PROPERTY WITH CHANGE NOTIFICATION
        public int Quantity
        {
            get { return _quantity; }
            set
            {
                int oldValue = _quantity;
                _quantity = value;
                txtQuantity.Text = _quantity.ToString();

                // 4. RAISING THE CUSTOM ROUTED EVENT
                OnValueChanged(oldValue, _quantity);
            }
        }

        // 5. METHOD TO RAISE THE EVENT
        protected virtual void OnValueChanged(int oldValue, int newValue)
        {
            var args = new RoutedPropertyChangedEventArgs<int>(
                oldValue, 
                newValue, 
                ValueChangedEvent);
            RaiseEvent(args);
        }

        // 6. HANDLING UI INTERACTIONS
        private void BtnIncrease_Click(object sender, RoutedEventArgs e)
        {
            Quantity++;
        }

        private void BtnDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (Quantity > 1)
            {
                Quantity--;
            }
        }

        private void TxtQuantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(txtQuantity.Text, out int newQuantity) && newQuantity > 0)
            {
                Quantity = newQuantity;
            }
        }
    }

    // 7. DEFINING THE CUSTOM EVENT ARGS AND DELEGATE
    public delegate void RoutedPropertyChangedEventHandler<T>(object sender, RoutedPropertyChangedEventArgs<T> e);

    public class RoutedPropertyChangedEventArgs<T> : RoutedEventArgs
    {
        public T OldValue { get; }
        public T NewValue { get; }

        public RoutedPropertyChangedEventArgs(T oldValue, T newValue, RoutedEvent routedEvent) : base(routedEvent)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}