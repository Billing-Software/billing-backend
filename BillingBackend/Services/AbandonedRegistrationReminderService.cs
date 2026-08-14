using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BillingBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BillingBackend.Services
{
    public class AbandonedRegistrationReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AbandonedRegistrationReminderService> _logger;
        // Interval: check every 5 minutes (for local testing responsiveness, but enforces >15m elapsed time)
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

        public AbandonedRegistrationReminderService(
            IServiceProvider serviceProvider, 
            IConfiguration configuration,
            ILogger<AbandonedRegistrationReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[BackgroundService] AbandonedRegistrationReminderService is starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendRemindersAsync();
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Invalid object name") || ex.InnerException?.Message.Contains("Invalid object name") == true)
                    {
                        _logger.LogWarning("[AbandonedRegistrationReminderService] PendingRegistrations table does not exist in SQL Server DB yet. Will retry after schema initialization.");
                    }
                    else
                    {
                        _logger.LogWarning("[BackgroundService Notice] AbandonedRegistrationReminderService notice: {Message}", ex.Message);
                    }
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task SendRemindersAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var cutoffTime = DateTime.UtcNow.AddMinutes(-15);
                
                // Get pending registrations stuck > 15 minutes, not completed, and email not sent yet
                var pendingList = await context.PendingRegistrations
                    .Where(p => p.Status != "Completed" && 
                                p.ReminderEmailSent == false && 
                                p.CreatedAt <= cutoffTime)
                    .ToListAsync();

                if (!pendingList.Any())
                {
                    return;
                }

                _logger.LogInformation("[BackgroundService] Found {Count} abandoned registrations. Processing reminder emails...", pendingList.Count);

                var marketingSiteUrl = _configuration["MarketingSiteUrl"] ?? "http://localhost:5173";

                foreach (var pending in pendingList)
                {
                    try
                    {
                        var recoveryLink = $"{marketingSiteUrl.TrimEnd('/')}/#/checkout/resume?token={pending.Token}";
                        
                        var emailBody = $@"
                            <h2>Complete your BillCom Setup!</h2>
                            <p>Hi {pending.Username},</p>
                            <p>We noticed you started setting up your account for <strong>{pending.LegalName}</strong>, but did not complete your payment setup.</p>
                            <p>Your details are safely saved. Click the link below to complete your Razorpay Autopay setup and activate your account:</p>
                            <p style='margin: 20px 0;'>
                                <a href='{recoveryLink}' style='background-color: #006a61; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;'>
                                    Complete Payment & Activate Account
                                </a>
                            </p>
                            <p>If the button doesn't work, copy and paste this link into your browser: <br/> {recoveryLink}</p>
                            <br/>
                            <p>Best regards,<br/>BillCom Onboarding Support Team</p>";

                        await emailService.SendEmailAsync(pending.Email, "Finish setting up your BillCom Account", emailBody);

                        // Mark as sent
                        pending.ReminderEmailSent = true;
                        pending.ReminderEmailSentAt = DateTime.UtcNow;
                        context.PendingRegistrations.Update(pending);
                        
                        _logger.LogInformation("[BackgroundService] Sent reminder email to: {Email}", pending.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[BackgroundService Error] Failed to send reminder email to {Email}", pending.Email);
                    }
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
