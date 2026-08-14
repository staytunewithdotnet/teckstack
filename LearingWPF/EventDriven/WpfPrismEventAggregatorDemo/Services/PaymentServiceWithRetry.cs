using System;
using System.Threading.Tasks;
using Prism.Events;
using WpfPrismEventAggregatorDemo.Events;

namespace WpfPrismEventAggregatorDemo.Services
{
    /// <summary>
    /// PaymentServiceWithRetry - Enhanced payment service with retry logic and error recovery.
    /// 
    /// IMPROVEMENTS OVER BASIC PaymentService:
    /// 1. Retry Logic: Automatically retries transient failures (network issues, timeouts)
    /// 2. Exponential Backoff: Waits progressively longer between retries
    /// 3. Circuit Breaker: Stops retrying after too many failures
    /// 4. Async/Await: Non-blocking asynchronous operations
    /// 5. Background Threading: Uses ThreadOption.BackgroundThread for async operations
    /// 
    /// RETRY STRATEGY:
    /// - Transient errors (network timeout, temporary unavailability): Retry up to 3 times
    /// - Permanent errors (insufficient funds, invalid card): No retry, trigger rollback immediately
    /// - Backoff delays: 1s → 2s → 4s (exponential backoff with jitter)
    /// 
    /// WHEN TO USE THIS VS BASIC PaymentService:
    /// - Use this for production systems calling external APIs
    /// - Basic service is fine for demos and simple in-memory operations
    /// </summary>
    public class PaymentServiceWithRetry
    {
        private readonly IEventAggregator _eventAggregator;
        
        // Configuration for retry logic
        private const int MaxRetries = 3;
        private const int InitialDelayMs = 1000; // Start with 1 second delay
        
        // Circuit breaker state
        private static int _consecutiveFailures = 0;
        private const int CircuitBreakerThreshold = 5; // Stop after 5 consecutive failures
        private static DateTime _circuitOpenTime = DateTime.MinValue;
        private const int CircuitResetTimeoutMs = 30000; // Reset after 30 seconds

        /// <summary>
        /// Static flag to simulate different failure scenarios for testing.
        /// </summary>
        public static bool SimulateTransientFailure { get; set; } = false;
        public static bool SimulatePermanentFailure { get; set; } = false;

        /// <summary>
        /// Initializes the PaymentServiceWithRetry with background threading for async operations.
        /// 
        /// KEY DIFFERENCE FROM BASIC SERVICE:
        /// Uses ThreadOption.BackgroundThread to enable true asynchronous processing.
        /// This prevents blocking the UI or publisher thread during payment processing.
        /// </summary>
        /// <param name="eventAggregator">Prism's event aggregator</param>
        public PaymentServiceWithRetry(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;

            // Subscribe with BackgroundThread for async processing
            // This allows us to use async/await without blocking the UI
            _eventAggregator.GetEvent<InventoryReservedEvent>().Subscribe(
                OnInventoryReservedAsync, 
                ThreadOption.BackgroundThread
            );
            
            Log("PaymentServiceWithRetry initialized with retry logic and background threading", "INFO");
        }

