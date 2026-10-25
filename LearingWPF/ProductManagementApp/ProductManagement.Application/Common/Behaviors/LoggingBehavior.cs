using MediatR;

using Microsoft.Extensions.Logging;

using System;

namespace ProductManagement.Application.Common.Behaviors
{
    /// <summary>
    /// Simple POCO model representing a Product.
    /// This represents the Domain layer in Clean Architecture - pure business object with no dependencies
    /// </summary>
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next
            , CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogInformation("[START] MediatR Request: {Request} | Data: {@Request}", requestName, request);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var response = await next();
                sw.Stop();
                _logger.LogInformation("[END] {Request} succeeded in {Elapsed}ms", requestName, sw.ElapsedMilliseconds);
                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "[FAIL] {Request} failed in {Elapsed}ms", requestName, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}