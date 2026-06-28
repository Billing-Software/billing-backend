using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
