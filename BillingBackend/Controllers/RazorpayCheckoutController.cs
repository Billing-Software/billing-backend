using BillingBackend.Data;
using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using BillingBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillingBackend.Controllers;

[Authorize]
[ApiController]
[Route("api/razorpay")]
public sealed class RazorpayCheckoutController : ControllerBase
{
    private readonly BillingDbContext _context;
    private readonly IRazorpayService _razorpay;
    private readonly ILogger<RazorpayCheckoutController> _logger;
    public RazorpayCheckoutController(BillingDbContext context, IRazorpayService razorpay, ILogger<RazorpayCheckoutController> logger) => (_context, _razorpay, _logger) = (context, razorpay, logger);
    private int BusinessId => int.TryParse(User.FindFirst("businessId")?.Value, out var id) ? id : 0;

    [AllowAnonymous]
    [HttpGet("config")]
    public IActionResult GetConfig() => !_razorpay.IsConfigured
        ? Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Razorpay is not configured.")
        : Ok(new { keyId = _razorpay.GetKeyId(), currency = "INR" });

    [HttpPost("orders")]
    public async Task<ActionResult<CreateOrderResponseDto>> CreateOrder([FromBody] CreateOrderRequestDto request)
    {
        if (!ModelState.IsValid || request.PlanId <= 0 || !IsCycleValid(request.BillingCycle)) return BadRequest(new { message = "A valid planId and billingCycle (monthly or yearly) are required." });
        if (!_razorpay.IsConfigured) return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Payment gateway is not configured." });
        if (BusinessId <= 0) return Forbid();
        var plan = await _context.SubscriptionPlans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.PlanId && x.IsActive);
        if (plan is null) return BadRequest(new { message = "Selected subscription plan is unavailable." });

        var yearly = string.Equals(request.BillingCycle, "yearly", StringComparison.OrdinalIgnoreCase);
        var basePrice = yearly ? plan.YearlyPrice : plan.MonthlyPrice;
        var total = basePrice + Math.Round(basePrice * .18m, 2, MidpointRounding.AwayFromZero);
        var amount = decimal.ToInt64(decimal.Round(total * 100m, 0, MidpointRounding.AwayFromZero));
        var receipt = $"sub_{BusinessId}_{Guid.NewGuid():N}"[..40];
        var result = await _razorpay.CreateOrderAsync(amount, "INR", receipt, new Dictionary<string, string> { ["business_id"] = BusinessId.ToString(), ["plan_id"] = plan.Id.ToString(), ["billing_cycle"] = yearly ? "yearly" : "monthly" });
        if (!result.Success) return StatusCode(result.StatusCode, new { message = result.ErrorMessage ?? "Unable to create Razorpay order." });

        _context.PaymentTransactions.Add(new PaymentTransaction { BusinessId = BusinessId, SubscriptionPlanId = plan.Id, BillingCycle = yearly ? "yearly" : "monthly", RazorpayOrderId = result.OrderId, Amount = result.Amount / 100m, Status = "Created", PaymentMethod = "Razorpay", CorrelationId = HttpContext.TraceIdentifier, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();
        return Ok(new CreateOrderResponseDto { OrderId = result.OrderId!, Amount = result.Amount, Currency = result.Currency!, KeyId = _razorpay.GetKeyId(), PlanId = plan.Id, PlanName = plan.Name });
    }

    [HttpPost("payments/verify")]
    public async Task<ActionResult<VerifyPaymentResponseDto>> VerifyPayment([FromBody] VerifyPaymentRequestDto request)
    {
        var orderId = request.EffectiveOrderId?.Trim(); var paymentId = request.EffectivePaymentId?.Trim(); var signature = request.EffectiveSignature?.Trim();
        if (BusinessId <= 0) return Forbid();
        if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(paymentId) || string.IsNullOrWhiteSpace(signature)) return BadRequest(new VerifyPaymentResponseDto { Success = false, Message = "Razorpay order ID, payment ID, and signature are required." });
        var transaction = await _context.PaymentTransactions.SingleOrDefaultAsync(x => x.RazorpayOrderId == orderId);
        if (transaction is null || transaction.BusinessId != BusinessId) return NotFound(new VerifyPaymentResponseDto { Success = false, Message = "Payment order was not found." });
        if (transaction.Status == "Captured") return Ok(new VerifyPaymentResponseDto { Success = true, Message = "Payment has already been confirmed.", OrderId = orderId, PaymentId = transaction.RazorpayPaymentId });
        if (!_razorpay.VerifyOrderPaymentSignature(transaction.RazorpayOrderId!, paymentId, signature))
        {
            transaction.Status = "Failed"; transaction.FailureReason = "Signature mismatch"; transaction.UpdatedAt = DateTime.UtcNow; await _context.SaveChangesAsync();
            return BadRequest(new VerifyPaymentResponseDto { Success = false, Message = "Payment signature verification failed." });
        }

        var payment = await _razorpay.FetchPaymentAsync(paymentId);
        if (!payment.Success || payment.OrderId != transaction.RazorpayOrderId || payment.Amount != decimal.ToInt64(transaction.Amount * 100m))
        {
            _logger.LogWarning("Razorpay payment confirmation mismatch for order {OrderId}.", orderId);
            return BadRequest(new VerifyPaymentResponseDto { Success = false, Message = "Payment details could not be confirmed." });
        }
        transaction.RazorpayPaymentId = paymentId; transaction.PaymentMethod = payment.Method ?? "Razorpay"; transaction.UpdatedAt = DateTime.UtcNow;
        if (!string.Equals(payment.Status, "captured", StringComparison.OrdinalIgnoreCase))
        {
            transaction.Status = "Authorized"; await _context.SaveChangesAsync();
            return Accepted(new VerifyPaymentResponseDto { Success = false, PendingCapture = true, Message = "Payment is authorized and awaiting capture confirmation.", OrderId = orderId, PaymentId = paymentId });
        }
        await CaptureAndActivateAsync(transaction);
        return Ok(new VerifyPaymentResponseDto { Success = true, Message = "Payment captured and subscription activated.", OrderId = orderId, PaymentId = paymentId });
    }

    private async Task CaptureAndActivateAsync(PaymentTransaction transaction)
    {
        if (transaction.Status == "Captured") return;
        var business = await _context.Businesses.SingleAsync(x => x.Id == transaction.BusinessId);
        var plan = await _context.SubscriptionPlans.SingleAsync(x => x.Id == transaction.SubscriptionPlanId);
        var start = business.SubscriptionExpiresAt > DateTime.UtcNow ? business.SubscriptionExpiresAt.Value : DateTime.UtcNow;
        business.ActivePlanId = plan.Id; business.AllowedBranches = plan.MaxBranches == -1 ? 999 : plan.MaxBranches; business.AllowedStaff = plan.MaxStaff == -1 ? 999 : plan.MaxStaff;
        business.IsTrial = false; business.SubscriptionStatus = "Active"; business.SubscriptionExpiresAt = string.Equals(transaction.BillingCycle, "yearly", StringComparison.OrdinalIgnoreCase) ? start.AddYears(1) : start.AddMonths(1); business.UpdatedAt = DateTime.UtcNow;
        transaction.Status = "Captured"; transaction.UpdatedAt = DateTime.UtcNow; await _context.SaveChangesAsync();
    }
    private static bool IsCycleValid(string? cycle) => string.Equals(cycle, "monthly", StringComparison.OrdinalIgnoreCase) || string.Equals(cycle, "yearly", StringComparison.OrdinalIgnoreCase);
}
