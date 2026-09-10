using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingBackend.Middleware;

/// <summary>
/// Global exception handler: never leaks ex.Message / stack / SQL errors to clients.
/// Returns a generic envelope + correlation id; full details go to server logs only.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;
            _logger.LogWarning(ex, "Optimistic concurrency conflict. CorrelationId={CorrelationId} Path={Path}", correlationId, context.Request.Path);
            if (context.Response.HasStarted) throw;
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = "This record was changed by another user. Refresh and try again.", correlationId });
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;
            _logger.LogError(ex, "Unhandled exception. CorrelationId={CorrelationId} Path={Path}", correlationId, context.Request.Path);
            if (context.Response.HasStarted)
                throw;
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                message = "An unexpected error occurred. Please try again.",
                correlationId
            });
        }
    }
}
