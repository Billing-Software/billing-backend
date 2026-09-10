using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using BillingBackend.Data;
using BillingBackend.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingBackend.Controllers
{
    public class UpgradeSubscriptionRequestDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "PlanId must be positive.")]
        public int PlanId { get; set; }
        [Required]
        [RegularExpression("^(monthly|yearly)$", ErrorMessage = "BillingCycle must be monthly or yearly.")]
        public string BillingCycle { get; set; } = "monthly"; // monthly or yearly
        [StringLength(50)]
        public string? PaymentMethod { get; set; } = "Razorpay";
        [StringLength(100)]
        public string? RazorpayPaymentId { get; set; }
        [StringLength(100)]
        public string? RazorpayOrderId { get; set; }
        [StringLength(500)]
        public string? RazorpaySignature { get; set; }
    }

    public class SubscriptionController : BaseApiController
    {
        private readonly BillingDbContext _context;
        private readonly ILogger<SubscriptionController> _logger;

        public SubscriptionController(BillingDbContext context, ILogger<SubscriptionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentSubscription()
        {
            try
            {
                var business = await _context.Businesses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == CurrentBusinessId);

                if (business == null)
                {
                    return NotFound(new { message = "Business not found." });
                }

                // Get Active Plan details
                var activePlan = await _context.SubscriptionPlans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == business.ActivePlanId)
                    ?? await _context.SubscriptionPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1);

                // Get All Active Plans
                var allPlans = await _context.SubscriptionPlans
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.DisplayOrder)
                    .AsNoTracking()
                    .ToListAsync();

                // Live Usage Quotas
                var branchCount = await _context.Branches.CountAsync(b => b.BusinessId == CurrentBusinessId && b.IsActive);
                var staffCount = await _context.StaffMembers.CountAsync(s => s.BusinessId == CurrentBusinessId && s.Status == "Active");
                var billCount = await _context.Bills.CountAsync(b => b.BusinessId == CurrentBusinessId);

                // Past Billing Transactions
                var transactions = await _context.PaymentTransactions
                    .Where(pt => pt.BusinessId == CurrentBusinessId)
                    .OrderByDescending(pt => pt.CreatedAt)
                    .Take(10)
                    .AsNoTracking()
                    .Select(pt => new
                    {
                        pt.Id,
                        pt.RazorpayPaymentId,
                        pt.RazorpayOrderId,
                        pt.Amount,
                        pt.Status,
                        pt.PaymentMethod,
                        pt.CreatedAt
                    })
                    .ToListAsync();

                var now = DateTime.UtcNow;
                var isTrial = business.IsTrial;
                var trialStartsAt = business.TrialStartsAt ?? business.CreatedAt;
                var trialEndsAt = business.TrialEndsAt ?? trialStartsAt.AddDays(7);
                var isTrialExpired = isTrial && trialEndsAt < now;
                var trialDaysRemaining = isTrial ? (int)Math.Max(0, Math.Ceiling((trialEndsAt - now).TotalDays)) : 0;
                var trialHoursRemaining = isTrial ? (int)Math.Max(0, Math.Ceiling((trialEndsAt - now).TotalHours)) : 0;

                var expiresAt = isTrial 
                    ? trialEndsAt 
                    : (business.SubscriptionExpiresAt ?? now.AddMonths(1));
                var daysRemaining = isTrial ? trialDaysRemaining : (int)Math.Max(0, (expiresAt - now).TotalDays);
                var isExpired = isTrial ? isTrialExpired : (expiresAt < now);

                var status = isTrial
                    ? (isTrialExpired ? "TrialExpired" : "Trial")
                    : (isExpired && business.SubscriptionStatus == "Active" ? "Expired" : business.SubscriptionStatus);

                return Ok(new
                {
                    businessId = business.Id,
                    legalName = business.LegalName,
                    tradingName = business.TradingName,
                    gstIn = business.GstIn,
                    activePlanId = business.ActivePlanId,
                    planName = activePlan?.Name ?? "Starter Shop",
                    planSubtitle = activePlan?.Subtitle ?? "Ideal for Single Kirana & Retail Stores",
                    monthlyPrice = activePlan?.MonthlyPrice ?? 499,
                    yearlyPrice = activePlan?.YearlyPrice ?? 4999,
                    subscriptionStatus = status,
                    isTrial = isTrial,
                    trialStartsAt = trialStartsAt,
                    trialEndsAt = trialEndsAt,
                    trialDaysRemaining = trialDaysRemaining,
                    trialHoursRemaining = trialHoursRemaining,
                    isTrialExpired = isTrialExpired,
                    subscriptionExpiresAt = expiresAt,
                    daysRemaining = daysRemaining,
                    isExpired = isExpired,
                    allowedBranches = business.AllowedBranches,
                    usedBranches = branchCount,
                    allowedStaff = business.AllowedStaff,
                    usedStaff = staffCount,
                    totalBills = billCount,
                    plans = allPlans.Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Subtitle,
                        p.MonthlyPrice,
                        p.YearlyPrice,
                        p.MaxBranches,
                        p.MaxStaff,
                        p.IsPopular,
                        p.DisplayOrder
                    }),
                    billingHistory = transactions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SubscriptionController] Error retrieving subscription details");
                return StatusCode(500, new { message = "Error loading subscription details.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        // Subscription activation is intentionally performed only by the verified Razorpay
        // payment flow (or its signed payment.captured webhook), never by a client request.
        [NonAction]
        public async Task<IActionResult> UpgradeSubscription([FromBody] UpgradeSubscriptionRequestDto dto)
        {
            try
            {
                var business = await _context.Businesses
                    .FirstOrDefaultAsync(b => b.Id == CurrentBusinessId);

                if (business == null)
                {
                    return NotFound(new { message = "Business not found." });
                }

                var targetPlan = await _context.SubscriptionPlans
                    .FirstOrDefaultAsync(p => p.Id == dto.PlanId);

                if (targetPlan == null)
                {
                    return BadRequest(new { message = "Invalid subscription plan specified." });
                }

                bool isYearly = string.Equals(dto.BillingCycle, "yearly", StringComparison.OrdinalIgnoreCase);
                decimal chargedAmount = isYearly ? targetPlan.YearlyPrice : targetPlan.MonthlyPrice;

                // Update business subscription
                business.ActivePlanId = targetPlan.Id;
                business.AllowedBranches = targetPlan.MaxBranches == -1 ? 999 : targetPlan.MaxBranches;
                business.AllowedStaff = targetPlan.MaxStaff == -1 ? 999 : targetPlan.MaxStaff;
                business.SubscriptionStatus = "Active";
                business.IsTrial = false;

                // Extend validity
                var baseDate = (business.SubscriptionExpiresAt.HasValue && business.SubscriptionExpiresAt.Value > DateTime.UtcNow)
                    ? business.SubscriptionExpiresAt.Value
                    : DateTime.UtcNow;

                business.SubscriptionExpiresAt = isYearly ? baseDate.AddYears(1) : baseDate.AddMonths(1);
                business.UpdatedAt = DateTime.UtcNow;

                // Record transaction
                var txn = new PaymentTransaction
                {
                    BusinessId = business.Id,
                    RazorpayPaymentId = string.IsNullOrWhiteSpace(dto.RazorpayPaymentId) 
                        ? $"pay_sim_{Guid.NewGuid().ToString().Substring(0, 10)}" 
                        : dto.RazorpayPaymentId,
                    RazorpayOrderId = dto.RazorpayOrderId,
                    Amount = chargedAmount,
                    Status = "Captured",
                    PaymentMethod = dto.PaymentMethod ?? "Razorpay",
                    CreatedAt = DateTime.UtcNow
                };

                _context.PaymentTransactions.Add(txn);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Successfully upgraded to {targetPlan.Name}!",
                    activePlanId = business.ActivePlanId,
                    planName = targetPlan.Name,
                    subscriptionStatus = business.SubscriptionStatus,
                    isTrial = false,
                    subscriptionExpiresAt = business.SubscriptionExpiresAt,
                    allowedBranches = business.AllowedBranches,
                    allowedStaff = business.AllowedStaff,
                    transactionId = txn.RazorpayPaymentId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SubscriptionController] Error upgrading subscription");
                return StatusCode(500, new { message = "Error upgrading subscription plan.", correlationId = HttpContext.TraceIdentifier });
            }
        }
    }
}
