using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows; // Added for Application.Current
using Prism.Events;
using WpfPrismEventAggregatorDemo.Events;

namespace WpfPrismEventAggregatorDemo.Services
{
    /// <summary>
    /// ThreadOptionDemoService - Demonstrates different threading options in Prism EventAggregator.
    /// 
    /// PURPOSE:
    /// This service shows how ThreadOption affects event handler execution.
    /// Run the application and observe the log output to see the differences.
    /// 
    /// THREAD OPTIONS DEMONSTRATED:
    /// 1. PublisherThread (default) - Handler runs on publisher's thread
    /// 2. UIThread - Handler runs on WPF UI thread
    /// 3. BackgroundThread - Handler runs on thread pool thread
    /// 
    /// HOW TO TEST:
    /// 1. Open MainWindow.xaml
    /// 2. Look for "Thread Option Demo" section
    /// 3. Click buttons to publish events with different thread options
    /// 4. Observe log messages showing thread IDs and timing
    /// 
    /// KEY LEARNING POINTS:
    /// - PublisherThread: Fast, synchronous, blocks publisher
    /// - UIThread: Safe for UI updates, can cause deadlocks if misused
    /// - BackgroundThread: Non-blocking, good for async work, can't update UI directly
    /// </summary>
    public class ThreadOptionDemoService
    {
        private readonly IEventAggregator _eventAggregator;

        public ThreadOptionDemoService(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;

            Log("ThreadOptionDemoService initialized", "INFO");
            Log("===========================================================", "INFO");
            Log("THREAD OPTION DEMONSTRATION SERVICE", "INFO");
            Log("This demonstrates how different ThreadOptions affect execution", "INFO");
            Log("===========================================================", "INFO");

            SetupSubscriptions();
        }

