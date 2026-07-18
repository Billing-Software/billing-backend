using BillingBackend.Repositories;

namespace BillingBackend.Services
{
    /// <summary>
    /// General settings service.
    /// WhatsApp settings have been moved to WhatsAppService.
    /// This service is reserved for future non-WhatsApp settings.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        public SettingsService()
        {
        }
    }
}
