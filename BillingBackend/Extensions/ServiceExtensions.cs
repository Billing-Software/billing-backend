using BillingBackend.Data;
using BillingBackend.Repositories;
using BillingBackend.Services;
using BillingBackend.Services.Sms;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BillingBackend.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<BillingDbContext>(options =>
                options.UseSqlServer(config.GetConnectionString("DefaultConnection")));
            return services;
        }

        public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration config)
        {
            var jwtKey = config["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key is missing from configuration");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = config["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IBusinessRepository, BusinessRepository>();
            services.AddScoped<IBranchRepository, BranchRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IServiceRepository, ServiceRepository>();
            services.AddScoped<IInventoryRepository, InventoryRepository>();
            services.AddScoped<IStaffRepository, StaffRepository>();
            services.AddScoped<IBillRepository, BillRepository>();
            services.AddScoped<IDashboardRepository, DashboardRepository>();
            services.AddScoped<ISettingsRepository, SettingsRepository>();
            services.AddScoped<IExpenseRepository, ExpenseRepository>();
            services.AddScoped<IPurchaseRepository, PurchaseRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();

            // Services
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IBusinessService, BusinessService>();
            services.AddScoped<IBranchService, BranchService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IServiceService, ServiceService>();
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<IStaffService, StaffService>();
            services.AddScoped<IBillService, BillService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<ISettingsService, SettingsService>();
            services.AddScoped<IExpenseService, ExpenseService>();
            services.AddScoped<IPurchaseService, PurchaseService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IBusinessConfigurationService, BusinessConfigurationService>();
            services.AddScoped<ITaxService, TaxService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IFeatureService, FeatureService>();


            services.AddHttpClient<IRazorpayService, RazorpayService>();
            services.AddHttpClient<ISmsService, ExotelSmsService>();

            // Storage Service configuration
            services.AddHttpContextAccessor();
            var accountId = config["CloudflareR2:AccountId"];
            if (!string.IsNullOrEmpty(accountId))
            {
                services.AddSingleton<IStorageService, CloudflareR2StorageService>();
            }
            else
            {
                services.AddScoped<IStorageService, LocalStorageService>();
            }

            return services;
        }

        public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration? config = null)
        {
            var raw = config?["Cors:AllowedOrigins"] ?? Environment.GetEnvironmentVariable("Cors__AllowedOrigins") ?? string.Empty;
            var origins = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(o => o.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                         || o.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", policy =>
                {
                    policy.WithHeaders("Authorization", "Content-Type", "X-Requested-With", "X-Correlation-Id", "X-Idempotency-Key")
                          .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                          .SetPreflightMaxAge(TimeSpan.FromHours(1));
                    if (origins.Length > 0)
                    {
                        policy.WithOrigins(origins).AllowCredentials();
                    }
                    else
                    {
                        // Fail closed. Set Cors__AllowedOrigins explicitly for every environment.
                        policy.DisallowCredentials();
                    }
                });
            });
            return services;
        }

        public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Strict bucket for anonymous auth endpoints (login/refresh/forgot/reset/trial).
                options.AddFixedWindowLimiter("auth", o =>
                {
                    o.PermitLimit = 10;
                    o.Window = TimeSpan.FromMinutes(1);
                    o.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                    o.QueueLimit = 0;
                });

                // General API bucket per authenticated user / IP.
                options.AddFixedWindowLimiter("api", o =>
                {
                    o.PermitLimit = 300;
                    o.Window = TimeSpan.FromMinutes(1);
                    o.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                    o.QueueLimit = 0;
                });

                // Very strict bucket for OTP verification / payment verification.
                options.AddFixedWindowLimiter("strict", o =>
                {
                    o.PermitLimit = 5;
                    o.Window = TimeSpan.FromMinutes(1);
                    o.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                    o.QueueLimit = 0;
                });
            });
            return services;
        }

        public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "BillCom API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("Bearer"),
                        new List<string>()
                    }
                });
            });

            return services;
        }
    }
}