        /// <summary>
        /// Sets up subscriptions with different thread options to demonstrate behavior.
        /// </summary>
        private void SetupSubscriptions()
        {
            // ========================================================================
            // EXAMPLE 1: PublisherThread (DEFAULT)
            // ========================================================================
            // Behavior: Executes immediately on the thread that published the event
            // Use when: Handler is fast and doesn't need UI access
            // Warning: Can block the publisher if handler is slow
            // ========================================================================
            _eventAggregator.GetEvent<PublisherThreadDemoEvent>().Subscribe(
                payload =>
                {
                    var threadId = Thread.CurrentThread.ManagedThreadId;
                    var isBackground = Thread.CurrentThread.IsBackground;
                    
                    Log($"[PublisherThread] Handler executed on Thread #{threadId} (Background: {isBackground})", "INFO");
                    Log($"[PublisherThread] Payload received: {payload.Message}", "INFO");
                    
                    // Simulate some work (this BLOCKS the publisher!)
                    Thread.Sleep(2000);
                    
                    Log($"[PublisherThread] Work completed (blocked publisher for 500ms)", "SUCCESS");
                }
                // Note: No ThreadOption specified = defaults to PublisherThread
            );

            // ========================================================================
            // EXAMPLE 2: UIThread
            // ========================================================================
            // Behavior: Executes on WPF UI thread via Dispatcher
            // Use when: Need to update UI elements (ObservableCollection, controls)
            // Warning: Can deadlock if publisher is on UI thread and waits for result
            // ========================================================================
            _eventAggregator.GetEvent<UIThreadDemoEvent>().Subscribe(
                payload =>
                {
                    var threadId = Thread.CurrentThread.ManagedThreadId;
                    var isUI = Thread.CurrentThread.ManagedThreadId == GetCurrentUIThreadId();
                    
                    Log($"[UIThread] Handler executed on Thread #{threadId} (Is UI Thread: {isUI})", "INFO");
                    Log($"[UIThread] Payload received: {payload.Message}", "INFO");
                    
                    // Safe to update UI-bound collections here without Dispatcher.Invoke
                    // In OrderViewModel, we use this for updating Logs ObservableCollection
                    
                    // Simulate UI work
                    Thread.Sleep(3000);
                    
                    Log($"[UIThread] UI work completed", "SUCCESS");
                },
                ThreadOption.UIThread // Explicitly specify UI thread
            );

            // ========================================================================
            // EXAMPLE 3: BackgroundThread
            // ========================================================================
            // Behavior: Executes on thread pool thread (background)
            // Use when: Long-running operations (API calls, file I/O, calculations)
            // Warning: Cannot directly update UI - must marshal back via Dispatcher
            // ========================================================================
            _eventAggregator.GetEvent<BackgroundThreadDemoEvent>().Subscribe(
                payload =>
                {
                    var threadId = Thread.CurrentThread.ManagedThreadId;
                    var isBackground = Thread.CurrentThread.IsBackground;
                    
                    Log($"[BackgroundThread] Handler executed on Thread #{threadId} (Background: {isBackground})", "INFO");
                    Log($"[BackgroundThread] Payload received: {payload.Message}", "INFO");
                    
                    // Simulate long-running operation (doesn't block UI!)
                    Thread.Sleep(5000);
                    
                    Log($"[BackgroundThread] Long operation completed (didn't block UI)", "SUCCESS");
                    
                    // If we needed to update UI, we'd do:
                    // Application.Current.Dispatcher.Invoke(() => 
                    // {
                    //     // Update UI here
                    // });
                },
                ThreadOption.BackgroundThread // Explicitly specify background thread
            );

            // ========================================================================
            // EXAMPLE 4: Async with BackgroundThread (PRODUCTION PATTERN)
            // ========================================================================
            // This shows the recommended pattern for async operations
            // ========================================================================
            _eventAggregator.GetEvent<AsyncDemoEvent>().Subscribe(
                async payload =>
                {
                    var threadIdBefore = Thread.CurrentThread.ManagedThreadId;
                    Log($"[AsyncDemo] Starting on Thread #{threadIdBefore}", "INFO");
                    
                    // Simulate async operation (HTTP call, database query, etc.)
                    await Task.Delay(800);
                    
                    var threadIdAfter = Thread.CurrentThread.ManagedThreadId;
                    Log($"[AsyncDemo] Completed on Thread #{threadIdAfter} (may be different!)", "SUCCESS");
                    Log($"[AsyncDemo] Payload: {payload.Message}", "INFO");
                    
                    // Note: Thread ID may change after await due to async state machine
                },
                ThreadOption.BackgroundThread // Important for async operations!
            );

            // ========================================================================
            // EXAMPLE 5: Multiple Subscribers - Execution Order
            // ========================================================================
            // Demonstrates that multiple subscribers execute sequentially
            // ========================================================================
            _eventAggregator.GetEvent<MultipleSubscribersEvent>().Subscribe(
                payload =>
                {
                    Log($"[Subscriber 1] Processing on Thread #{Thread.CurrentThread.ManagedThreadId}", "INFO");
                    Thread.Sleep(200);
                    Log($"[Subscriber 1] Completed", "SUCCESS");
                },
                ThreadOption.PublisherThread
            );

            _eventAggregator.GetEvent<MultipleSubscribersEvent>().Subscribe(
                payload =>
                {
                    Log($"[Subscriber 2] Processing on Thread #{Thread.CurrentThread.ManagedThreadId}", "INFO");
                    Thread.Sleep(200);
                    Log($"[Subscriber 2] Completed", "SUCCESS");
                },
                ThreadOption.PublisherThread
            );

            _eventAggregator.GetEvent<MultipleSubscribersEvent>().Subscribe(
                payload =>
                {
                    Log($"[Subscriber 3] Processing on Thread #{Thread.CurrentThread.ManagedThreadId}", "INFO");
                    Thread.Sleep(200);
                    Log($"[Subscriber 3] Completed", "SUCCESS");
                },
                ThreadOption.PublisherThread
            );

            // ========================================================================
            // EXAMPLE 6: PARALLEL EXECUTION with BackgroundThread
            // ========================================================================
            // This demonstrates that multiple subscribers can run IN PARALLEL
            // when using ThreadOption.BackgroundThread
            // 
            // KEY LEARNING:
            // - PublisherThread: Subscribers run sequentially (one after another)
            // - BackgroundThread: Subscribers can run in parallel (simultaneously)
            // - This is important for performance when subscribers are independent
            // ========================================================================
            _eventAggregator.GetEvent<ParallelExecutionEvent>().Subscribe(
                payload =>
                {
                    var threadId = Thread.CurrentThread.ManagedThreadId;
                    Log($"[Parallel Task A] Started on Thread #{threadId}", "INFO");
                    
                    // Simulate independent work (e.g., calling different APIs)
                    Thread.Sleep(2000); // 2 seconds of work
                    
                    Log($"[Parallel Task A] Completed on Thread #{threadId}", "SUCCESS");
                },
                ThreadOption.BackgroundThread // Allows parallel execution
            );

            _eventAggregator.GetEvent<ParallelExecutionEvent>().Subscribe(
                payload =>
                {
                    var threadId = Thread.CurrentThread.ManagedThreadId;
                    Log($"[Parallel Task B] Started on Thread #{threadId}", "INFO");
                    
                    // Simulate independent work
                    Thread.Sleep(2000); // 2 seconds of work
                    
                    Log($"[Parallel Task B] Completed on Thread #{threadId}", "SUCCESS");
                },
                ThreadOption.BackgroundThread // Allows parallel execution
            );

            _eventAggregator.GetEvent<ParallelExecutionEvent>().Subscribe(
                payload =>
                {
                    var threadId = Thread.CurrentThread.ManagedThreadId;
                    Log($"[Parallel Task C] Started on Thread #{threadId}", "INFO");
                    
                    // Simulate independent work
                    Thread.Sleep(2000); // 2 seconds of work
                    
                    Log($"[Parallel Task C] Completed on Thread #{threadId}", "SUCCESS");
                },
                ThreadOption.BackgroundThread // Allows parallel execution
            );

            // ========================================================================
            // EXAMPLE 7: SEQUENTIAL EXECUTION with PublisherThread (Comparison)
            // ========================================================================
            // Same scenario as above, but with PublisherThread to show the difference
            // Total time will be ~6 seconds (3 tasks × 2 seconds each)
            // ========================================================================
            _eventAggregator.GetEvent<SequentialExecutionEvent>().Subscribe(
                payload =>
                {
                    var threadId = Thread.CurrentThread.ManagedThreadId;
                    Log($"[Sequential Task 1] Started on Thread #{threadId}", "INFO");
                    Thread.Sleep(2000);
                    Log($"[Sequential Task 1] Completed", "SUCCESS");
                },
                ThreadOption.PublisherThread // Forces sequential execution
            );

            _eventAggregator.GetEvent<SequentialExecutionEvent>().Subscribe(
                payload =>
                {
                    var threadId = Thread.CurrentThread.ManagedThreadId;
                    Log($"[Sequential Task 2] Started on Thread #{threadId}", "INFO");
                    Thread.Sleep(2000);
                    Log($"[Sequential Task 2] Completed", "SUCCESS");
                },
                ThreadOption.PublisherThread // Forces sequential execution
            );

            _eventAggregator.GetEvent<SequentialExecutionEvent>().Subscribe(
                payload =>
                {
                    var threadId = Thread.CurrentThread.ManagedThreadId;
                    Log($"[Sequential Task 3] Started on Thread #{threadId}", "INFO");
                    Thread.Sleep(2000);
                    Log($"[Sequential Task 3] Completed", "SUCCESS");
                },
                ThreadOption.PublisherThread // Forces sequential execution
            );
        }

