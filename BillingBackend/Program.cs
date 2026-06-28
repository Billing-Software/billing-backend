using BillingBackend.Data;
using BillingBackend.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register application configuration & dependencies via extensions
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddCorsPolicy();
builder.Services.AddApplicationServices();
builder.Services.AddSwaggerServices();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Automatically create the database if it doesn't exist on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BillingDbContext>();
        context.Database.EnsureCreated();
        // We commented out the DROP TABLE loop to prevent data loss.

        //var tablesToDrop = new[] 
        //{
        //    "PurchaseItems", "Purchases", "Expenses", "WhatsAppTemplates", "WhatsAppSettings",
        //    "BillItems", "Bills", "StaffMembers", "InventoryItems", "Services",
        //    "Customers", "Branches", "Businesses", "Categories", "Users"
        //};
        //foreach (var table in tablesToDrop)
        //{
        //    try
        //    {
        //        context.Database.ExecuteSqlRaw($"DROP TABLE \"{table}\" CASCADE CONSTRAINTS");
        //    }
        //    catch { /* Ignore if table doesn't exist */}
        //}
        
        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE \"StaffMembers\" DROP CONSTRAINT \"FK_Staff_Branches_BranchId\"");
        }
        catch { }

        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE \"StaffMembers\" DROP COLUMN \"BranchId\"");
        }
        catch { }

        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE \"Users\" ADD \"PasswordResetToken\" VARCHAR2(100) NULL");
        }
        catch { }

        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE \"Users\" ADD \"PasswordResetTokenExpiry\" TIMESTAMP NULL");
        }
        catch { }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the database.");
    }
}

app.Run();
