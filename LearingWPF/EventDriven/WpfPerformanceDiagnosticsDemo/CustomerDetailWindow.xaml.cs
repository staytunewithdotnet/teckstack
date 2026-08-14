using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WpfPerformanceDiagnosticsDemo
{
    public partial class CustomerDetailWindow : Window
    {
        private DispatcherTimer _timer;
        private int _ticks = 0;
        private bool _leakEvents;
        private bool _leakTimer;
        private bool _leakBitmap;

        // Static list to simulate bitmap caching/leaks
        public static List<BitmapSource> LeakedBitmaps = new List<BitmapSource>();

        public CustomerDetailWindow(Customer customer, bool leakMemory, bool leakEvents, bool leakTimer, bool leakBitmap)
        {
            InitializeComponent();

            NameText.Text = customer.Name;
            EmailText.Text = customer.Email;
            StatusText.Text = customer.Company;

            _leakEvents = leakEvents;
            _leakTimer = leakTimer;
            _leakBitmap = leakBitmap;

            // 1. Simulate Memory Leak: Add this window object to a static list so GC cannot reclaim it
            if (leakMemory)
            {
                MemoryLeakRegistry.LeakedObjects.Add(this);
            }

            // 2. Simulate Event Handler Leak
            if (_leakEvents)
            {
                // Subscribe but NEVER unsubscribe
                GlobalEventPublisher.DataUpdated += GlobalEventPublisher_DataUpdated;
                EventsStatusText.Text = "Subscribed to static publisher (Leak ON)";
                EventsStatusText.Foreground = Brushes.Red;
            }
            else
            {
                // Subscribe and unsubscribe in closed event (proper way)
                GlobalEventPublisher.DataUpdated += GlobalEventPublisher_DataUpdated;
                EventsStatusText.Text = "Subscribed to static publisher (Leak OFF)";
                EventsStatusText.Foreground = Brushes.Green;
            }

            // 3. Simulate Timer Leak
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            TimerTicksText.Text = "Timer Running...";

            // 4. Simulate Bitmap Leak: Generate/load a large image (e.g. 4MB)
            if (_leakBitmap)
            {
                // Create a large RenderTargetBitmap (1000x1000 pixels) to consume memory quickly
                RenderTargetBitmap largeBmp = new RenderTargetBitmap(1000, 1000, 96, 96, PixelFormats.Pbgra32);
                DrawingVisual dv = new DrawingVisual();
                using (DrawingContext dc = dv.RenderOpen())
                {
                    dc.DrawRectangle(Brushes.Blue, null, new Rect(0, 0, 1000, 1000));
                    dc.DrawText(
                        new FormattedText("Large Image Leak " + Guid.NewGuid().ToString().Substring(0, 8),
                            System.Globalization.CultureInfo.InvariantCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Segoe UI"),
                            32,
                            Brushes.White,
                            96),
                        new Point(50, 50));
                }
                largeBmp.Render(dv);
                CustomerPhoto.Source = largeBmp;

                // Cache it statically to leak it
                LeakedBitmaps.Add(largeBmp);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _ticks++;
            TimerTicksText.Text = $"Ticks: {_ticks} (Timer active)";
        }

        private void GlobalEventPublisher_DataUpdated(object sender, EventArgs e)
        {
            // Update UI on event triggered
            StatusText.Text = "Data Updated at " + DateTime.Now.ToLongTimeString();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // If leakEvents is OFF, unsubscribe properly
            if (!_leakEvents)
            {
                GlobalEventPublisher.DataUpdated -= GlobalEventPublisher_DataUpdated;
            }

            // If leakTimer is OFF, stop the timer properly
            if (!_leakTimer)
            {
                _timer.Stop();
                _timer = null;
            }
            // If leakTimer is ON, we leave _timer running, keeping a reference chain alive!
        }
    }
}