        /// <summary>
        /// Gets the current UI thread ID for comparison.
        /// </summary>
        private int GetCurrentUIThreadId()
        {
            // In WPF, UI thread is the thread where Application.Current was created
            return Application.Current?.Dispatcher?.Thread?.ManagedThreadId ?? -1;
        }

        /// <summary>
        /// Helper method to publish demo events from UI.
        /// Call these methods from ViewModel or code-behind for testing.
        /// </summary>
        public void TestPublisherThread()
        {
            Log("[TEST] Publishing PublisherThreadDemoEvent...", "WARNING");
            var stopwatch = Stopwatch.StartNew();
            
            _eventAggregator.GetEvent<PublisherThreadDemoEvent>().Publish(new ThreadDemoPayload
            {
                Message = "Test message on publisher thread",
                Timestamp = DateTime.Now
            });
            
            stopwatch.Stop();
            Log($"[TEST] Publisher thread test completed in {stopwatch.ElapsedMilliseconds}ms (includes handler time)", "SUCCESS");
        }

        public void TestUIThread()
        {
            Log("[TEST] Publishing UIThreadDemoEvent...", "WARNING");
            
            _eventAggregator.GetEvent<UIThreadDemoEvent>().Publish(new ThreadDemoPayload
            {
                Message = "Test message on UI thread",
                Timestamp = DateTime.Now
            });
            
            Log("[TEST] UIThread test published (handler executes asynchronously on UI thread)", "SUCCESS");
        }

        public void TestBackgroundThread()
        {
            Log("[TEST] Publishing BackgroundThreadDemoEvent...", "WARNING");
            var stopwatch = Stopwatch.StartNew();
            
            _eventAggregator.GetEvent<BackgroundThreadDemoEvent>().Publish(new ThreadDemoPayload
            {
                Message = "Test message on background thread",
                Timestamp = DateTime.Now
            });
            
            stopwatch.Stop();
            Log($"[TEST] BackgroundThread test published in {stopwatch.ElapsedMilliseconds}ms (handler runs async)", "SUCCESS");
        }

