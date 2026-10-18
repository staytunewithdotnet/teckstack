using Microsoft.AspNetCore.Http;
namespace BuildingBlocks.Middleware;
public class CorrelationIdMiddleware{private readonly RequestDelegate _n; public const string H="X-Correlation-Id"; public CorrelationIdMiddleware(RequestDelegate n)=>_n=n; public async Task InvokeAsync(HttpContext c){var id=c.Request.Headers[H].FirstOrDefault()??Guid.NewGuid().ToString(); c.Items[H]=id; c.Response.Headers[H]=id; using(Serilog.Context.LogContext.PushProperty("CorrelationId",id)) await _n(c);}}
public static class CorrelationExt{public static string GetHttpCorrelationId(this HttpContext c)=>c.Items["X-Correlation-Id"]?.ToString()??c.Request.Headers["X-Correlation-Id"].FirstOrDefault()??Guid.NewGuid().ToString();}
