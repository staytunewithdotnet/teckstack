using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfPerformanceDiagnosticsDemo
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _telemetryTimer;
        private List<Customer> _allCustomers = new List<Customer>();
        
        // Simulation states
        private bool _dispatcherOverloadActive = false;
        private bool _gcPressureActive = false;
        private bool _bindingErrorsActive = false;
        private bool _loggingOverheadActive = false;

        // Cancellation tokens for simulation loops
        private CancellationTokenSource _loggingCts;
        private CancellationTokenSource _dispatcherCts;

        // Custom trace listener for binding errors
        private BindingErrorTraceListener _bindingTraceListener;

        public MainWindow()
        {
            InitializeComponent();
            
            // Set up telemetry timer (ticks every 500ms)
            _telemetryTimer = new DispatcherTimer();
            _telemetryTimer.Interval = TimeSpan.FromMilliseconds(500);
            _telemetryTimer.Tick += TelemetryTimer_Tick;
            _telemetryTimer.Start();

            // Set up Binding Error tracking
            _bindingTraceListener = new BindingErrorTraceListener(LogMessage);
            PresentationTraceSources.DataBindingSource.Listeners.Add(_bindingTraceListener);
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;

            // Generate initial mock data
            GenerateMockCustomers(100);
            CustomerListBox.ItemsSource = _allCustomers;

            LogMessage("Sandbox started. Telemetry active. Ready for diagnostics.");
        }

        private void TelemetryTimer_Tick(object sender, EventArgs e)
        {
            // 1. Measure Heap Memory
            long bytes = GC.GetTotalMemory(false);
            double mb = bytes / (1024.0 * 1024.0);
            MemoryUsageText.Text = $"{mb:F2} MB";
            MemoryProgressBar.Value = Math.Min(mb, MemoryProgressBar.Maximum);

            // 2. Measure GC collection counts
            Gen0CountText.Text = GC.CollectionCount(0).ToString();
            Gen1CountText.Text = GC.CollectionCount(1).ToString();
            Gen2CountText.Text = GC.CollectionCount(2).ToString();

            // 3. Measure Dispatcher Lag
            var sw = Stopwatch.StartNew();
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                sw.Stop();
                long lag = sw.ElapsedMilliseconds;
                DispatcherLagText.Text = $"{lag} ms";
                if (lag > 100)
                {
                    DispatcherLagText.Foreground = Brushes.Red;
                }
                else if (lag > 30)
                {
                    DispatcherLagText.Foreground = Brushes.Orange;
                }
                else
                {
                    DispatcherLagText.Foreground = Brushes.LightGreen;
                }
            }));

            // 4. Update subscriber counts & window counts
            SubscriberCountText.Text = $"{GlobalEventPublisher.GetSubscriberCount()} subscribers";
            OpenWindowsCountText.Text = $"Leaked window/object references in registry: {MemoryLeakRegistry.LeakedObjects.Count}";

            // If GC pressure simulation is active, run a fast allocation batch on the UI thread
            if (_gcPressureActive)
            {
                for (int i = 0; i < 50000; i++)
                {
                    // Allocate small short-lived objects to trigger GC
                    var dummy = new byte[128];
                    var dummyStr = new string('x', 10);
                }
            }
        }

        private void GenerateMockCustomers(int count)
        {
            _allCustomers.Clear();
            string[] firstNames = { "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda", "William", "Elizabeth" };
            string[] lastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
            string[] domains = { "corp.com", "global.net", "tech.org", "enterprise.co", "solutions.io" };

            var rand = new Random();
            for (int i = 1; i <= count; i++)
            {
                string first = firstNames[rand.Next(firstNames.Length)];
                string last = lastNames[rand.Next(lastNames.Length)];
                _allCustomers.Add(new Customer
                {
                    Id = i,
                    Name = $"{first} {last}",
                    Email = $"{first.ToLower()}.{last.ToLower()}@{domains[rand.Next(domains.Length)]}",
                    Phone = $"+1 (555) {rand.Next(100, 999)}-{rand.Next(1000, 9999)}",
                    Address = $"{rand.Next(100, 9999)} Maple St, Springfield",
                    Company = $"Enterprise Client {i}"
                });
            }
        }

        private void LogMessage(string message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ConsoleLogBox.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
                if (ConsoleLogBox.Text.Length > 20000)
                {
                    ConsoleLogBox.Text = ConsoleLogBox.Text.Substring(10000);
                }
                ConsoleLogBox.ScrollToEnd();
            }));
        }

        // Open a Single Customer Window
        private void OpenCustomerDetail_Click(object sender, RoutedEventArgs e)
        {
            var randCustomer = _allCustomers[new Random().Next(_allCustomers.Count)];
            var win = new CustomerDetailWindow(
                randCustomer,
                MemoryLeakCheckbox.IsChecked == true,
                EventLeakCheckbox.IsChecked == true,
                TimerLeakCheckbox.IsChecked == true,
                BitmapLeakCheckbox.IsChecked == true
            );
            win.Owner = this;
            win.Show();
            LogMessage($"Opened Detail View for: {randCustomer.Name}");
        }

        // Simulate 50 open/closes
        private void SimulateManyOpens_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Simulating 50 window creations/closes...");
            var rand = new Random();
            for (int i = 0; i < 50; i++)
            {
                var randCustomer = _allCustomers[rand.Next(_allCustomers.Count)];
                var win = new CustomerDetailWindow(
                    randCustomer,
                    MemoryLeakCheckbox.IsChecked == true,
                    EventLeakCheckbox.IsChecked == true,
                    TimerLeakCheckbox.IsChecked == true,
                    BitmapLeakCheckbox.IsChecked == true
                );
                win.Show();
                win.Close(); // Immediately close to test if references are leaked
            }
            LogMessage("Simulation complete. Check telemetry for heap size & subscribers.");
        }

        // 3. Dispatcher Queue Overload Toggle
        private void ToggleDispatcherOverload_Click(object sender, RoutedEventArgs e)
        {
            if (_dispatcherOverloadActive)
            {
                _dispatcherOverloadActive = false;
                _dispatcherCts?.Cancel();
                DispatcherStatusText.Text = "Dispatcher Status: Normal";
                DispatcherStatusText.Foreground = Brushes.Green;
                DispatcherOverloadBtn.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246));
                LogMessage("Dispatcher Overload Simulator stopped.");
            }
            else
            {
                _dispatcherOverloadActive = true;
                _dispatcherCts = new CancellationTokenSource();
                DispatcherStatusText.Text = "Dispatcher Status: OVERLOADED";
                DispatcherStatusText.Foreground = Brushes.Red;
                DispatcherOverloadBtn.Background = Brushes.Red;
                LogMessage("Dispatcher Overload Simulator started. Flooding UI thread...");
                
                var token = _dispatcherCts.Token;
                Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        // Flood the Dispatcher queue with empty operations
                        for (int i = 0; i < 5000; i++)
                        {
                            _ = Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                            {
                                // Simulate minor UI update work
                                double result = Math.Sin(10.0) * Math.Cos(20.0);
                            }));
                        }
                        await Task.Delay(10, token);
                    }
                }, token);
            }
        }

        // 5. Large Collection Load
        private void LoadCustomers100_Click(object sender, RoutedEventArgs e)
        {
            GenerateMockCustomers(100);
            CustomerListBox.ItemsSource = null;
            CustomerListBox.ItemsSource = _allCustomers;
            LogMessage("Loaded 100 customers.");
        }

        private void LoadCustomers100k_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Generating 100,000 customers...");
            var sw = Stopwatch.StartNew();
            GenerateMockCustomers(100000);
            sw.Stop();
            LogMessage($"Generated 100,000 customer objects in {sw.ElapsedMilliseconds} ms. Binding to list...");

            sw.Restart();
            CustomerListBox.ItemsSource = null;
            CustomerListBox.ItemsSource = _allCustomers;
            sw.Stop();
            LogMessage($"Binding source set in {sw.ElapsedMilliseconds} ms. UI might freeze if Virtualization is disabled.");
        }

        // 6. GC Pressure Toggle
        private void ToggleGcPressure_Click(object sender, RoutedEventArgs e)
        {
            _gcPressureActive = !_gcPressureActive;
            if (_gcPressureActive)
            {
                GcPressureStatusText.Text = "GC Pressure: ACTIVE (Creating garbage on UI render)";
                GcPressureStatusText.Foreground = Brushes.Red;
                GcPressureBtn.Background = Brushes.Red;
                LogMessage("GC Pressure simulation active. Check Gen 0 collections count.");
            }
            else
            {
                GcPressureStatusText.Text = "GC Pressure: Idle";
                GcPressureStatusText.Foreground = Brushes.Green;
                GcPressureBtn.Background = new SolidColorBrush(Color.FromRgb(236, 72, 153));
                LogMessage("GC Pressure simulation stopped.");
            }
        }

        // 8. Binding Errors Toggle
        private void ToggleBindingErrors_Click(object sender, RoutedEventArgs e)
        {
            _bindingErrorsActive = !_bindingErrorsActive;
            if (_bindingErrorsActive)
            {
                BindingErrorStatusText.Text = "Binding Errors: ACTIVE";
                BindingErrorStatusText.Foreground = Brushes.Red;
                BindingErrorBtn.Background = Brushes.Red;
                LogMessage("Binding Errors active. Swapped to faulty DataTemplate. Every UI render or scroll of list will output binding error traces.");

                CustomerListBox.ItemTemplate = (DataTemplate)FindResource("BindingErrorTemplate");
                CustomerListBox.Items.Refresh();
            }
            else
            {
                BindingErrorStatusText.Text = "Binding Errors: Inactive";
                BindingErrorStatusText.Foreground = Brushes.Green;
                BindingErrorBtn.Background = new SolidColorBrush(Color.FromRgb(234, 179, 8));
                LogMessage("Binding Errors inactive. Swapped back to Normal DataTemplate.");

                CustomerListBox.ItemTemplate = (DataTemplate)FindResource("NormalTemplate");
                CustomerListBox.Items.Refresh();
            }
        }

        // 9. Logging Overhead Toggle
        private void ToggleLoggingOverhead_Click(object sender, RoutedEventArgs e)
        {
            if (_loggingOverheadActive)
            {
                _loggingOverheadActive = false;
                _loggingCts?.Cancel();
                LoggingStatusText.Text = "Logging: Normal";
                LoggingStatusText.Foreground = Brushes.Green;
                LoggingOverheadBtn.Background = new SolidColorBrush(Color.FromRgb(249, 115, 22));
                LogMessage("Logging Simulator stopped.");
            }
            else
            {
                _loggingOverheadActive = true;
                _loggingCts = new CancellationTokenSource();
                LoggingStatusText.Text = "Logging: HEAVY WRITES ACTIVE";
                LoggingStatusText.Foreground = Brushes.Red;
                LoggingOverheadBtn.Background = Brushes.Red;
                LogMessage("Logging Simulator started. Writing verbose log lines to disk synchronously...");

                var token = _loggingCts.Token;
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "excessive_diagnostic.log");

                Task.Run(() =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            // Synchronous excessive logging simulation
                            for (int i = 0; i < 500; i++)
                            {
                                File.AppendAllText(logPath, $"[VERBOSE] [{DateTime.Now:O}] Mouse position simulated: X={Random.Shared.Next(1920)}, Y={Random.Shared.Next(1080)} - App Status Normal.\n");
                            }
                        }
                        catch (Exception)
                        {
                            // ignore write issues
                        }
                        Thread.Sleep(5);
                    }
                }, token);
            }
        }

        // Reset memory leaks, clear registries and run GC
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Cleaning all simulated resources...");
            MemoryLeakRegistry.Clear();
            GlobalEventPublisher.ClearSubscribers();
            CustomerDetailWindow.LeakedBitmaps.Clear();
            
            // Forces clean garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            LogMessage("Clean up completed. GC run completed.");
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            ConsoleLogBox.Clear();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // Clean up timers & threads
            _telemetryTimer.Stop();
            _loggingCts?.Cancel();
            _dispatcherCts?.Cancel();

            PresentationTraceSources.DataBindingSource.Listeners.Remove(_bindingTraceListener);
        }
    }

    // Custom Trace Listener to channel WPF binding errors to the diagnostic log box
    public class BindingErrorTraceListener : TraceListener
    {
        private Action<string> _logAction;

        public BindingErrorTraceListener(Action<string> logAction)
        {
            _logAction = logAction;
        }

        public override void Write(string message)
        {
            // Binding errors come in parts
        }

        public override void WriteLine(string message)
        {
            if (message.Contains("System.Windows.Data Error"))
            {
                _logAction?.Invoke($"[BINDING ERROR] {message}");
            }
        }
    }
}