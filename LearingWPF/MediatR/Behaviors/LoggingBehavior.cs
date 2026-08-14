using MediatR;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WpfMediatRLearningApp.Behaviors
{
    /// <summary>
    /// Section 7.1: Middleware that wraps every request to log execution time.
    /// IPipelineBehavior allows us to intercept requests before and after they reach the handler.
    /// </summary>
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(
            TRequest request, 
            RequestHandlerDelegate<TResponse> next, 
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            Debug.WriteLine($"[PIPELINE] Starting request: {requestName}");

            var stopwatch = Stopwatch.StartNew();

            // 'next()' invokes the next behavior in the chain, or the actual handler if this is the last behavior.
            var response = await next();

            stopwatch.Stop();
            Debug.WriteLine($"[PIPELINE] Completed request: {requestName} in {stopwatch.ElapsedMilliseconds}ms");

            return response;
        }
    }
}