        /// <summary>
        /// Async handler for inventory reservation events with retry logic.
        /// 
        /// PROCESS FLOW:
        /// 1. Check circuit breaker (is payment system available?)
        /// 2. Attempt payment with retry logic
        /// 3. On success: Publish PaymentProcessedEvent
        /// 4. On permanent failure: Publish PaymentFailedEvent (triggers rollback)
        /// 
        /// THREADING:
        /// - Runs on background thread (ThreadOption.BackgroundThread)
        /// - Does NOT block UI or publisher
        /// - Safe for long-running operations and API calls
        /// 
        /// ASYNC PATTERN:
        /// - Uses async void because EventAggregator doesn't support Task return
        /// - In production with better frameworks, would use async Task
        /// - Exceptions are caught and handled internally
        /// </summary>
        /// <param name="payload">Inventory reservation payload</param>
        private async void OnInventoryReservedAsync(InventoryReservedPayload payload)
        {
            try
            {
                Log($"Order {payload.OrderId}: Starting async payment processing...", "INFO");
                
                // Check circuit breaker before attempting payment
                if (IsCircuitOpen())
                {
                    Log($"Order {payload.OrderId}: Circuit breaker is OPEN. Payment system unavailable.", "ERROR");
                    PublishFailure(payload, "Payment system temporarily unavailable (circuit breaker open)");
                    return;
                }
                
                // Attempt payment with retry logic
                var result = await ProcessPaymentWithRetryAsync(payload);
                
                if (result.Success)
                {
                    // Reset circuit breaker on success
                    ResetCircuitBreaker();
                    
                    string transactionId = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                    Log($"Order {payload.OrderId}: Payment successful. Txn: {transactionId}", "SUCCESS");
                    
                    _eventAggregator.GetEvent<PaymentProcessedEvent>().Publish(new PaymentProcessedPayload
                    {
                        OrderId = payload.OrderId,
                        TransactionId = transactionId
                    });
                }
                else
                {
                    // Permanent failure - trigger rollback
                    Log($"Order {payload.OrderId}: Payment permanently failed: {result.ErrorMessage}", "ERROR");
                    PublishFailure(payload, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                // Unexpected error - log and trigger rollback
                Log($"Order {payload.OrderId}: Unexpected error: {ex.Message}", "ERROR");
                PublishFailure(payload, $"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Attempts to process payment with exponential backoff retry logic.
        /// 
        /// RETRY STRATEGY:
        /// - Retry only transient errors (network issues, timeouts)
        /// - Don't retry permanent errors (insufficient funds, invalid card)
        /// - Exponential backoff: 1s → 2s → 4s between retries
        /// - Add jitter to prevent thundering herd problem
        /// 
        /// BACKOFF CALCULATION:
        /// Delay = InitialDelayMs * (2 ^ attemptNumber) + random jitter
        /// Attempt 0: ~1 second
        /// Attempt 1: ~2 seconds
        /// Attempt 2: ~4 seconds
        /// Total max wait: ~7 seconds
        /// </summary>
        /// <param name="payload">Payment request payload</param>
        /// <returns>Payment result with success status and error message</returns>
        private async Task<PaymentResult> ProcessPaymentWithRetryAsync(InventoryReservedPayload payload)
        {
            Exception lastException = null;
            
            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    Log($"Order {payload.OrderId}: Payment attempt {attempt + 1}/{MaxRetries + 1}", "INFO");
                    
                    // Simulate async payment gateway call
                    var result = await CallPaymentGatewayAsync(payload, attempt);
                    
                    // Success!
                    return result;
                }
                catch (TransientPaymentException ex)
                {
                    // Transient error - can retry
                    lastException = ex;
                    Log($"Order {payload.OrderId}: Transient error (attempt {attempt + 1}): {ex.Message}", "WARNING");
                    
                    if (attempt < MaxRetries)
                    {
                        // Calculate backoff delay with jitter
                        var delay = CalculateBackoffWithJitter(attempt);
                        Log($"Order {payload.OrderId}: Retrying in {delay}ms...", "WARNING");
                        
                        await Task.Delay(delay);
                    }
                }
                catch (PermanentPaymentException ex)
                {
                    // Permanent error - don't retry
                    Log($"Order {payload.OrderId}: Permanent error (no retry): {ex.Message}", "ERROR");
                    IncrementCircuitBreaker();
                    return new PaymentResult { Success = false, ErrorMessage = ex.Message };
                }
            }
            
            // All retries exhausted
            Log($"Order {payload.OrderId}: All {MaxRetries + 1} attempts failed", "ERROR");
            IncrementCircuitBreaker();
            return new PaymentResult 
            { 
                Success = false, 
                ErrorMessage = $"Payment failed after {MaxRetries + 1} attempts. Last error: {lastException?.Message}" 
            };
        }

        /// <summary>
        /// Simulates calling an external payment gateway API.
        /// 
        /// This method demonstrates different failure scenarios:
        /// - Transient failures: Network timeout, temporary unavailability (retryable)
        /// - Permanent failures: Insufficient funds, invalid card (not retryable)
        /// - Success: Normal operation
        /// 
        /// IN PRODUCTION:
        /// Replace this with actual HTTP call to Stripe/PayPal/etc.
        /// Example:
        /// var response = await _httpClient.PostAsync("https://api.stripe.com/v1/charges", content);
        /// return ParsePaymentResponse(response);
        /// </summary>
        /// <param name="payload">Payment details</param>
        /// <param name="attempt">Current attempt number (for simulating transient failures)</param>
        /// <returns>Payment result</returns>
        private async Task<PaymentResult> CallPaymentGatewayAsync(InventoryReservedPayload payload, int attempt)
        {
            // Simulate network latency (500ms - 1500ms)
            await Task.Delay(new Random().Next(500, 1500));
            
            // Simulate different scenarios based on flags
            if (SimulatePermanentFailure)
            {
                throw new PermanentPaymentException("Insufficient funds");
            }
            
            if (SimulateTransientFailure)
            {
                // Fail first 2 attempts, succeed on 3rd (tests retry logic)
                if (attempt < 2)
                {
                    throw new TransientPaymentException($"Network timeout (attempt {attempt + 1})");
                }
            }
            
            // Simulate random transient failures (10% chance)
            if (new Random().NextDouble() < 0.1)
            {
                throw new TransientPaymentException("Temporary payment gateway error");
            }
            
            // Success case
            return new PaymentResult { Success = true };
        }

        /// <summary>
        /// Calculates exponential backoff delay with random jitter.
        /// 
        /// FORMULA:
        /// baseDelay = InitialDelayMs * (2 ^ attempt)
        /// jitter = random value between 0 and baseDelay
        /// finalDelay = baseDelay + jitter
        /// 
        /// EXAMPLES:
        /// Attempt 0: 1000ms + (0-1000ms jitter) = 1000-2000ms
        /// Attempt 1: 2000ms + (0-2000ms jitter) = 2000-4000ms
        /// Attempt 2: 4000ms + (0-4000ms jitter) = 4000-8000ms
        /// 
        /// WHY JITTER?
        /// Prevents "thundering herd" where many clients retry simultaneously
        /// and overwhelm the server. Randomization spreads out retries.
        /// </summary>
        /// <param name="attempt">Current attempt number (0-based)</param>
        /// <returns>Delay in milliseconds</returns>
        private int CalculateBackoffWithJitter(int attempt)
        {
            var baseDelay = InitialDelayMs * (int)Math.Pow(2, attempt);
            var jitter = new Random().Next(0, baseDelay);
            return baseDelay + jitter;
        }

        /// <summary>
        /// Circuit Breaker Pattern: Tracks consecutive failures and stops retries
        /// when system appears to be down.
        /// 
        /// STATES:
        /// - CLOSED (normal): Allow requests, track failures
        /// - OPEN (tripped): Reject requests immediately, wait for reset timeout
        /// - HALF-OPEN (testing): Allow one test request after timeout
        /// 
        /// BENEFITS:
        /// - Prevents cascading failures
        /// - Gives downstream system time to recover
        /// - Reduces unnecessary load on failing system
        /// - Faster failure response (don't wait for retries)
        /// </summary>
        /// <returns>True if circuit is open (should reject request)</returns>
        private bool IsCircuitOpen()
        {
            if (_consecutiveFailures < CircuitBreakerThreshold)
            {
                return false; // Circuit closed - allow requests
            }
            
            var timeSinceOpen = (DateTime.Now - _circuitOpenTime).TotalMilliseconds;
            if (timeSinceOpen < CircuitResetTimeoutMs)
            {
                return true; // Circuit still open - reject requests
            }
            
            // Timeout elapsed - allow test request (half-open state)
            Log("Circuit breaker: Entering half-open state (allowing test request)", "WARNING");
            _consecutiveFailures = 0; // Reset for test
            return false;
        }

        /// <summary>
        /// Increments consecutive failure counter and opens circuit if threshold reached.
        /// </summary>
        private void IncrementCircuitBreaker()
        {
            _consecutiveFailures++;
            if (_consecutiveFailures >= CircuitBreakerThreshold)
            {
                _circuitOpenTime = DateTime.Now;
                Log($"Circuit breaker OPENED after {_consecutiveFailures} consecutive failures", "ERROR");
            }
        }

        /// <summary>
        /// Resets circuit breaker after successful operation.
        /// </summary>
        private void ResetCircuitBreaker()
        {
            if (_consecutiveFailures > 0)
            {
                Log($"Circuit breaker RESET (previous failures: {_consecutiveFailures})", "SUCCESS");
            }
            _consecutiveFailures = 0;
        }

        /// <summary>
        /// Helper method to publish payment failure events.
        /// </summary>
        /// <param name="payload">Original payload</param>
        /// <param name="reason">Failure reason</param>
        private void PublishFailure(InventoryReservedPayload payload, string reason)
        {
            _eventAggregator.GetEvent<PaymentFailedEvent>().Publish(new PaymentFailedPayload
            {
                OrderId = payload.OrderId,
                Reason = reason
            });
        }

        /// <summary>
        /// Logs messages to the transaction log.
        /// </summary>
        private void Log(string message, string type)
        {
            _eventAggregator.GetEvent<TransactionLogEvent>().Publish(new TransactionLogPayload
            {
                Message = $"[PaymentServiceWithRetry] {message}",
                Type = type
            });
        }

        // =============================================================================
        // CUSTOM EXCEPTION TYPES
        // =============================================================================
        
        /// <summary>
        /// Represents transient errors that may resolve with retry.
        /// Examples: Network timeout, temporary service unavailability, rate limiting.
        /// </summary>
        private class TransientPaymentException : Exception
        {
            public TransientPaymentException(string message) : base(message) { }
        }

        /// <summary>
        /// Represents permanent errors that won't resolve with retry.
        /// Examples: Insufficient funds, invalid card, fraud detection.
        /// </summary>
        private class PermanentPaymentException : Exception
        {
            public PermanentPaymentException(string message) : base(message) { }
        }

        /// <summary>
        /// Result object for payment processing.
        /// </summary>
        private class PaymentResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
        }
    }
}
