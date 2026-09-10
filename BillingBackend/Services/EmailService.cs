using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BillingBackend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Recipient email is required.", nameof(toEmail));

            var smtpSection = _configuration.GetSection("Smtp");
            var host = smtpSection["Host"] ?? "";
            var portStr = smtpSection["Port"] ?? "587";
            var enableSslStr = smtpSection["EnableSsl"] ?? "true";
            var username = smtpSection["Username"] ?? "";
            var password = smtpSection["Password"] ?? "";
            var fromEmail = smtpSection["FromEmail"] ?? "";
            var fromName = smtpSection["FromName"] ?? "BillCom";

            int port = int.TryParse(portStr, out var p) ? p : 587;
            bool enableSsl = bool.TryParse(enableSslStr, out var s) ? s : true;

            // Fail closed when SMTP is not configured in production; log only metadata (never body/PII).
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(host))
            {
                _logger.LogWarning("SMTP is not configured. Email to {Domain} skipped.", GetDomain(toEmail));
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

        private static string GetDomain(string email)
        {
            var at = email.LastIndexOf('@');
            return at >= 0 ? email[(at + 1)..] : "unknown";
        }
    }
}
