using BillingBackend.Data;
using BillingBackend.Extensions;
using BillingBackend.Middleware;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
// Automatically load environment variables from .env if present
LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

static void LoadDotEnv()
{
    try
    {
        string[] searchPaths = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory,
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "private")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "private")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "private")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".."))
        };

        foreach (var dir in searchPaths)
        {
            var envFile = Path.Combine(dir, ".env");
            if (File.Exists(envFile))
            {
                foreach (var line in File.ReadAllLines(envFile))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;
                    var separatorIndex = trimmed.IndexOf('=');
                    if (separatorIndex > 0)
                    {
                        var key = trimmed.Substring(0, separatorIndex).Trim();
                        var value = trimmed.Substring(separatorIndex + 1).Trim().Trim('"', '\'');
                        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                        {
                            Environment.SetEnvironmentVariable(key, value);
                        }
                    }
                }
                break;
            }
        }
    }
    catch { }
}

// Fail fast on missing critical secrets in production (industrial guardrail).
if (builder.Environment.IsProduction())
{
    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey))
        throw new InvalidOperationException("Jwt:Key is missing. Set Jwt__Key env var (min 32 random bytes).");
    if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
        throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing. Set ConnectionStrings__DefaultConnection env var.");
}

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<BillingBackend.Filters.SubscriptionCheckFilter>();
});

// Global upload / body limits (defense against decompression-bomb DoS).
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
    o.ValueLengthLimit = 2 * 1024 * 1024;
    o.MultipartHeadersLengthLimit = 32 * 1024;
});
builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 12 * 1024 * 1024; // 12 MB global ceiling
});

// Register application configuration & dependencies via extensions
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddAppRateLimiting();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddHostedService<BillingBackend.Services.AbandonedRegistrationReminderService>();
builder.Services.AddHealthChecks();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerServices();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();
}

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

// Schema changes are applied only by the versioned release migrations.
// Never run DDL opportunistically at application startup.

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();

// CorrelationId middleware — must be FIRST so all requests (incl. errors) get a trace ID
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BillCom API V1");
    });
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors("CorsPolicy");

app.UseStaticFiles();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers().RequireRateLimiting("api");

// Database updates are managed by the BillingBackend.Migrator project.
// Run BillingBackend.Migrator to apply schema changes and table alters.

app.Run();
