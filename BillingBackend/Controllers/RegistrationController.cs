using System;
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
                var rzpSection = _configuration.GetSection("Razorpay");
                var upiVpa = _configuration["Upi:Vpa"] ?? "billcom.payments@okaxis";
                var merchantName = _configuration["Upi:MerchantName"] ?? "BillCom POS";
                return Ok(new 
                { 
                    keyId = rzpSection["KeyId"] ?? "rzp_test_mockKeyId123",
                    upiVpa = upiVpa,
                    merchantName = merchantName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RegistrationController] Error retrieving Razorpay config");
                return StatusCode(500, new { message = "Error retrieving Razorpay key configuration.", error = ex.Message });
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
                return StatusCode(500, new { message = "Error retrieving subscription plans.", error = ex.Message });
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
        public async Task<IActionResult> StartFreeTrial([FromBody] RegisterDto dto, [FromQuery] int planId = 1)
        {
            try
            {
                _logger.LogInformation("[RegistrationController] StartFreeTrial requested for Username: {Username}, Email: {Email}, Plan: {PlanId}", dto.Username, dto.Email, planId);

                // 1. Validate if username/email already exists
                if (await _context.Users.AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower()))
                {
                    return BadRequest(new { message = "Username already exists. Please choose another username or login." });
                }

                if (await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
                {
                    return BadRequest(new { message = "Email already registered. Please login with your credentials." });
                }

                // 2. Resolve target plan
                var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == planId)
                           ?? await _context.SubscriptionPlans.FirstOrDefaultAsync()
                           ?? new SubscriptionPlan { Id = 1, Name = "Starter Shop", MaxBranches = 1, MaxStaff = 2, MonthlyPrice = 499.00m };

                // 3. Compute Password Hash
                using var hmac = new HMACSHA512();
                var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password));
                var passwordSalt = hmac.Key;

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
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartRegistration([FromBody] RegisterDto dto, [FromQuery] int planId, [FromQuery] string? billingCycle = "monthly")
        {
            try
            {
                _logger.LogInformation("[RegistrationController] StartRegistration requested for Username: {Username}, Email: {Email}, Plan: {PlanId}, Cycle: {BillingCycle}", dto.Username, dto.Email, planId, billingCycle);

                // 1. Validate if username/email already exists in active Users
                if (await _context.Users.AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower()))
                {
                    _logger.LogWarning("[RegistrationController] Registration failed - Username already exists: {Username}", dto.Username);
                    return BadRequest(new { message = "Username already exists." });
                }

                if (await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
                {
                    _logger.LogWarning("[RegistrationController] Registration failed - Email already exists: {Email}", dto.Email);
                    return BadRequest(new { message = "Email already exists." });
                }

                // 2. Resolve Subscription Plan by fixed ID from SubscriptionPlans table
                var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == planId)
                           ?? await _context.SubscriptionPlans.FirstOrDefaultAsync()
                           ?? new SubscriptionPlan { Id = 1, RazorpayPlanIdMonthly = "plan_starter_monthly", MonthlyPrice = 499.00m, YearlyPrice = 4999.00m, Name = "Starter Shop" };

                bool isYearly = string.Equals(billingCycle, "yearly", StringComparison.OrdinalIgnoreCase);
                string razorpayPlanId = isYearly && !string.IsNullOrWhiteSpace(plan.RazorpayPlanIdYearly)
                    ? plan.RazorpayPlanIdYearly
                    : plan.RazorpayPlanIdMonthly;
                decimal amount = isYearly ? plan.YearlyPrice : plan.MonthlyPrice;

                // 3. Create Razorpay Customer & Subscription
                var razorpayCustomerId = await _razorpayService.CreateCustomerAsync(dto.Username, dto.Email, dto.BusinessPhone ?? "");
                if (string.IsNullOrEmpty(razorpayCustomerId))
                {
                    _logger.LogError("[RegistrationController] Failed to register customer profile with Razorpay gateway.");
                    return StatusCode(500, new { message = "Failed to register customer profile with payment gateway." });
                }

                var razorpaySubscriptionId = await _razorpayService.CreateSubscriptionAsync(razorpayPlanId, razorpayCustomerId);
                if (string.IsNullOrEmpty(razorpaySubscriptionId))
                {
                    _logger.LogError("[RegistrationController] Failed to configure Razorpay subscription billing cycle.");
                    return StatusCode(500, new { message = "Failed to configure subscription billing cycle." });
                }

                // 4. Hash password for draft state
                using var hmac = new HMACSHA512();
                var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password));
                var passwordSalt = hmac.Key;

                // 5. Save Pending Registration
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
                    Status = "PendingPayment",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    RawRegistrationData = System.Text.Json.JsonSerializer.Serialize(dto)
                };

                await _context.PendingRegistrations.AddAsync(pending);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[RegistrationController] Pending registration session created. Token: {Token}", token);

                var upiVpa = _configuration["Upi:Vpa"] ?? "billcom.payments@okaxis";
                var merchantName = _configuration["Upi:MerchantName"] ?? "BillCom POS";
                var upiNote = Uri.EscapeDataString($"BillCom {plan.Name} Subscription");
                var upiUri = $"upi://pay?pa={upiVpa}&pn={Uri.EscapeDataString(merchantName)}&am={amount:F2}&cu=INR&tn={upiNote}&tr={token}";

                return Ok(new
                {
                    token = token,
                    subscriptionId = razorpaySubscriptionId,
                    customerId = razorpayCustomerId,
                    email = dto.Email,
                    businessName = dto.LegalName,
                    planId = plan.Id,
                    planName = plan.Name,
                    amount = amount,
                    billingCycle = isYearly ? "yearly" : "monthly",
                    upiVpa = upiVpa,
                    merchantName = merchantName,
                    upiUri = upiUri
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RegistrationController] Error in StartRegistration");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("verify-payment")]
        public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest request)
        {
            try
            {
                _logger.LogInformation("[RegistrationController] VerifyPayment requested. Token: {Token}, PaymentId: {PaymentId}, Method: {PaymentMethod}", request.Token, request.RazorpayPaymentId, request.PaymentMethod);

                // Fetch Pending Registration with transaction lock/state check
                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                
                var pending = await _context.PendingRegistrations
                    .FirstOrDefaultAsync(p => p.Token == request.Token);
                
                if (pending == null)
                {
                    _logger.LogWarning("[RegistrationController] Registration session not found for token: {Token}", request.Token);
                    return NotFound(new { message = "Registration session not found." });
                }

                // If already completed (race condition mitigation), retrieve cached tokens instead of duplicate business registration
                if (pending.Status == "Completed")
                {
                    _logger.LogInformation("[RegistrationController] Registration already completed for token: {Token}. Returning cached credentials.", request.Token);
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
                    _logger.LogWarning("[RegistrationController] Registration for token: {Token} is currently being processed by another thread.", request.Token);
                    await dbTransaction.RollbackAsync();
                    return BadRequest(new { message = "Registration is currently being processed. Please wait..." });
                }

                // Transition status to Processing to block concurrent requests
                pending.Status = "Processing";
                _context.PendingRegistrations.Update(pending);
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync(); // Commit state change to Processing

                // Check payment method
                bool isUpi = string.Equals(request.PaymentMethod, "UPI", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(request.UpiTransactionId);

                // If Razorpay gateway payment with signature provided, verify it
                if (!isUpi && !string.IsNullOrEmpty(request.RazorpaySignature))
                {
                    _logger.LogInformation("[RegistrationController] Verifying payment signature for RazorpaySubscriptionId: {SubId}", request.RazorpaySubscriptionId);
                    
                    bool isSignatureValid = _razorpayService.VerifyPaymentSignature(
                        request.RazorpaySubscriptionId, 
                        request.RazorpayPaymentId, 
                        request.RazorpaySignature);

                    if (!isSignatureValid)
                    {
                        _logger.LogWarning("[RegistrationController] Payment signature verification failed. Token: {Token}", request.Token);
                        
                        // Roll back state to PendingPayment
                        pending.Status = "PendingPayment";
                        _context.PendingRegistrations.Update(pending);
                        await _context.SaveChangesAsync();

                        return BadRequest(new { message = "Payment signature verification failed. Unauthorized transaction." });
                    }
                }

                // 3. Resolve limits from plan
                var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == pending.SelectedPlanId) 
                           ?? new SubscriptionPlan { Id = 1, Name = "Starter Shop", MaxBranches = 1, MaxStaff = 2, MonthlyPrice = 499.00m };

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

                // 5. Activate User & Business
                var expiresAt = DateTime.UtcNow.AddMonths(1); // Standard recurring cycle
                
                _logger.LogInformation("[RegistrationController] Registering user and business database entities for Business: {BizName}", registerDto.LegalName);
                
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

                // 7. Track Payment Transaction
                var effectivePaymentId = !string.IsNullOrEmpty(request.UpiTransactionId)
                    ? request.UpiTransactionId
                    : (!string.IsNullOrEmpty(request.RazorpayPaymentId) ? request.RazorpayPaymentId : $"UPI_{Guid.NewGuid():N}".Substring(0, 18));

                var transaction = new PaymentTransaction
                {
                    BusinessId = result.BusinessId,
                    RazorpayPaymentId = effectivePaymentId,
                    RazorpaySubscriptionId = pending.RazorpaySubscriptionId,
                    PaymentMethod = isUpi ? "UPI" : (request.PaymentMethod ?? "Razorpay"),
                    Amount = plan.MonthlyPrice,
                    Status = "Captured",
                    CreatedAt = DateTime.UtcNow,
                    CorrelationId = HttpContext?.Items["CorrelationId"]?.ToString()
                };
                await _context.PaymentTransactions.AddAsync(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[RegistrationController] Account successfully activated. User: {User}, BusinessId: {BizId}", result.Username, result.BusinessId);

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
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("resume/{token}")]
        public async Task<IActionResult> ResumeRegistration(string token)
        {
            try
            {
                _logger.LogInformation("[RegistrationController] ResumeRegistration requested for token: {Token}", token);

                var pending = await _context.PendingRegistrations.FirstOrDefaultAsync(p => p.Token == token);
                if (pending == null)
                {
                    _logger.LogWarning("[RegistrationController] Pending registration not found for token: {Token}", token);
                    return NotFound(new { message = "Checkout token invalid or expired." });
                }

                var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == pending.SelectedPlanId)
                           ?? new SubscriptionPlan { Id = 1, Name = "Starter Shop", MonthlyPrice = 499.00m, YearlyPrice = 4999.00m };

                var upiVpa = _configuration["Upi:Vpa"] ?? "billcom.payments@okaxis";
                var merchantName = _configuration["Upi:MerchantName"] ?? "BillCom POS";
                var upiNote = Uri.EscapeDataString($"BillCom {plan.Name} Subscription");
                var upiUri = $"upi://pay?pa={upiVpa}&pn={Uri.EscapeDataString(merchantName)}&am={plan.MonthlyPrice:F2}&cu=INR&tn={upiNote}&tr={pending.Token}";

                return Ok(new
                {
                    token = pending.Token,
                    subscriptionId = pending.RazorpaySubscriptionId,
                    customerId = pending.RazorpayCustomerId,
                    email = pending.Email,
                    businessName = pending.LegalName,
                    username = pending.Username,
                    planId = plan.Id,
                    planName = plan.Name,
                    amount = plan.MonthlyPrice,
                    monthlyPrice = plan.MonthlyPrice,
                    yearlyPrice = plan.YearlyPrice,
                    upiVpa = upiVpa,
                    merchantName = merchantName,
                    upiUri = upiUri
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RegistrationController] Error resuming registration session");
                return StatusCode(500, new { message = "Error resuming registration session.", error = ex.Message });
            }
        }

        [HttpPost("mark-failed")]
        public async Task<IActionResult> MarkFailed([FromBody] MarkFailedRequest request)
        {
            try
            {
                _logger.LogInformation("[RegistrationController] MarkFailed requested for token: {Token}. Reason: {Reason}", request.Token, request.Reason);

                var pending = await _context.PendingRegistrations.FirstOrDefaultAsync(p => p.Token == request.Token);
                if (pending == null)
                {
                    _logger.LogWarning("[RegistrationController] Pending registration not found to mark failed: {Token}", request.Token);
                    return NotFound();
                }

                pending.Status = "Failed";
                _context.PendingRegistrations.Update(pending);

                // Track Failed Payment Transaction in DB
                var transaction = new PaymentTransaction
                {
                    RazorpaySubscriptionId = pending.RazorpaySubscriptionId,
                    Status = "Failed",
                    FailureReason = request.Reason ?? "Payment session cancelled/dismissed by user.",
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
                return StatusCode(500, new { message = "Error updating registration status.", error = ex.Message });
            }
        }
    }

    public class VerifyPaymentRequest
    {
        public string Token { get; set; } = string.Empty;
        public string RazorpaySubscriptionId { get; set; } = string.Empty;
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public string RazorpaySignature { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? UpiTransactionId { get; set; }
    }

    public class MarkFailedRequest
    {
        public string Token { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}
