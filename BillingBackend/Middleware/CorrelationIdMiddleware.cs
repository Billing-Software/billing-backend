using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BillingBackend.Middleware
{
    /// <summary>
    /// Middleware that generates a unique CorrelationId for every request.
    /// This ID is injected into ILogger scope so ALL logs for that request share the same ID.
    /// The ID is also returned in the response header for frontend debugging.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private const string CorrelationIdHeader = "X-Correlation-Id";

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Use client-provided correlation ID if available, otherwise generate one
            var correlationId = context.Request.Headers[CorrelationIdHeader].ToString();
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = Guid.NewGuid().ToString("N")[..12]; // Short but unique
            }

            // Store in HttpContext.Items for access by services (e.g., AuditService)
            context.Items["CorrelationId"] = correlationId;

            // Add to response headers for frontend debugging
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationIdHeader] = correlationId;
                return Task.CompletedTask;
            });

            // Add to logger scope so all logs within this request include the correlation ID
            using (_logger.BeginScope(new System.Collections.Generic.Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            }))
            {
                await _next(context);
            }
        }
    }
}
