using Microsoft.AspNetCore.Http;

namespace BillingBackend.Middleware;

/// <summary>Defense-in-depth response headers (no dependency on UseHsts ordering surprises).</summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            // Swagger UI needs inline scripts; keep CSP report-only style permissive but still blocks framing/objects.
            headers["Content-Security-Policy"] =
                "default-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
            return Task.CompletedTask;
        });
        await _next(context);
    }
}
