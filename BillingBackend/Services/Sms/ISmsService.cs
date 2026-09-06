using System.Collections.Generic;
using System.Threading.Tasks;
using BillingBackend.DTOs;

namespace BillingBackend.Services.Sms
{
    public interface ISmsService
    {
        Task<BusinessSmsSettingsDto?> GetSettingsAsync(int businessId);
        Task<BusinessSmsSettingsDto> SaveSettingsAsync(int businessId, UpdateSmsSettingsDto dto);
        Task<SmsResult> SendAsync(int businessId, string phoneNumber, string message, string? templateId = null);
        Task<SmsResult> SendInvoiceSmsAsync(int businessId, int billId, string customerPhone);
        Task<IEnumerable<SmsLogDto>> GetLogsAsync(int businessId, int? billId = null);
    }
}
