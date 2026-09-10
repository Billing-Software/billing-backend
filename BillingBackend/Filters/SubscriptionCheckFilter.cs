using System;
using System.Threading.Tasks;
using BillingBackend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace BillingBackend.Filters
{
    /// <summary>
    /// Global Action Filter that automatically validates the subscription and suspension status
    /// of the business associated with the authenticated user.
    /// Returns 402 Payment Required for expired/past-due subscriptions and 403 Forbidden for suspensions.
    /// Bypasses check for SuperAdmins. Fail-closed for invalid scope.
    /// </summary>
    public class SubscriptionCheckFilter : IAsyncActionFilter
    {
        private readonly BillingDbContext _context;

        public SubscriptionCheckFilter(BillingDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;

            // Check only authenticated non-SuperAdmin users
            if (user?.Identity?.IsAuthenticated == true && !user.IsInRole("SuperAdmin"))
            {
                // Allow anonymous endpoints and auth/registration/webhook/health through.
                var endpoint = context.HttpContext.GetEndpoint();
                var allowAnonymous = endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>() != null;
                if (allowAnonymous)
                {
                    await next();
                    return;
                }

                var path = context.HttpContext.Request.Path.Value ?? string.Empty;
                if (path.StartsWith("/api/auth/", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/api/registration/", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/api/webhooks/", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("/health", StringComparison.OrdinalIgnoreCase))
                {
                    await next();
                    return;
                }

                var businessIdClaim = user.FindFirst("businessId")?.Value;
                if (!int.TryParse(businessIdClaim, out int businessId) || businessId <= 0)
                {
                    // Fail-closed: authenticated tenant request without valid scope is forbidden.
                    context.Result = new ObjectResult(new
                    {
                        message = "Invalid business scope. Please login again."
                    })
                    {
                        StatusCode = 403
                    };
                    return;
                }

                // Query only the required columns for maximum efficiency
                var business = await _context.Businesses
                    .Select(b => new {
                        b.Id,
                        b.IsSuspended,
                        b.SubscriptionStatus,
                        b.SubscriptionExpiresAt
                    })
                    .FirstOrDefaultAsync(b => b.Id == businessId);

                if (business == null)
                {
                    context.Result = new ObjectResult(new
                    {
                        message = "Business account not found. Please contact platform support."
                    })
                    {
                        StatusCode = 403
                    };
                    return;
                }

                // 1. Suspension Check
                if (business.IsSuspended)
                {
                    context.Result = new ObjectResult(new {
                        message = "Your business account has been suspended. Please contact platform support."
                    })
                    {
                        StatusCode = 403 // Forbidden
                    };
                    return;
                }

                // 2. Subscription Status & Expiry Check
                bool hasExpired = business.SubscriptionExpiresAt.HasValue && business.SubscriptionExpiresAt.Value < DateTime.UtcNow;

                // Block if status is explicitly Cancelled, Suspended, or is Inactive/PastDue and the expiry date has passed
                if (business.SubscriptionStatus == "Cancelled" ||
                    business.SubscriptionStatus == "Suspended" ||
                    (business.SubscriptionStatus == "PastDue" && hasExpired) ||
                    (business.SubscriptionStatus == "Inactive" && hasExpired))
                {
                    context.Result = new ObjectResult(new {
                        message = "Subscription expired or billing details pending. Please log in to complete subscription payment to resume billing access.",
                        status = business.SubscriptionStatus,
                        expiresAt = business.SubscriptionExpiresAt
                    })
                    {
                        StatusCode = 402 // Payment Required
                    };
                    return;
                }
            }

            await next();
        }
    }
}
