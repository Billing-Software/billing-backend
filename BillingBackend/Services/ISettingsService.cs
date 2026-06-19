using BillingBackend.DTOs;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public interface ISettingsService
    {
        Task<WhatsAppSettingsDto> GetWhatsAppSettingsAsync(int businessId);
        Task<WhatsAppSettingsDto> UpdateWhatsAppSettingsAsync(int businessId, UpdateWhatsAppSettingsDto dto);
        Task<WhatsAppTemplateDto> AddWhatsAppTemplateAsync(int businessId, AddWhatsAppTemplateDto dto);
        Task<bool> DeleteWhatsAppTemplateAsync(int businessId, int templateId);
    }
}
