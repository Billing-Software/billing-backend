using BillingBackend.Data;
using BillingBackend.Extensions;
using BillingBackend.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<BillingBackend.Filters.SubscriptionCheckFilter>();
});

// Register application configuration & dependencies via extensions
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddCorsPolicy();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddHostedService<BillingBackend.Services.AbandonedRegistrationReminderService>();
builder.Services.AddSwaggerServices();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Automatically verify and apply missing columns & tables to Oracle DB on startup
DatabaseSchemaInitializer.EnsureDatabaseSchemaUpdated(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Billing API V1");
    });
}

// app.UseHttpsRedirection();

app.UseCors("CorsPolicy");

app.UseStaticFiles();

// CorrelationId middleware — must be BEFORE auth so all requests get a trace ID
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Database updates are managed by the BillingBackend.Migrator project.
// Run BillingBackend.Migrator to apply schema changes and table alters.

app.Run();

