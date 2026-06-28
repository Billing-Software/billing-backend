using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BillingBackend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpSection = _configuration.GetSection("Smtp");
            var host = smtpSection["Host"] ?? "";
            var portStr = smtpSection["Port"] ?? "587";
            var enableSslStr = smtpSection["EnableSsl"] ?? "true";
            var username = smtpSection["Username"] ?? "";
            var password = smtpSection["Password"] ?? "";
            var fromEmail = smtpSection["FromEmail"] ?? "";
            var fromName = smtpSection["FromName"] ?? "SmartBill Pro";

            int port = int.TryParse(portStr, out var p) ? p : 587;
            bool enableSsl = bool.TryParse(enableSslStr, out var s) ? s : true;

            // If SMTP username/password are empty, fall back to console logging so the app doesn't crash on startup/testing.
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(host))
            {
                Console.WriteLine("\n========================================================");
                Console.WriteLine("[EMAIL SERVICE WARNING] SMTP credentials are not configured in appsettings.json.");
                Console.WriteLine($"[EMAIL SERVICE SIMULATION] Sending email to: {toEmail}");
                Console.WriteLine($"[EMAIL SERVICE SIMULATION] Subject: {subject}");
                Console.WriteLine($"[EMAIL SERVICE SIMULATION] Reset Code in body");
                Console.WriteLine("========================================================\n");
                return;
            }

            using (var smtpClient = new SmtpClient(host, port))
            {
                smtpClient.Credentials = new NetworkCredential(username, password);
                smtpClient.EnableSsl = enableSsl;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);
            }
        }
    }
}
