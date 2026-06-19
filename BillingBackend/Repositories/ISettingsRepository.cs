using BillingBackend.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingBackend.Repositories
{
    public interface ISettingsRepository
    {
        Task<(WhatsAppSettings? Settings, IEnumerable<WhatsAppTemplate> Templates)> GetWhatsAppSettingsAsync(int businessId);
        Task<WhatsAppSettings> UpdateWhatsAppSettingsAsync(int businessId, string? apiKey, bool isConnected);
        Task<WhatsAppTemplate> AddWhatsAppTemplateAsync(int businessId, string templateName);
        Task<bool> DeleteWhatsAppTemplateAsync(int businessId, int templateId);
    }
}
