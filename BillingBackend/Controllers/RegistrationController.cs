using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BillingBackend.Data;
using BillingBackend.Data.Entities;
using BillingBackend.DTOs;
using BillingBackend.Services;
using BillingBackend.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BillingBackend.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class RegistrationController : ControllerBase
    {
        private readonly BillingDbContext _context;
        private readonly IRazorpayService _razorpayService;
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegistrationController> _logger;

        public RegistrationController(
            BillingDbContext context,
            IRazorpayService razorpayService,
            IUserRepository userRepository,
            ITokenService tokenService,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<RegistrationController> logger)
        {
            _context = context;
            _razorpayService = razorpayService;
            _userRepository = userRepository;
            _tokenService = tokenService;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            try
            {
                // Never fall back to a hardcoded test key and never expose secrets.
                // Frontend only needs the public KeyId; secret/webhook stay server-side.
                var keyId = Environment.GetEnvironmentVariable("RAZORPAY_KEY_ID")
                    ?? _configuration["RAZORPAY_KEY_ID"]
                    ?? _configuration.GetSection("Razorpay")["KeyId"];
                if (string.IsNullOrWhiteSpace(keyId))
                    return StatusCode(503, new { message = "Payment gateway is not configured." });
                var merchantName = _configuration["Upi:MerchantName"] ?? "BillCom POS";
                return Ok(new
                {
                    keyId = keyId,
                    merchantName = merchantName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RegistrationController] Error retrieving Razorpay config");
                return StatusCode(500, new { message = "Error retrieving payment configuration." });
            }
        }

        [HttpGet("plans")]
        public async Task<IActionResult> GetPlans()
        {
            try
            {
                var dbPlans = await _context.SubscriptionPlans
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.DisplayOrder)
                    .ToListAsync();

                var plans = dbPlans.Select(p =>
                {
                    var subtitle = !string.IsNullOrWhiteSpace(p.Subtitle) ? p.Subtitle : (
                        p.Id == 1 ? "Ideal for Single Kirana, Small Cafes & Standalone Stores" :
                        p.Id == 2 ? "Perfect for High-Volume Retailers, Salons & Restaurants" :
                        "Custom Architecture for Large Multi-City Franchises"
                    );

                    var isPopular = p.IsPopular || p.Id == 2;

                    object[] features = Array.Empty<object>();
                    if (!string.IsNullOrWhiteSpace(p.FeaturesJson) && p.FeaturesJson != "[]")
                    {
                        try
                        {
                            features = System.Text.Json.JsonSerializer.Deserialize<object[]>(p.FeaturesJson) ?? Array.Empty<object>();
                        }
                        catch { }
                    }

                    if (features.Length == 0)
                    {
                        features = GetDefaultFeaturesForPlan(p.Id);
                    }

                    return new
                    {
                        id = p.Id,
                        name = p.Name,
                        subtitle = subtitle,
                        monthlyPrice = p.MonthlyPrice,
                        yearlyPrice = p.YearlyPrice,
                        maxBranches = p.MaxBranches,
                        maxStaff = p.MaxStaff,
                        isPopular = isPopular,
                        displayOrder = p.DisplayOrder,
                        razorpayPlanIdMonthly = p.RazorpayPlanIdMonthly,
                        razorpayPlanIdYearly = p.RazorpayPlanIdYearly,
                        features = features
                    };
                });

                return Ok(plans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RegistrationController] Error retrieving subscription plans");
                return StatusCode(500, new { message = "Error retrieving subscription plans.", correlationId = HttpContext.TraceIdentifier });
            }
        }

        private static object[] GetDefaultFeaturesForPlan(int planId)
        {
            if (planId == 1)
            {
                return new object[]
                {
                    new { text = "1 Branch & 2 Cashier Profiles", included = true },
                    new { text = "GST & Non-GST Invoicing", included = true },
                    new { text = "CRM Customer Directory", included = true },
                    new { text = "SMS Invoice Dispatches", included = true },
                    new { text = "Auto WhatsApp Webhooks", included = false },
                    new { text = "Multi-Branch Syncing", included = false }
                };
            }
            else if (planId == 2)
            {
                return new object[]
                {
                    new { text = "Up to 5 Branches Syncing", included = true },
                    new { text = "Up to 10 Cashier Profiles", included = true },
                    new { text = "Unlimited GST Invoices", included = true },
                    new { text = "Auto WhatsApp Webhooks", included = true },
                    new { text = "Stock Warning Alerts", included = true },
                    new { text = "Dedicated Database Node", included = false }
                };
            }
            else
            {
                return new object[]
                {
                    new { text = "Unlimited Branches & Cashiers", included = true },
                    new { text = "Dedicated Database Cluster", included = true },
                    new { text = "Custom PDF Invoice Templates", included = true },
                    new { text = "SMS + WhatsApp Gateway Sync", included = true },
                    new { text = "24/7 Priority Dedicated Manager", included = true },
                    new { text = "API Integrations & Webhooks", included = true }
                };
            }
        }

        [HttpPost("trial")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
        public async Task<IActionResult> StartFreeTrial([FromBody] RegisterDto dto, [FromQuery] int planId = 1)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                _logger.LogInformation("[RegistrationController] StartFreeTrial requested.");

                // 1. Validate if username/email already exists
                if (await _context.Users.AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower()))
                {
                    return BadRequest(new { message = "Username already exists. Please choose another username or login." });
                }

                if (await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
                {
                    return BadRequest(new { message = "Email already registered. Please login with your credentials." });
                }

                // 2. Resolve target plan (trial is always Starter to prevent free enterprise abuse)
                var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == 1 && p.IsActive)
                           ?? new SubscriptionPlan { Id = 1, Name = "Starter Shop", MaxBranches = 1, MaxStaff = 2, MonthlyPrice = 499.00m };

                // 3. Compute Password Hash (PBKDF2; policy-enforced)
                var (pwOkTrial, pwErrTrial) = BillingBackend.Security.PasswordPolicy.Validate(dto.Password);
                if (!pwOkTrial)
                    return BadRequest(new { message = pwErrTrial });
                BillingBackend.Security.PasswordHasher.CreateHash(dto.Password, out var passwordHash, out var passwordSalt);

                // 4. Register User and Business with 7-Day Free Trial
                dto.PlanId = plan.Id;
                var result = await _userRepository.RegisterUserAndBusinessAsync(dto, passwordHash, passwordSalt);
                if (result == null)
                {
                    return BadRequest(new { message = "Failed to create business profile." });
                }

                // 5. Generate Auth Tokens
                var dummyUser = new User
                {
                    Id = result.UserId,
                    Username = result.Username,
                    Email = result.Email,
                    Role = result.Role
                };

                var token = _tokenService.CreateToken(dummyUser, result.BusinessId);
                var refreshToken = _tokenService.GenerateRefreshToken();

                var dbRefreshToken = new UserRefreshToken
                {
                    UserId = dummyUser.Id,
                    Token = refreshToken,
                    ExpiryTime = DateTime.UtcNow.AddDays(30),
                    IsRevoked = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _userRepository.AddRefreshTokenAsync(dbRefreshToken);
                await _userRepository.SaveChangesAsync();

                // 6. Asynchronous Welcome Email
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var emailBody = $@"
                            <h2>Welcome to BillCom 7-Day Free Trial!</h2>
                            <p>Dear {result.Username},</p>
                            <p>Your 7-day free trial on the <strong>{plan.Name}</strong> plan is now active.</p>
                            <p><strong>Store Name:</strong> {dto.LegalName}</p>
                            <p><strong>Trial Period:</strong> 7 Days Full Access</p>
                            <p><strong>Allowed Branches:</strong> {(plan.MaxBranches == -1 ? "Unlimited" : plan.MaxBranches.ToString())}</p>
                            <p><strong>Allowed Staff:</strong> {(plan.MaxStaff == -1 ? "Unlimited" : plan.MaxStaff.ToString())}</p>
                            <p>Enjoy full features risk-free! Upgrade anytime from the Subscription menu.</p>
                            <br/>
                            <p>Best regards,<br/>BillCom POS Team</p>";
                        await _emailService.SendEmailAsync(result.Email, "BillCom - 7-Day Free Trial Active!", emailBody);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[Welcome Trial Email Error]");
                    }
                });

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    Username = result.Username,
                    Email = result.Email,
                    Role = result.Role,
                    BusinessId = result.BusinessId,
                    BusinessName = result.BusinessName,
                    OnboardingPending = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RegistrationController] Error in StartFreeTrial");
                return StatusCode(500, new { message = "Could not start trial. Please try again." });
            }
        }

        [HttpPost("start")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
        public async Task<IActionResult> StartRegistration([FromBody] RegisterDto dto, [FromQuery] int planId, [FromQuery] string? billingCycle = "monthly")
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            if (planId <= 0)
                return BadRequest(new { message = "A valid planId is required." });
            if (!string.Equals(billingCycle, "monthly", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(billingCycle, "yearly", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "billingCycle must be monthly or yearly." });
            try
            {
                _logger.LogInformation("[RegistrationController] StartRegistration requested.");

                // 1. Validate if username/email already exists in active Users
                if (await _context.Users.AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower()))
                {
                    _logger.LogWarning("[RegistrationController] Registration failed - username exists.");
                    return BadRequest(new { message = "Username already exists." });
                }

                if (await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
                {
                    _logger.LogWarning("[RegistrationController] Registration failed - email exists.");
                    return BadRequest(new { message = "Email already exists." });
                }

                // 2. Resolve Subscription Plan (active only; no fallback to attacker-controlled defaults)
                var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == planId && p.IsActive);
                if (plan == null)
                    return BadRequest(new { message = "Selected subscription plan is unavailable." });

                bool isYearly = string.Equals(billingCycle, "yearly", StringComparison.OrdinalIgnoreCase);
                string? razorpayPlanId = isYearly ? plan.RazorpayPlanIdYearly : plan.RazorpayPlanIdMonthly;
                if (string.IsNullOrWhiteSpace(razorpayPlanId))
                    return StatusCode(500, new { message = "Subscription plan is misconfigured." });
                decimal baseAmount = isYearly ? plan.YearlyPrice : plan.MonthlyPrice;
                decimal gstAmount = Math.Round(baseAmount * 0.18m, 2, MidpointRounding.AwayFromZero);
                decimal totalAmount = baseAmount + gstAmount;
                long amountInPaise = (long)Math.Round(totalAmount * 100m, MidpointRounding.AwayFromZero);

                if (!_razorpayService.IsConfigured)
                    return StatusCode(503, new { message = "Payment gateway is not configured." });

                // 3. Create Razorpay Customer & Subscription (fail closed — no simulated IDs)
                var razorpayCustomerId = await _razorpayService.CreateCustomerAsync(dto.Username, dto.Email, dto.BusinessPhone ?? "");
                if (string.IsNullOrWhiteSpace(razorpayCustomerId))
                    return StatusCode(502, new { message = "Could not initialize payment customer." });
                var razorpaySubscriptionId = await _razorpayService.CreateSubscriptionAsync(razorpayPlanId, razorpayCustomerId);
                if (string.IsNullOrWhiteSpace(razorpaySubscriptionId))
                    return StatusCode(502, new { message = "Could not initialize subscription." });

                // 3b. Create official Razorpay Order for Universal Payment Support (UPI, Cards, NetBanking, Wallets)
                var receipt = $"reg_{Guid.NewGuid():N}".Substring(0, 20);
                var notes = new Dictionary<string, string>
                {
                    { "email", dto.Email },
                    { "username", dto.Username },
                    { "planId", plan.Id.ToString() },
                    { "planName", plan.Name },
                    { "billingCycle", isYearly ? "yearly" : "monthly" },
                    { "legalName", dto.LegalName }
                };

                var (orderSuccess, orderId, orderAmt, orderCurr, orderErr, _) =
                    await _razorpayService.CreateOrderAsync(amountInPaise, "INR", receipt, notes);

                if (!orderSuccess || string.IsNullOrWhiteSpace(orderId))
                    return StatusCode(502, new { message = orderErr ?? "Could not create payment order." });

                string finalOrderId = orderId;

                // 4. Hash password for draft state (PBKDF2; validate now so activation can't use weak password)
                var (pwOkStart, pwErrStart) = BillingBackend.Security.PasswordPolicy.Validate(dto.Password);
                if (!pwOkStart)
                    return BadRequest(new { message = pwErrStart });
                BillingBackend.Security.PasswordHasher.CreateHash(dto.Password, out var passwordHash2, out var passwordSalt2);
                var passwordHash = passwordHash2;
                var passwordSalt = passwordSalt2;

                // 5. Save Pending Registration (never persist plaintext password in RawRegistrationData)
                var sanitizedDto = System.Text.Json.JsonSerializer.Deserialize<RegisterDto>(
                    System.Text.Json.JsonSerializer.Serialize(dto));
                string rawData;
                if (sanitizedDto != null)
                {
                    sanitizedDto.Password = "";
                    sanitizedDto.Role = "Owner";
                    rawData = System.Text.Json.JsonSerializer.Serialize(sanitizedDto);
                }
                else
                {
                    rawData = "{}";
                }
                var token = Guid.NewGuid().ToString("N");
                var pending = new PendingRegistration
                {
                    Token = token,
                    Email = dto.Email,
                    Username = dto.Username,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    LegalName = dto.LegalName,
                    Phone = dto.BusinessPhone,
                    GstIn = dto.GstIn,
                    Address = dto.BusinessAddress,
                    SelectedPlanId = plan.Id,
                    RazorpayCustomerId = razorpayCustomerId,
                    RazorpaySubscriptionId = razorpaySubscriptionId,
                    RazorpayOrderId = finalOrderId,
                    Status = "PendingPayment",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    RawRegistrationData = rawData
                };

                await _context.PendingRegistrations.AddAsync(pending);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[RegistrationController] Pending registration session created.");

                // Frontend must use Razorpay Checkout (orderId + keyId). No direct UPI VPA/URI to avoid gateway bypass.
                return Ok(new
                {
                    token = token,
                    orderId = finalOrderId,
                    subscriptionId = razorpaySubscriptionId,
                    customerId = razorpayCustomerId,
                    keyId = _razorpayService.GetKeyId(),
                    email = dto.Email,
                    businessName = dto.LegalName,
                    planId = plan.Id,
                    planName = plan.Name,
                    baseAmount = baseAmount,
                    gstAmount = gstAmount,
                    amount = totalAmount,
                    amountInPaise = amountInPaise,
                    currency = "INR",
                    billingCycle = isYearly ? "yearly" : "monthly"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RegistrationController] Error in StartRegistration");
                return StatusCode(500, new { message = "Could not start registration." });
            }
        }

        [HttpPost("verify-payment")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("strict")]
        public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest request)
        {
            try
            {
                _logger.LogInformation("[RegistrationController] VerifyPayment requested.");

                // Fetch Pending Registration with transaction lock/state check
                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                
                var pending = await _context.PendingRegistrations
                    .FirstOrDefaultAsync(p => p.Token == request.Token);
                
                if (pending == null)
                {
                    _logger.LogWarning("[RegistrationController] Registration session not found.");
                    return NotFound(new { message = "Registration session not found." });
                }

                // If already completed (race condition mitigation), retrieve cached tokens instead of duplicate business registration
                if (pending.Status == "Completed")
                {
                    _logger.LogInformation("[RegistrationController] Registration already completed. Returning cached credentials.");
                    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == pending.Email.ToLower() || u.Username.ToLower() == pending.Username.ToLower());
                    if (existingUser != null)
                    {
                        var biz = await _context.Businesses.FirstOrDefaultAsync(b => b.OwnerId == existingUser.Id);
                        var existingToken = _tokenService.CreateToken(existingUser, biz?.Id ?? 0);
                        var existingRefreshToken = await _context.UserRefreshTokens
                            .Where(rt => rt.UserId == existingUser.Id && !rt.IsRevoked)
                            .OrderByDescending(rt => rt.CreatedAt)
                            .FirstOrDefaultAsync();

                        await dbTransaction.CommitAsync();
                        return Ok(new AuthResponseDto
                        {
                            Token = existingToken,
                            RefreshToken = existingRefreshToken?.Token ?? "",
                            Username = existingUser.Username,
                            Email = existingUser.Email,
                            Role = existingUser.Role,
                            BusinessId = biz?.Id ?? 0,
                            BusinessName = biz?.LegalName ?? "",
                            OnboardingPending = true
                        });
                    }
                    await dbTransaction.RollbackAsync();
                    return BadRequest(new { message = "Registration already completed, but matching user account was not found." });
                }

                if (pending.Status == "Processing")
                {
                    _logger.LogWarning("[RegistrationController] Registration is currently being processed by another thread.");
                    await dbTransaction.RollbackAsync();
                    return BadRequest(new { message = "Registration is currently being processed. Please wait..." });
                }

                // Transition status to Processing to block concurrent requests
                pending.Status = "Processing";
                _context.PendingRegistrations.Update(pending);
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync(); // Commit state change to Processing

                // INDUSTRIAL: all activations REQUIRE Razorpay order signature + server-side payment confirmation.
                // Manual UPI TransactionId path removed (was a payment bypass: any string activated accounts).
                if (string.IsNullOrWhiteSpace(request.RazorpayPaymentId) ||
                    string.IsNullOrWhiteSpace(request.RazorpaySignature))
                {
                    pending.Status = "PendingPayment";
                    _context.PendingRegistrations.Update(pending);
                    await _context.SaveChangesAsync();
                    return BadRequest(new { message = "Razorpay payment ID and signature are required." });
                }

                var orderIdToVerify = !string.IsNullOrEmpty(request.RazorpayOrderId) ? request.RazorpayOrderId : pending.RazorpayOrderId;
                if (string.IsNullOrWhiteSpace(orderIdToVerify) &&
                    string.IsNullOrWhiteSpace(request.RazorpaySubscriptionId) &&
                    string.IsNullOrWhiteSpace(pending.RazorpaySubscriptionId))
                {
                    pending.Status = "PendingPayment";
                    _context.PendingRegistrations.Update(pending);
                    await _context.SaveChangesAsync();
                    return BadRequest(new { message = "Payment order reference is required." });
                }

                // Reject simulated / placeholder IDs in production (test backdoor guard).
                var allIds = $"{orderIdToVerify} {request.RazorpayPaymentId} {request.RazorpaySubscriptionId} {pending.RazorpaySubscriptionId}";
                if (allIds.Contains("simulated", StringComparison.OrdinalIgnoreCase) ||
                    allIds.Contains("order_sim_", StringComparison.OrdinalIgnoreCase) ||
                    allIds.StartsWith("UPI_", StringComparison.OrdinalIgnoreCase))
                {
                    pending.Status = "PendingPayment";
                    _context.PendingRegistrations.Update(pending);
                    await _context.SaveChangesAsync();
                    return BadRequest(new { message = "Simulated payments are not accepted." });
                }

                bool isSignatureValid = false;
                if (!string.IsNullOrEmpty(orderIdToVerify))
                {
                    isSignatureValid = _razorpayService.VerifyOrderPaymentSignature(
                        orderIdToVerify,
                        request.RazorpayPaymentId,
                        request.RazorpaySignature);
                }
                if (!isSignatureValid)
                {
                    var subToVerify = !string.IsNullOrEmpty(request.RazorpaySubscriptionId)
                        ? request.RazorpaySubscriptionId : pending.RazorpaySubscriptionId;
                    if (!string.IsNullOrEmpty(subToVerify))
                    {
                        isSignatureValid = _razorpayService.VerifyPaymentSignature(
                            subToVerify,
                            request.RazorpayPaymentId,
                            request.RazorpaySignature);
                    }
                }

                if (!isSignatureValid)
                {
                    _logger.LogWarning("[RegistrationController] Payment signature verification failed.");

                    // Roll back state to PendingPayment
                    pending.Status = "PendingPayment";
                    _context.PendingRegistrations.Update(pending);
                    await _context.SaveChangesAsync();

                    return BadRequest(new { message = "Payment signature verification failed. Unauthorized transaction." });
                }

                // 3. Resolve limits from plan
                var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == pending.SelectedPlanId)
                           ?? new SubscriptionPlan { Id = 1, Name = "Starter Shop", MaxBranches = 1, MaxStaff = 2, MonthlyPrice = 499.00m, YearlyPrice = 4999.00m };

                // Server-side amount confirmation: payment must be captured and match plan total (monthly or yearly + 18% GST).
                var fetched = await _razorpayService.FetchPaymentAsync(request.RazorpayPaymentId);
                if (!fetched.Success || !string.Equals(fetched.Status, "captured", StringComparison.OrdinalIgnoreCase))
                {
                    pending.Status = "PendingPayment";
                    _context.PendingRegistrations.Update(pending);
                    await _context.SaveChangesAsync();
                    return BadRequest(new { message = "Payment has not been captured. Please complete payment first." });
                }
                if (!string.IsNullOrWhiteSpace(orderIdToVerify) && !string.IsNullOrWhiteSpace(fetched.OrderId) &&
                    !string.Equals(fetched.OrderId, orderIdToVerify, StringComparison.Ordinal))
                {
                    pending.Status = "PendingPayment";
                    _context.PendingRegistrations.Update(pending);
                    await _context.SaveChangesAsync();
                    return BadRequest(new { message = "Payment does not belong to this registration order." });
                }
                var monthlyTotalPaise = (long)Math.Round((plan.MonthlyPrice * 1.18m) * 100m, MidpointRounding.AwayFromZero);
                var yearlyTotalPaise = (long)Math.Round((plan.YearlyPrice * 1.18m) * 100m, MidpointRounding.AwayFromZero);
                if (fetched.Amount != monthlyTotalPaise && fetched.Amount != yearlyTotalPaise)
                {
                    _logger.LogWarning("[RegistrationController] Payment amount mismatch. Expected {Monthly} or {Yearly}, got {Actual}.",
                        monthlyTotalPaise, yearlyTotalPaise, fetched.Amount);
                    pending.Status = "PendingPayment";
                    _context.PendingRegistrations.Update(pending);
                    await _context.SaveChangesAsync();
                    return BadRequest(new { message = "Payment amount does not match the selected plan." });
                }
                bool isYearlyCycle = fetched.Amount == yearlyTotalPaise;

                // 4. Map back to RegisterDto
                var registerDto = new RegisterDto
                {
                    Username = pending.Username,
                    Email = pending.Email,
                    Password = "", // Password hash already computed
                    LegalName = pending.LegalName,
                    BusinessPhone = pending.Phone,
                    BusinessAddress = pending.Address,
                    GstIn = pending.GstIn
                };

                if (!string.IsNullOrEmpty(pending.RawRegistrationData))
                {
                    try
                    {
                        var deserialized = System.Text.Json.JsonSerializer.Deserialize<RegisterDto>(pending.RawRegistrationData);
                        if (deserialized != null)
                        {
                            registerDto = deserialized;
                            registerDto.Password = "";
                        }
                    }
                    catch (Exception) { /* Fallback to standard mapping */ }
                }

                // 5. Activate User & Business (expiry follows verified billing cycle)
                var expiresAt = isYearlyCycle ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1);

                _logger.LogInformation("[RegistrationController] Registering user and business database entities.");

                var result = await _userRepository.RegisterUserAndBusinessWithSubscriptionAsync(
                    registerDto,
                    pending.PasswordHash,
                    pending.PasswordSalt,
                    plan.Id,
                    plan.MaxBranches,
                    plan.MaxStaff,
                    pending.RazorpayCustomerId ?? "",
                    pending.RazorpaySubscriptionId ?? "",
                    expiresAt
                );

                if (result == null)
                {
                    _logger.LogError("[RegistrationController] Failed to create database entities for user/business.");
                    // Reset to PendingPayment
                    pending.Status = "PendingPayment";
                    _context.PendingRegistrations.Update(pending);
                    await _context.SaveChangesAsync();

                    return BadRequest(new { message = "Failed to create accounts during activation." });
                }

                // 6. Complete Pending Registration
                pending.Status = "Completed";
                _context.PendingRegistrations.Update(pending);

                // 7. Track Payment Transaction (server-verified values only)
                var transaction = new PaymentTransaction
                {
                    BusinessId = result.BusinessId,
                    RazorpayPaymentId = request.RazorpayPaymentId,
                    RazorpayOrderId = orderIdToVerify,
                    RazorpaySubscriptionId = pending.RazorpaySubscriptionId,
                    PaymentMethod = fetched.Method ?? "Razorpay",
                    Amount = fetched.Amount / 100m,
                    Status = "Captured",
                    CreatedAt = DateTime.UtcNow,
                    CorrelationId = HttpContext?.Items["CorrelationId"]?.ToString()
                };
                await _context.PaymentTransactions.AddAsync(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[RegistrationController] Account successfully activated. BusinessId: {BizId}", result.BusinessId);

                // 8. Generate JWT Auth Access & Refresh Tokens
                var dummyUser = new User
                {
                    Id = result.UserId,
                    Username = result.Username,
                    Email = result.Email,
                    Role = result.Role
                };

                var token = _tokenService.CreateToken(dummyUser, result.BusinessId);
                var refreshToken = _tokenService.GenerateRefreshToken();

                var dbRefreshToken = new UserRefreshToken
                {
                    UserId = dummyUser.Id,
                    Token = refreshToken,
                    ExpiryTime = DateTime.UtcNow.AddDays(30),
                    IsRevoked = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _userRepository.AddRefreshTokenAsync(dbRefreshToken);
                await _userRepository.SaveChangesAsync();

                // Send Welcome email asynchronously (non-blocking)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var emailBody = $@"
                            <h2>Welcome to BillCom!</h2>
                            <p>Dear {pending.Username},</p>
                            <p>Your subscription is successfully activated for <strong>{plan.Name}</strong>.</p>
                            <p><strong>Business Name:</strong> {pending.LegalName}</p>
                            <p><strong>Branch Limit:</strong> {(plan.MaxBranches == -1 ? "Unlimited" : plan.MaxBranches.ToString())} Branch(es)</p>
                            <p>Download our recommended Android App or open the web client to start billing today.</p>
                            <br/>
                            <p>Best regards,<br/>BillCom Operations Team</p>";

                        await _emailService.SendEmailAsync(pending.Email, "BillCom - Account Activated!", emailBody);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[Welcome Email Error]");
                    }
                });

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    Username = result.Username,
                    Email = result.Email,
                    Role = result.Role,
                    BusinessId = result.BusinessId,
                    BusinessName = result.BusinessName,
                    OnboardingPending = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RegistrationController] Error in VerifyPayment");
                return StatusCode(500, new { message = "Verification failed. Please try again." });
            }
        }

        [HttpGet("resume/{token}")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
        public async Task<IActionResult> ResumeRegistration(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token) || token.Length > 100)
                    return BadRequest(new { message = "Invalid token." });

                var pending = await _context.PendingRegistrations.FirstOrDefaultAsync(p => p.Token == token);
                if (pending == null)
                {
                    _logger.LogWarning("[RegistrationController] Pending registration not found.");
                    return NotFound(new { message = "Checkout token invalid or expired." });
                }

                if (pending.ExpiresAt < DateTime.UtcNow || pending.Status == "Completed")
                    return BadRequest(new { message = "Checkout session expired. Please start again." });

                var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == pending.SelectedPlanId)
                           ?? new SubscriptionPlan { Id = 1, Name = "Starter Shop", MonthlyPrice = 499.00m, YearlyPrice = 4999.00m };

                // Do not expose PII beyond what checkout needs; frontend uses Razorpay order.
                return Ok(new
                {
                    token = pending.Token,
                    orderId = pending.RazorpayOrderId,
                    subscriptionId = pending.RazorpaySubscriptionId,
                    customerId = pending.RazorpayCustomerId,
                    keyId = _razorpayService.IsConfigured ? _razorpayService.GetKeyId() : null,
                    planId = plan.Id,
                    planName = plan.Name,
                    monthlyPrice = plan.MonthlyPrice,
                    yearlyPrice = plan.YearlyPrice
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RegistrationController] Error resuming registration session");
                return StatusCode(500, new { message = "Error resuming registration session." });
            }
        }

        [HttpPost("mark-failed")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
        public async Task<IActionResult> MarkFailed([FromBody] MarkFailedRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Token) || request.Token.Length > 100)
                    return BadRequest(new { message = "Invalid token." });

                var pending = await _context.PendingRegistrations.FirstOrDefaultAsync(p => p.Token == request.Token);
                if (pending == null)
                {
                    return NotFound();
                }

                pending.Status = "Failed";
                _context.PendingRegistrations.Update(pending);

                // Track Failed Payment Transaction in DB (truncate reason to prevent oversized input)
                var safeReason = string.IsNullOrWhiteSpace(request.Reason)
                    ? "Payment session cancelled/dismissed by user."
                    : request.Reason.Trim()[..Math.Min(450, request.Reason.Trim().Length)];
                var transaction = new PaymentTransaction
                {
                    RazorpaySubscriptionId = pending.RazorpaySubscriptionId,
                    RazorpayOrderId = pending.RazorpayOrderId,
                    Status = "Failed",
                    FailureReason = safeReason,
                    CreatedAt = DateTime.UtcNow,
                    CorrelationId = HttpContext?.Items["CorrelationId"]?.ToString()
                };
                await _context.PaymentTransactions.AddAsync(transaction);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RegistrationController] Error marking registration failed");
                return StatusCode(500, new { message = "Error updating registration status." });
            }
        }
    }

    public class VerifyPaymentRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 8)]
        public string Token { get; set; } = string.Empty;
        [StringLength(100)]
        public string? RazorpayOrderId { get; set; }
        [StringLength(100)]
        public string? RazorpaySubscriptionId { get; set; }
        [Required]
        [StringLength(100)]
        public string RazorpayPaymentId { get; set; } = string.Empty;
        [Required]
        [StringLength(500)]
        public string RazorpaySignature { get; set; } = string.Empty;
    }

    public class MarkFailedRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 8)]
        public string Token { get; set; } = string.Empty;
        [StringLength(1000)]
        public string? Reason { get; set; }
    }
}