        public async Task TestAsyncOperation()
        {
            Log("[TEST] Publishing AsyncDemoEvent...", "WARNING");
            
            await Task.Run(() =>
            {
                _eventAggregator.GetEvent<AsyncDemoEvent>().Publish(new ThreadDemoPayload
                {
                    Message = "Test async operation",
                    Timestamp = DateTime.Now
                });
            });
            
            Log("[TEST] Async test published", "SUCCESS");
        }

        public void TestMultipleSubscribers()
        {
            Log("[TEST] Publishing MultipleSubscribersEvent (3 subscribers)...", "WARNING");
            var stopwatch = Stopwatch.StartNew();
            
            _eventAggregator.GetEvent<MultipleSubscribersEvent>().Publish(new ThreadDemoPayload
            {
                Message = "Test with multiple subscribers",
                Timestamp = DateTime.Now
            });
            
            stopwatch.Stop();
            Log($"[TEST] Multiple subscribers test completed in {stopwatch.ElapsedMilliseconds}ms", "SUCCESS");
            Log($"[TEST] Note: All 3 subscribers ran sequentially on same thread", "INFO");
        }

        /// <summary>
        /// Demonstrates PARALLEL execution with BackgroundThread.
        /// Three subscribers run simultaneously on different threads.
        /// Total time: ~2 seconds (not 6 seconds!)
        /// </summary>
        public void TestParallelExecution()
        {
            Log("[TEST] ===== PARALLEL EXECUTION DEMO =====", "WARNING");
            Log("[TEST] Publishing event with 3 BackgroundThread subscribers...", "WARNING");
            Log("[TEST] Each task takes 2 seconds. Watch them run in parallel!", "INFO");
            
            var stopwatch = Stopwatch.StartNew();
            
            _eventAggregator.GetEvent<ParallelExecutionEvent>().Publish(new ThreadDemoPayload
            {
                Message = "Parallel execution test",
                Timestamp = DateTime.Now
            });
            
            stopwatch.Stop();
            Log($"[TEST] Publisher returned in {stopwatch.ElapsedMilliseconds}ms", "SUCCESS");
            Log($"[TEST] Tasks are now running in background...", "INFO");
            Log($"[TEST] Expected completion: ~2 seconds (parallel, not sequential!)", "INFO");
        }

        /// <summary>
        /// Demonstrates SEQUENTIAL execution with PublisherThread.
        /// Three subscribers run one after another on same thread.
        /// Total time: ~6 seconds (3 tasks × 2 seconds)
        /// </summary>
        public void TestSequentialExecution()
        {
            Log("[TEST] ===== SEQUENTIAL EXECUTION DEMO =====", "WARNING");
            Log("[TEST] Publishing event with 3 PublisherThread subscribers...", "WARNING");
            Log("[TEST] Each task takes 2 seconds. Watch them run sequentially!", "INFO");
            
            var stopwatch = Stopwatch.StartNew();
            
            _eventAggregator.GetEvent<SequentialExecutionEvent>().Publish(new ThreadDemoPayload
            {
                Message = "Sequential execution test",
                Timestamp = DateTime.Now
            });
            
            stopwatch.Stop();
            Log($"[TEST] Sequential execution completed in {stopwatch.ElapsedMilliseconds}ms", "SUCCESS");
            Log($"[TEST] Note: Publisher was blocked for entire duration (~6 seconds)", "INFO");
        }

        private void Log(string message, string type)
        {
            _eventAggregator.GetEvent<TransactionLogEvent>().Publish(new TransactionLogPayload
            {
                Message = $"[ThreadDemo] {message}",
                Type = type
            });
        }
    }

    // =============================================================================
    // DEMO EVENT DEFINITIONS
    // =============================================================================

    public class ThreadDemoPayload
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class PublisherThreadDemoEvent : PubSubEvent<ThreadDemoPayload> { }
    public class UIThreadDemoEvent : PubSubEvent<ThreadDemoPayload> { }
    public class BackgroundThreadDemoEvent : PubSubEvent<ThreadDemoPayload> { }
    public class AsyncDemoEvent : PubSubEvent<ThreadDemoPayload> { }
    public class MultipleSubscribersEvent : PubSubEvent<ThreadDemoPayload> { }
    
    // New events for parallel vs sequential demonstration
    public class ParallelExecutionEvent : PubSubEvent<ThreadDemoPayload> { }
    public class SequentialExecutionEvent : PubSubEvent<ThreadDemoPayload> { }
}
